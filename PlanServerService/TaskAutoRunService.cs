using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace PlanServerService
{
    /// <summary>
    /// 自动轮询处理任务的服务类
    /// </summary>
    public static class TaskAutoRunService
    {
        public static string Version = "20141024";

        static TaskAutoRunService()
        {
            Utils.Output("当前版本:" + Version, "start");
            ServicePointManager.DefaultConnectionLimit = 100;
            ThreadPool.SetMinThreads(10, 10);
        }

        #region 字段与属性
        private static int _refreshDbSecond;
        /// <summary>
        /// 任务状态刷新和轮询间隔
        /// </summary>
        static int RefreshDbSecond
        {
            get
            {
                if (_refreshDbSecond <= 0)
                {
                    string tmp = Common.GetSetting("refreshDbSecond");
                    if (!int.TryParse(tmp, out _refreshDbSecond))
                        _refreshDbSecond = 30;
                    if (_refreshDbSecond <= 0)
                        _refreshDbSecond = 30;
                }
                return _refreshDbSecond;
            }
        }

        #endregion


        #region 任务及进程处理相关方法
        // 主调方法：遍历数据库，循环处理任务
        public static void Run()
        {
            Utils.Output("计划任务服务端启动", "start");
            UpdateDB();

            // 程序首次运行，必须清空任务参数表（该表用于判断定时运行的任务结束时间）
            Dal.Default.ClearTimePara();

            int second = RefreshDbSecond;
            while (true)
            {
                try
                {
                    List<TaskItem> tasks = Dal.Default.GetAllTask();
                    List<Thread> threads = new List<Thread>();
                    if (tasks != null && tasks.Any())
                    {
                        Utils.Output("找到" + tasks.Count.ToString() + "个任务");

                        // 读取任务执行前的进程清单，用于后面比对
                        var processes = ProcessItem.GetProcessesAndCache();

                        foreach (TaskItem task in tasks)
                        {
                            Thread thre = new Thread(RunTask) { IsBackground = true };
                            threads.Add(thre);
                            thre.Start(task);
                        }
                        threads.ForEach(item => item.Join());// 阻塞到全部线程完成

                        RunTaskFinish(tasks, processes);
                    }

                    // 一轮完成，更新进程最后检查时间
                    Dal.Default.UpdateLastRuntime();
                }
                catch (Exception exp)
                {
                    Utils.Output("Main出错", exp);
                }

                Utils.Output("休眠" + second.ToString() + "秒");
                Thread.Sleep(second * 1000);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        // 运行单个任务的主调方法, 注：因为是多线程执行，所以通过task.newpid 临时存储启动了的pid
        static void RunTask(object args)
        {
            try
            {
                TaskItem task = (TaskItem)args;

                // 防止出现 c:\\\\a.exe 或 c:/a.exe这样的路径,统一格式化成：c:\a.exe形式
                task.exepath = Path.Combine(Path.GetDirectoryName(task.exepath) ?? "",
                    Path.GetFileName(task.exepath) ?? "");

                if (!File.Exists(task.exepath))
                {
                    // 可执行文件不存在, 更新任务运行状态
                    Dal.Default.UpdateTaskExeStatus(task.id, ExeStatus.NoFile);
                    var tmpmsg = task.desc + " " + task.exepath + " 文件不存在";
                    Utils.Output(tmpmsg);
                    return;
                }

                // 根据exe路径，查找运行中的进程
                var processes = ProcessItem.GetProcessesAndCache().FindAll(item =>
                    item.exePath.Equals(task.exepath, StringComparison.OrdinalIgnoreCase));

                ExeStatus status = ExeStatus.Unknown; // 判断进程状态，以更新表
                DateTime now = DateTime.Now;

                StringBuilder msg = new StringBuilder(200);
                msg.AppendFormat(task.desc);

                // 每n分钟输出一次任务具体数据
                if (now.Minute % 10 == 0 && now.Second <= RefreshDbSecond)
                {
                    msg.AppendFormat("\r\n{0} {1} {2} {3} 已运行{4}次",
                                     task.runtype.ToString(), task.taskpara,
                                     task.exepath, task.exepara, task.runcount.ToString());
                }

                switch (task.runtype)
                {
                    case RunType.Stop:
                        // 不启动，不停止
                        status = NoOperation(processes, msg);
                        break;

                    case RunType.Restart:
                        // 重启
                        status = Restart(task, processes, msg);
                        break;

                    case RunType.StopAndWait1Min:
                        // 停止并等1分钟重启
                        status = StopAndWait1Min(task, processes, msg);
                        break;

                    case RunType.ForceStop:
                        // 强行停止
                        status = ForceStop(task, processes, msg);
                        break;

                    case RunType.Always:
                    case RunType.OneTime:
                        // 一直运行 或 只运行一次
                        status = AlwaysOrOneTime(task, processes, msg);
                        break;

                    case RunType.PerDay:
                    case RunType.PerWeek:
                    case RunType.PerMonth:
                        // 定时运行
                        status = PerTime(task, processes, msg);
                        break;
                }

                // 更新任务运行状态
                Dal.Default.UpdateTaskExeStatus(task.id, status);
            }
            catch (Exception ex)
            {
                Utils.Output("运行任务时错误", ex);
            }
        }

        static void RunTaskFinish(List<TaskItem> tasks, List<ProcessItem> processesBefore)
        {
#if DEBUG
             var end = DateTime.Now;
#endif
            var processesLater = ProcessItem.GetProcessesAndCache(false);
            foreach (TaskItem task in tasks)
            {
                var proBefore = ProcessHelper.FilterByPath(processesBefore, task.exepath);
                var proLater = ProcessHelper.FilterByPath(processesLater, task.exepath);
                var pidBefore = new StringBuilder();
                var pidEnd = new StringBuilder();
                foreach (var processItem in proBefore)
                {
                    pidBefore.AppendFormat("{0},", processItem.pid.ToString());
                }
                var taskNewPidFinded = false;
                foreach (var processItem in proLater)
                {
                    if (task.NewPid > 0 && processItem.pid == task.NewPid)
                    {
                        taskNewPidFinded = true;
                    }
                    pidEnd.AppendFormat("{0},", processItem.pid.ToString());
                }
                var noFindAndLog = (task.NewPid > 0 && !taskNewPidFinded);

                // Utils.Output(task.exepath+"\r\n"+pidBefore + "\r\n" + pidEnd + "\r\n" + task.NewPid, "aa");
                // 运行时的进程不见了，或 前后的pid不同了
                if (noFindAndLog || pidBefore.ToString() != pidEnd.ToString())
                {
                    // 更新任务的pid
                    var newpid = proLater.Count > 0 ? proLater[0].pid : 0;
                    Dal.Default.UpdateTaskProcessId(task.id, newpid);
                    if (pidBefore.Length > 0)
                    {
                        pidBefore.Insert(0, "; 运行前pid:");
                    }
                    if (pidEnd.Length > 0)
                    {
                        pidBefore.AppendFormat("; 运行中pid:{0}", pidEnd);
                    }
                    if (noFindAndLog)
                    {
                        pidBefore.AppendFormat("; 运行中pid{0} 已自动退出", task.NewPid.ToString());
                    }

                    var pidMsg = task.runtype.ToString() + pidBefore;
                    Dal.Default.AddTaskLog(task.exepath, pidMsg);
                }
            }

#if DEBUG
            // 输出任务执行前后的进程情况
            var procMsg = new StringBuilder();
            // procMsg.AppendFormat("执行前:{0}\r\n", begin.ToString("HH:mm:ss.fff"));
            foreach (var processItem in processesBefore.OrderBy(item => item.exePath))
            {
                procMsg.AppendFormat("{0} {1}\r\n", processItem.pid.ToString(), processItem.exePath);
            }
            procMsg.AppendFormat("执行后:{0}\r\n", end.ToString("HH:mm:ss.fff"));
            foreach (var processItem in processesLater.OrderBy(item => item.exePath))
            {
                procMsg.AppendFormat("{0} {1}\r\n", processItem.pid.ToString(), processItem.exePath);
            }
            Utils.Output(procMsg.ToString(), "process");
#endif
        }

        // RunType.Stop
        static ExeStatus NoOperation(List<ProcessItem> processes, StringBuilder msg)
        {
            ExeStatus status;
            var processCnt = processes.Count;
            if (processCnt > 0)
            {
                msg.Append("\r\n\t运行中任务id：");
                foreach (ProcessItem item in processes)
                {
                    msg.AppendFormat("{0} ", item.pid);
                }
                status = ExeStatus.Running;
            }
            else
            {
                msg.Append("\r\n\t任务未启动");
                status = ExeStatus.Stopped;
            }
            Utils.Output(msg);
            return status;
        }

        // RunType.Restart
        static ExeStatus Restart(TaskItem task, List<ProcessItem> processes, StringBuilder msg)
        {
            // 重启后要把它设置为一直运行
            Dal.Default.UpdateTaskType(task.id, task.runtype, RunType.Always);
            if (processes.Count > 0)
            {
                // 杀死进程
                var killNum = ProcessHelper.KillProcesses(processes);
                msg.Append("\r\n\t已停止" + killNum.ToString() + "个,");
            }
            else
            {
                msg.Append("\r\n\t进程不存在，");
            }
            // 启动进程
            var ret = ProcessHelper.CheckAndStartProcess(task.exepath, task.exepara);
            if (ret > 0)
            {
                task.NewPid = ret;
                msg.Append("重启完成,pid:" + ret.ToString());
                if (processes.Count > 0)
                {
                    foreach (ProcessItem item in processes)
                    {
                        msg.AppendFormat("\r\n\t\t{0}", item);
                    }
                }
            }
            else
            {
                msg.Append("进程存在，未启动");
            }

            Utils.Output(msg);
            return ExeStatus.Running;

        }

        static ExeStatus StopAndWait1Min(TaskItem task, List<ProcessItem> processes, StringBuilder msg)
        {
            // 重启后要把它设置为一直运行
            Dal.Default.UpdateTaskType(task.id, task.runtype, RunType.Always);

            if (processes.Count > 0)
            {
                // 杀死进程
                var killNum = ProcessHelper.KillProcesses(processes);
                Utils.Output(task.desc + " " + task.exepath + "已停止" + killNum.ToString() + "个，等1分钟后重启...");
                foreach (ProcessItem item in processes)
                {
                    msg.AppendFormat("\r\n\t\t{0}", item);
                }

                msg.Append("\r\n\t已停止，等1分钟，");
            }
            else
            {
                Utils.Output(task.desc + " " + task.exepath + "未启动，等1分钟后启动...");
                msg.Append("\r\n\t进程不存在，等1分钟，");
            }
            Thread.Sleep(TimeSpan.FromMinutes(1));

            var ret = ProcessHelper.CheckAndStartProcess(task.exepath, task.exepara);
            if (ret > 0)
            {
                task.NewPid = ret;
                msg.Append("\r\n\t重启完成,pid:" + ret.ToString());
            }
            else
            {
                msg.Append("\r\n\t进程存在，未启动");
            }
            Utils.Output(msg);
            return ExeStatus.Running;
        }

        static ExeStatus ForceStop(TaskItem task, List<ProcessItem> processes, StringBuilder msg)
        {
            // 停止后要把它设置为不启动状态
            Dal.Default.UpdateTaskType(task.id, task.runtype, RunType.Stop);

            if (processes.Count > 0)
            {
                // 杀死进程
                var killNum = ProcessHelper.KillProcesses(processes);
                msg.Append("\r\n\t停止完成" + killNum.ToString() + "个");
                foreach (ProcessItem item in processes)
                {
                    msg.AppendFormat("\r\n\t\t{0}", item);
                }
            }
            else
            {
                msg.Append("\r\n\t任务未启动");
            }
            Utils.Output(msg);
            return ExeStatus.Stopped;
        }

        static ExeStatus AlwaysOrOneTime(TaskItem task, List<ProcessItem> processes, StringBuilder msg)
        {
            var ret = ExeStatus.Running;
            // 查找进程是否运行中
            msg.Append("\r\n\t");
            if (task.runtype == RunType.OneTime)
            {
                // 更新为停止
                Dal.Default.UpdateTaskType(task.id, task.runtype, RunType.Stop);
                msg.Append("(只运行一次)");
            }

            if (processes.Count <= 0)
            {
                var lastRunDiffSecond = (DateTime.Now - task.pidtime).TotalSeconds;
                // 一直运行的，每2次启动必须间隔1分钟
                if (task.runtype == RunType.OneTime || lastRunDiffSecond > 60)
                {
                    // 启动进程
                    var pid = ProcessHelper.CheckAndStartProcess(task.exepath, task.exepara);
                    if (pid > 0)
                    {
                        task.NewPid = pid;
                        msg.Append("任务成功启动,pid:" + pid.ToString());
                    }
                    else
                    {
                        msg.Append("任务存在，启动失败");
                    }
                }
                else
                {
                    msg.Append("1分钟内任务只能启动1次");
                    ret = ExeStatus.Stopped;
                }
            }
            else
            {
                msg.Append("运行中pid:");
                foreach (ProcessItem item in processes)
                {
                    msg.AppendFormat("\t{0}", item.pid);
                }
            }

            Utils.Output(msg);

            return ret;
        }

        static ExeStatus PerTime(TaskItem task, List<ProcessItem> processes, StringBuilder msg)
        {
            ExeStatus status;

            // 未设置定时运行参数时
            if (!task.TaskPara.Any())
            {
                status = ExeStatus.NoPara;
                // 更新任务运行状态
                Dal.Default.UpdateTaskExeStatus(task.id, status);
                msg.Append("\r\n\t任务参数未配置，运行失败");
                Utils.Output(msg.ToString());
                return status;
            }


            if (processes.Count <= 0)
            {
                status = ExeStatus.Stopped;
            }
            else
            {
                foreach (ProcessItem item in processes)
                {
                    msg.AppendFormat("\r\n\t\t{0}", item);
                }
                status = ExeStatus.Running;
            }

            StringBuilder sbEndTime = new StringBuilder();
            var now = DateTime.Now;
            bool? isrun = null;
            foreach (TimePara timepara in task.TaskPara)
            {
                if (task.runtype == RunType.PerWeek && timepara.WeekOrDay != (int)now.DayOfWeek)
                    continue;
                if (task.runtype == RunType.PerMonth && timepara.WeekOrDay != now.Day)
                    continue;

                // 启动小时必须是小于24的数，否则会出异常
                int hour = timepara.StartHour;// == 24 ? 0 : timepara.StartHour;
                if (hour < 0)
                    hour = 0;
                else if (hour >= 24)
                    hour = 0;
                int min = timepara.StartMin;
                if (min > 59)
                    min = 59;
                else if (min < 0)
                    min = 0;

                DateTime starttime = new DateTime(now.Year, now.Month, now.Day,
                                                  hour, min, 0);
                if (timepara.StartHour >= 24)
                    starttime = starttime.AddDays(1);

                DateTime endtime = DateTime.MaxValue;
                // 为true表示启动时间是从数据库获取的
                bool isTimeParaSeted = false;
                if (timepara.RunMinute > 0)
                {
                    // 从数据库取得记录的结束时间（之所以保存到数据库，因为每次任务都是从db取得，每次StartTime都会被重置，导致隔日停止失败）
                    timepara.StartTime = Dal.Default.GetTimePara(task.id, timepara);
                    if (timepara.StartTime == default(DateTime))
                    {
                        timepara.StartTime = starttime;
                    }
                    else
                    {
                        isTimeParaSeted = true;
                    }
                    endtime = timepara.StartTime.AddMinutes(timepara.RunMinute);

                    sbEndTime.Append("\r\n\t" + timepara.StartTime.ToString("MM-dd_HH:mm") + "~" +
                                     endtime.ToString("MM-dd_HH:mm"));
                }
                // 任务已经处理过了
                if (isrun != null)
                    continue;

                // 前次运行时间比定时小 且 时间到了 且 进程没有运行 且 没过结束时间
                //if (starttime > task.pidtime && starttime <= now && now < endtime)
                // 在启动时间之后，且在1分钟之内，启动它
                if (starttime <= now && now < endtime && (now - starttime).TotalSeconds < 60)
                {
                    isrun = true;

                    // 记录开始时间，以便下次轮询判断结束
                    if (isTimeParaSeted)
                    {
                        Dal.Default.DelTimePara(task.id, timepara);
                    }
                    Dal.Default.AddTimePara(task.id, timepara);

                    if (processes.Count <= 0)
                    {
                        var lastRunDiffSecond = (now - task.pidtime).TotalSeconds;
                        if (lastRunDiffSecond > 60)
                        {
                            // 启动进程
                            var pid = ProcessHelper.CheckAndStartProcess(task.exepath, task.exepara);
                            if (pid > 0)
                            {
                                task.NewPid = pid;
                                msg.Append("任务成功启动,pid:" + pid.ToString());
                            }
                            else
                            {
                                msg.Append("任务存在，启动失败");
                            }
                            status = ExeStatus.Running;
                        }
                        else
                        {
                            msg.Append("\r\n\t任务1分钟只能启动1次");
                            status = ExeStatus.Stopped;
                        }
                    }
                    else
                    {
                        msg.Append("\r\n\t" + processes.Count.ToString() + "个任务运行中");
                        status = ExeStatus.Running;
                    }
                    // 记录之，用于计算结束时间
                    if (timepara.RunMinute > 0)
                        timepara.StartTime = now;

                    // 不用break，是为了统计并输出结束时间日志
                    continue;
                }
                // now比endtime大，且在1分钟之内，停止它
                if (endtime <= now && (now - endtime).TotalSeconds < 60 && processes.Count > 0)
                {
                    isrun = false;
                    // 结束完成，删除 开始时间记录，以便下次轮询重新计算
                    if (isTimeParaSeted)
                        Dal.Default.DelTimePara(task.id, timepara);

                    var killNum = ProcessHelper.KillProcesses(processes);
                    if (killNum > 0)
                    {
                        msg.Append("\r\n\t任务成功终止" + killNum.ToString() + "个");
                        status = ExeStatus.Stopped;
                    }
                    else
                        msg.Append("\r\n\t任务终止失败");

                    // 不用break，是为了统计并输出结束时间日志
                    //continue;
                }
                else if (endtime <= now && isTimeParaSeted)
                {
                    // 清除上次启动时间，避免过期时间在以前，导致程序永远无法终止
                    Dal.Default.DelTimePara(task.id, timepara);
                }
            }
            if (isrun == null)
            {
                msg.Append("\r\n\t时间没到 " + (status == ExeStatus.Stopped ? "任务未启动" : "任务运行中"));
            }
            if (sbEndTime.Length > 0)
                msg.Append("\r\n\t起止时间:" + sbEndTime);
            Utils.Output(msg);

            return status;
        }

        /// <summary>
        /// 服务启动时，检测有没有数据库变更脚本要执行
        /// </summary>
        static void UpdateDB()
        {
            // 服务启动时，检测到这个文件，会执行里面的sql，用于表结构变更等等.
            // 多个sql以分号分隔
            string _dbModityFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sqlupdate.sql");
            if (!File.Exists(_dbModityFile))
            {
                return;
            }

            string allsql;
            try
            {
                using (var sr = new StreamReader(_dbModityFile, Encoding.UTF8))
                {
                    allsql = sr.ReadToEnd();
                }
            }
            catch (Exception exp)
            {
                Utils.Output("UpdateDB 读取文件错误:" + _dbModityFile, exp);
                return;
            }
            foreach (string sql in allsql.Split(';'))
            {
                try
                {
                    var ret = Dal.Default.ExecuteSql(sql);
                    Utils.Output(ret.ToString() + "行\r\nsql:" + sql, "updatedb");
                }
                catch (Exception exp)
                {
                    Utils.Output("UpdateDB sql错误:" + sql, exp);
                    break;
                }
            }
            try
            {
                File.Move(_dbModityFile, _dbModityFile + DateTime.Now.ToString("yyyyMMddHHmmss"));
            }
            catch (Exception exp)
            {
                Utils.Output("UpdateDB 移动文件错误:" + _dbModityFile, exp);
            }
        }

        #endregion


    }
}
