using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text;
using System.Threading;
using PlanServerService.FileAdmin;

namespace PlanServerService
{
    public static class TaskService
    {
        public static string Version = "20141024";

        static TaskService()
        {
            Output("当前版本:" + Version, "start");
        }

        #region 字段与属性
        // 监听端口
        private static int _listenPort;
        public static int ListenPort
        {
            get
            {
                if (_listenPort <= 0)
                {
                    _listenPort = Common.GetInt32("listenPort", 23244);
                }
                return _listenPort;
            }
        }
        // 服务器等待客户端发送数据的时长(秒)，超时则关闭连接
        private static int _waitClientSecond;
        public static int WaitClientSecond
        {
            get
            {
                if (_waitClientSecond <= 0)
                {
                    _waitClientSecond = Common.GetInt32("waitClientSecond", 600);
                }
                return _waitClientSecond;
            }
        }

        private static int _refreshDbSecond;
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
            Output("计划任务服务端启动", "start");
            Dal dbaccess = Dal.Default;
            UpdateDB(dbaccess);

            // 第一次运行，必须清空任务参数表（该表用于判断定时运行的任务结束时间）
            dbaccess.ClearTimePara();
            int second = RefreshDbSecond;
            while (true)
            {
                try
                {
                    List<TaskItem> tasks = dbaccess.GetAllTask();
                    List<Thread> threads = new List<Thread>();
                    if (tasks != null && tasks.Any())
                    {
                        Output("找到" + tasks.Count.ToString() + "个任务");

                        foreach (TaskItem task in tasks)
                        {
                            object[] para = new object[] { task, dbaccess };
                            Thread thre = new Thread(RunTask);
                            threads.Add(thre);
                            thre.Start(para);
                        }
                    }

                    // 阻塞并等待所有线程结束
                    foreach (Thread thread in threads)
                    {
                        thread.Join();
                    }
                    // 一轮完成，更新进程最后检查时间
                    dbaccess.UpdateLastRuntime();
                }
                catch (Exception exp)
                {
                    Output("Main出错", exp);
                }

                Output("休眠" + second.ToString() + "秒");
                Thread.Sleep(second * 1000);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        // 运行单个任务的主调方法
        static void RunTask(object args)
        {
            try
            {
                ExeStatus status = ExeStatus.Unknown; // 判断进程状态，以更新表
                string runpid = "";
                var argstmp = (object[])args;
                TaskItem task = (TaskItem)argstmp[0];
                Dal dbaccess = (Dal)argstmp[1];

                // 可执行文件不存在
                if (!File.Exists(task.exepath))
                {
                    status = ExeStatus.NoFile;
                    // 更新任务运行状态
                    dbaccess.UpdateTaskExeStatus(task, status, runpid);

                    var tmpmsg = task.desc + " " + task.exepath + " 文件不存在";
                    Output(tmpmsg);
                    return;
                }
                DateTime now = DateTime.Now;

                // 防止出现 c:\\\\a.exe 或 c:/a.exe这样的路径,统一格式化成：c:\a.exe形式
                task.exepath = Path.Combine(Path.GetDirectoryName(task.exepath) ?? "",
                                            Path.GetFileName(task.exepath) ?? "");

                StringBuilder msg = new StringBuilder(200);
                msg.AppendFormat(task.desc);
                // 每5分钟输出一次任务具体数据
                if (now.Minute % 5 == 0 && now.Second <= RefreshDbSecond)
                {
                    msg.AppendFormat("\r\n{0} {1} {2} {3} 已运行{4}次",
                                     task.runtype.ToString(), task.taskpara,
                                     task.exepath, task.exepara, task.runcount.ToString());
                }

                // 根据exe路径，查找运行中的进程
                var processes = ProcessItem.GetProcessByPath(task.exepath);
                var processCnt = processes.Count;

                int ret;
                switch (task.runtype)
                {
                    case RunType.Stop:

                        #region 不启动，不停止
                        if (processCnt > 0)
                        {
                            msg.Append("\r\n\t" + processCnt.ToString() + "个任务运行中，前次任务尚未完成");
                            runpid += " run pid: ";
                            foreach (ProcessItem item in processes)
                            {
                                runpid += item.pid.ToString() + ", ";
                                msg.AppendFormat("\r\n\t\t{0}", item);
                            }
                            status = ExeStatus.Running;
                        }
                        else
                        {
                            msg.Append("\r\n\t任务未启动");
                            status = ExeStatus.Stopped;
                        }
                        Output(msg);

                        #endregion

                        break;
                    case RunType.Restart:

                        #region 重启
                        // 重启后要把它设置为一直运行
                        dbaccess.UpdateTaskType(task.id, task.runtype, RunType.Always);
                        if (processCnt > 0)
                        {
                            // 杀死进程
                            ret = KillProcesses(processes);
                            msg.Append("\r\n\t已停止" + ret.ToString() + "个,");
                        }
                        else
                        {
                            msg.Append("\r\n\t进程不存在，");
                        }
                        // 启动进程
                        ret = CheckAndStartProcess(task.exepath, task.exepara);
                        if (ret > 0)
                        {
                            msg.Append("重启完成,pid:" + ret.ToString());
                            runpid += "run pid:" + ret.ToString();
                            if (processes.Count > 0)
                            {
                                runpid += ", killed: ";
                                foreach (ProcessItem item in processes)
                                {
                                    runpid += item.pid.ToString() + ",";
                                    msg.AppendFormat("\r\n\t\t{0}", item);
                                }
                            }
                        }
                        else
                        {
                            msg.Append("进程存在，未启动");
                        }

                        status = ExeStatus.Running;
                        Output(msg);

                        #endregion

                        break;
                    case RunType.StopAndWait1Min:

                        #region 停止并等1分钟重启

                        // 重启后要把它设置为一直运行
                        dbaccess.UpdateTaskType(task.id, task.runtype, RunType.Always);

                        if (processCnt > 0)
                        {
                            // 杀死进程
                            ret = KillProcesses(processes);
                            Output(task.desc + " " + task.exepath + "已停止" + ret.ToString() + "个，等1分钟后重启...");
                            runpid += "  killed: ";
                            foreach (ProcessItem item in processes)
                            {
                                runpid += item.pid.ToString() + ", ";
                                msg.AppendFormat("\r\n\t\t{0}", item);
                            }

                            msg.Append("\r\n\t已停止，等1分钟，");
                        }
                        else
                        {
                            Output(task.desc + " " + task.exepath + "未启动，等1分钟后启动...");
                            msg.Append("\r\n\t进程不存在，等1分钟，");
                        }
                        Thread.Sleep(TimeSpan.FromMinutes(1));

                        ret = CheckAndStartProcess(task.exepath, task.exepara);
                        if (ret > 0)
                        {
                            runpid += " run pid: " + ret.ToString();
                            msg.Append("\r\n\t重启完成,pid:" + ret.ToString());
                        }
                        else
                        {
                            msg.Append("\r\n\t进程存在，未启动");
                        }
                        status = ExeStatus.Running;
                        Output(msg);

                        #endregion

                        break;
                    case RunType.ForceStop:

                        #region 强行停止

                        // 停止后要把它设置为不启动状态
                        dbaccess.UpdateTaskType(task.id, task.runtype, RunType.Stop);

                        if (processCnt > 0)
                        {
                            // 杀死进程
                            ret = KillProcesses(processes);
                            msg.Append("\r\n\t停止完成" + ret.ToString() + "个");
                            runpid += " killed: ";
                            foreach (ProcessItem item in processes)
                            {
                                runpid += item.pid.ToString() + ",";
                                msg.AppendFormat("\r\n\t\t{0}", item);
                            }
                        }
                        else
                        {
                            msg.Append("\r\n\t任务未启动");
                        }
                        status = ExeStatus.Stopped;
                        Output(msg);

                        #endregion

                        break;
                    case RunType.Always:
                    case RunType.OneTime:

                        #region 一直运行 或 只运行一次

                        // 查找进程是否运行中
                        msg.Append("\r\n\t");
                        if (task.runtype == RunType.OneTime)
                        {
                            // 更新为停止
                            dbaccess.UpdateTaskType(task.id, task.runtype, RunType.Stop);
                            msg.Append("(只运行一次)");
                        }

                        if (processCnt <= 0)
                        {
                            var lastRunMin = (now - task.pidtime).TotalMinutes;
                            // 一直运行的，每2次启动必须间隔1分钟
                            if (task.runtype == RunType.OneTime || lastRunMin > 1)
                            {
                                // 启动进程
                                var pid = CheckAndStartProcess(task.exepath, task.exepara);
                                if (pid > 0)
                                {
                                    runpid += " run pid:" + pid.ToString();
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
                            }
                        }
                        else
                        {
                            msg.Append(processCnt.ToString() + "个任务正运行中");
                            runpid += " run pid:";
                            foreach (ProcessItem item in processes)
                            {
                                runpid += item.pid.ToString() + ",";
                                msg.AppendFormat("\r\n\t\t{0}", item);
                            }
                        }

                        status = ExeStatus.Running;
                        Output(msg);

                        #endregion

                        break;
                    case RunType.PerDay:
                    case RunType.PerWeek:
                    case RunType.PerMonth:

                        #region 定时运行

                        // 未设置定时运行参数时
                        if (!task.TaskPara.Any())
                        {
                            status = ExeStatus.NoPara;
                            // 更新任务运行状态
                            dbaccess.UpdateTaskExeStatus(task, status, runpid);

                            msg.Append("\r\n\t任务参数未配置，运行失败");
                            Output(msg.ToString());
                            return;
                        }
                        bool? isrun = null;

                        if (processCnt <= 0)
                        {
                            status = ExeStatus.Stopped;
                        }
                        else
                        {
                            runpid += " run pid:";
                            foreach (ProcessItem item in processes)
                            {
                                runpid += item.pid.ToString() + ",";
                                msg.AppendFormat("\r\n\t\t{0}", item);
                            }

                            status = ExeStatus.Running;
                        }

                        StringBuilder sbEndTime = new StringBuilder();
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
                                timepara.StartTime = dbaccess.GetTimePara(task.id, timepara);
                                if (timepara.StartTime == default(DateTime))
                                {
                                    timepara.StartTime = starttime;
                                }
                                else
                                {
                                    isTimeParaSeted = true;
                                }
                                endtime = timepara.StartTime.AddMinutes(timepara.RunMinute);

                                // 计算结束时间
                                //// 不能用now.Day来作为starttime，比如22点启动，运行4小时，应该是次日的2点结束，此时day应该是前一天
                                //endtime = starttime.AddMinutes(timepara.RunMinute);
                                //int runDay = CountEndDay(timepara.StartHour, timepara.StartMin, timepara.RunMinute);
                                //endtime = endtime.AddDays(-runDay);

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
                                    dbaccess.DelTimePara(task.id, timepara);
                                }
                                dbaccess.AddTimePara(task.id, timepara);

                                if (processCnt <= 0)
                                {
                                    var lastRunMin = (now - task.pidtime).TotalMinutes;
                                    if (lastRunMin > 1)
                                    {
                                        // 启动进程
                                        var pid = CheckAndStartProcess(task.exepath, task.exepara);
                                        if (pid > 0)
                                        {
                                            runpid += " run pid:" + pid.ToString();
                                            msg.Append("任务成功启动,pid:" + pid.ToString());
                                        }
                                        else
                                        {
                                            msg.Append("任务存在，启动失败");
                                        }
                                    }
                                    else
                                    {
                                        msg.Append("\r\n\t任务1分钟只能启动1次");
                                    }
                                }
                                else
                                {
                                    msg.Append("\r\n\t" + processCnt.ToString() + "个任务正运行中");
                                }
                                // 记录之，用于计算结束时间
                                if (timepara.RunMinute > 0)
                                    timepara.StartTime = now;

                                status = ExeStatus.Running;
                                // 不用break，是为了统计并输出结束时间日志
                                continue;
                            }
                            // now比endtime大，且在1分钟之内，停止它
                            if (endtime <= now && (now - endtime).TotalSeconds < 60 && processCnt > 0)
                            {
                                isrun = false;
                                // 结束完成，删除 开始时间记录，以便下次轮询重新计算
                                if (isTimeParaSeted)
                                    dbaccess.DelTimePara(task.id, timepara);

                                var killNum = KillProcesses(processes);
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
                                dbaccess.DelTimePara(task.id, timepara);
                            }
                        }
                        if (isrun == null)
                        {
                            msg.Append("\r\n\t时间没到 " + (status == ExeStatus.Stopped ? "任务未启动" : "任务运行中"));
                        }
                        if (sbEndTime.Length > 0)
                            msg.Append("\r\n\t起止时间:" + sbEndTime);
                        Output(msg);

                        #endregion

                        break;
                }

                // 更新任务运行状态
                var processesLater = ProcessItem.GetProcessByPath(task.exepath);
                var newpid = processesLater.Count > 0 ? processesLater[0].pid : 0;
                if (newpid > 0 || task.pid != 0)
                {
                    // 更新任务的pid
                    dbaccess.UpdateTaskProcessId(task.id, newpid);
                }
                var oldpid = processes.Count > 0 ? processes[0].pid : 0;

                var needlog = processesLater.Count != processCnt || oldpid != newpid;
                if (!needlog)
                {
                    needlog = task.pid != newpid;
                }

                //var needlog = task.runtype == RunType.Restart || task.runtype == RunType.StopAndWait1Min || task.runtype == RunType.ForceStop || task.runtype == RunType.OneTime;
                dbaccess.UpdateTaskExeStatus(task, status, runpid, needlog);
            }
            catch (Exception ex)
            {
                Output("运行任务时错误", ex);
            }
        }


        /// <summary>
        /// 服务启动时，检测有没有数据库变更脚本要执行
        /// </summary>
        static void UpdateDB(Dal dbaccess)
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
                Output("UpdateDB 读取文件错误:" + _dbModityFile, exp);
                return;
            }
            foreach (string sql in allsql.Split(';'))
            {
                try
                {
                    var ret = dbaccess.ExecuteSql(sql);
                    Output(ret.ToString() + "行\r\nsql:" + sql, "updatedb");
                }
                catch (Exception exp)
                {
                    Output("UpdateDB sql错误:" + sql, exp);
                    break;
                }
            }
            try
            {
                File.Move(_dbModityFile, _dbModityFile + DateTime.Now.ToString("yyyyMMddHHmmss"));
            }
            catch (Exception exp)
            {
                Output("UpdateDB 移动文件错误:" + _dbModityFile, exp);
            }
        }


        /*
        // 根据exe程序物理路径杀进程，并返回杀死进程个数(如果同一exe启动多次，会被全部杀死)
        // noCompareProcess不比较的进程名列表
        // win32ExpProcess本次比较过程中出错的进程名列表
        static int KillProcessByPath(string exePath, Dictionary<string, bool> noCompareProcess,
            Dictionary<string, bool> win32ExpProcess)
        {
            int ret = 0;
    
            Process[] allProcess = Process.GetProcesses();
            foreach (Process proc in allProcess)
            {
                string procName = proc.ProcessName.ToLower();
                if (noCompareProcess != null && noCompareProcess.ContainsKey(procName))
                    continue;
                try
                {
                    if (exePath.Equals(proc.MainModule.FileName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!proc.CloseMainWindow())
                        {
                            proc.Kill();
                        }
                        ret++;
                    }
                    //if (proc.Id != 0 && proc.MainWindowHandle.ToInt32() != 0)
                    //{
                    //    // 如果有主窗口
                    //    ProcessModule pm = proc.MainModule;
                    //    String modname = pm.ModuleName;
                    //}
                }
                catch (Win32Exception)//System.ComponentModel.Win32Exception: 拒绝访问。
                {
                    if (win32ExpProcess != null)
                    {
                        win32ExpProcess[procName] = false;
                    }
                }
                catch (Exception exp)
                {
                    // 有些进程无法枚举进程模块
                    Output("KillProcessByPath出错 " + procName, exp);
                }
            }
            return ret;
            //GC.Collect();
        }
        */

        // 不能根据进程名杀，可能杀错，比如一个程序放在2个目录下
        // 要根据exe程序物理路径杀进程，并返回杀死进程个数(如果同一exe启动多次，会被全部杀死) 通过Win32_Process查询到id后杀死
     

        /// <summary>
        /// 杀死指定的进程列表
        /// </summary>
        /// <param name="processes"></param>
        /// <returns></returns>
        static int KillProcesses(List<ProcessItem> processes)
        {
            var ret = 0;
            foreach (ProcessItem process in processes)
            {
                var procName = process.name;
                try
                {
                    KillProcessByPid(process.pid);
                    ret++;
                }
                catch (Exception exp)
                {
                    Output("KillProcessByPid出错 " + procName, exp);
                }
            }
            return ret;
        }

        // 根据进程id杀进程
        // ReSharper disable once UnusedMethodReturnValue.Local
        private static bool KillProcessByPid(int pid)
        {
            using (var process = FindProcessByPid(pid))
            {
                if (process != null && !process.CloseMainWindow())
                {
                    process.Kill();
                    return true;
                }
            }
            return false;
        }

        /*
        // noCompareProcess不比较的进程名列表
        // win32ExpProcess本次比较过程中出错的进程名列表
        [Obsolete("Cann't get process exe path, Please use FindProcessByPath(string exePath) instead.")]
        static int FindProcessByPath(string exePath, Dictionary<string, bool> noCompareProcess,
            Dictionary<string, bool> win32ExpProcess)
        {
            int ret = 0;
            Process[] allProcess = Process.GetProcesses();
            foreach (Process proc in allProcess)
            {
                string procName = proc.ProcessName.ToLower();
                if (noCompareProcess != null && noCompareProcess.ContainsKey(procName))
                    continue;
                try
                {
                    if (exePath.Equals(proc.MainModule.FileName, StringComparison.OrdinalIgnoreCase))
                    {
                        ret++;
                    }
                }
                catch (Win32Exception)//System.ComponentModel.Win32Exception: 拒绝访问。
                {
                    if(win32ExpProcess != null)
                    {
                        win32ExpProcess[procName] = false;
                    }
                }
                catch (Exception exp)
                {
                    // 有些进程无法枚举进程模块
                    Output("FindProcessByPath出错 " + procName, exp);
                }
            }
            return ret;
        }
        */
        
        
        static Process FindProcessByPid(int pid)
        {
            return Process.GetProcessById(pid);
        }


        /// <summary>
        /// 启动进程的锁，避免同时启动多个进程
        /// </summary>
        private static object _objStartLock = new object();

        /// <summary>
        /// 进程不存在时，启动指定的任务
        /// </summary>
        /// <param name="exepath"></param>
        /// <param name="exepara"></param>
        /// <returns></returns>
        static int CheckAndStartProcess(string exepath, string exepara)
        {
            lock (_objStartLock)
            {
                if (ProcessItem.Exists(exepath))
                {
                    return 0;
                }
                return StartProcess(exepath, exepara);
            }
        }

        // 启动指定的任务
        static int StartProcess(string exepath, string exepara)
        {
            var p = new Process();
            p.StartInfo.FileName = exepath;
            p.StartInfo.UseShellExecute = true; // 在当前进程中启动，不使用系统外壳程序启动
            //p.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;// 让dos窗体最大化
            p.StartInfo.Arguments = exepara; //设定参数，"/C"表示执行完命令后马上退出
            p.StartInfo.RedirectStandardInput = false; //设置为true，后面可以通过StandardInput输入dos命令
            p.StartInfo.RedirectStandardOutput = false;
            p.StartInfo.CreateNoWindow = true;     //不创建窗口
            p.Start();
            //SetWindowPos(p.Handle, 3, Left, Top, Width, Height, 8);
            //p.StandardInput.WriteLine("ping " + url);
            //p.WaitForExit(1000);
            //MessageBox.Show(p.StandardOutput.ReadToEnd());
            //p.Close();
            return p.Id;
        }
        #endregion


        #region 获取Socket请求并执行操作
        // Socket主调方法
        /// <summary>
        /// 服务器端口监听，得到请求时的相应处理
        /// </summary>
        /// <param name="msg">接收到的消息</param>
        /// <param name="sendOrRecievedFilePath">
        /// 传入参数不为空时，表示通过Socket接收到了文件，比如文件上传操作。
        /// 传入参数为空时，此参数可以用于发送文件，比如下载单个或多个文件
        /// </param>
        /// <returns></returns>
        public static string ServerOperation(string msg, ref string sendOrRecievedFilePath)
        {
            string strType; // 数字字符串形式的操作类型
            int type; // 操作类型
            string strArgs; // 操作用到的参数

            #region 参数验证

            if (msg == null)
                return "err未传递参数";

            int splitIdx = msg.IndexOf('_');
            // 第一位或最后一位是第一个分隔符时
            if (splitIdx <= 0 || splitIdx >= msg.Length - 1)
                return "err无效的参数";
            string checkcode = msg.Substring(0, splitIdx);

            msg = msg.Substring(splitIdx + 1);
            splitIdx = msg.IndexOf('_');
            if (splitIdx > 0)
            {
                strType = msg.Substring(0, splitIdx);
                strArgs = splitIdx < msg.Length - 1 ? msg.Substring(splitIdx + 1) : string.Empty;
            }
            else
            {
                strType = msg;
                strArgs = string.Empty;
            }

            //验证CheckCode
            string checkCount = Common.GetCheckCode(strType, strArgs);
            if (checkcode != checkCount)
            {
                return "err验证失败";
            }

            if (!int.TryParse(strType, out type))
                return "err无效的操作类型" + strType;

            #endregion

            if (Common.EnableFileAdmin)
            {
                #region 所有文件管理分支逻辑

                switch ((OperationType)type)
                {
                    case OperationType.DirShow:
                        return DirShow(strArgs);
                    case OperationType.DirMove:
                        return DirMove(strArgs);
                    case OperationType.DirDel:
                        return DirDel(strArgs);
                    case OperationType.DirCreate:
                        return DirCreate(strArgs);
                    case OperationType.DirRename:
                        return DirRename(strArgs, true);
                    case OperationType.FileRename:
                        return DirRename(strArgs, false);
                    case OperationType.FileUnZip:
                        return FileUnZip(strArgs);
                    case OperationType.DirSizeGet:
                        return DirSizeGet(strArgs);

                    case OperationType.DirDownloadZip:
                        return DirDownloadZip(strArgs, out sendOrRecievedFilePath);
                    case OperationType.FileDownload:
                        return FileDownload(strArgs, out sendOrRecievedFilePath);
                    case OperationType.FileUpload:
                        return FileUpload(strArgs, sendOrRecievedFilePath);
                }

                #endregion
            }

            try
            {
                // 计划管理分支逻辑
                switch ((OperationType)type)
                {
                    default:
                        return "err不存在的操作类型:" + type.ToString();

                    case OperationType.GetAllTasks:
                        return GetAllTask();
                    case OperationType.DelTasks:
                        return DelTasks(strArgs);
                    case OperationType.SaveTasks:
                        return SaveTasks(strArgs);
                    case OperationType.TaskLog:
                        return ShowTaskLog(strArgs);
                    case OperationType.Immediate:
                        return ImmediateProcess(strArgs);

                    case OperationType.RunMethod:
                        return LoadAndRunMethod(strArgs);

                    case OperationType.GetProcesses:
                        return GetProcesses();
                }
            }
            catch (Exception exp)
            {
                return strArgs + "\r\nErr:" + exp;
            }
        }


        #region 任务相关操作
        static string GetAllTask()
        {
            Dal dbaccess = Dal.Default;
            List<TaskItem> tasks = dbaccess.GetAllTask();
            if (tasks == null)
                return "err未知错误";
            string lastRunTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + dbaccess.GetLastRuntime();
            return lastRunTime + "|" + Common.XmlSerializeToStr(tasks);
        }

        static string DelTasks(string strArgs)
        {
            string[] args = strArgs.Split('_');
            if (args.Length <= 0)
            {
                return "err未提交id";
            }
            Dal dbaccess = Dal.Default;
            int delcnt = 0;
            foreach (string strid in args)
            {
                int id;
                if (!string.IsNullOrEmpty(strid) &&
                    int.TryParse(strid, out id) && dbaccess.DelTaskById(id))
                    delcnt++;
            }
            return "成功删除" + delcnt + "条记录";
        }

        static string SaveTasks(string strArgs)
        {
            if (strArgs == string.Empty)
                return "err未提交任务数据";
            Dal dbaccess = Dal.Default;
            var tasks = Common.XmlDeserializeFromStr<List<TaskItem>>(strArgs);
            foreach (TaskItem task in tasks)
            {
                if (task.id <= 0)
                    dbaccess.AddTask(task);
                else
                    dbaccess.UpdateTask(task);

            }
            return GetAllTask();
        }

        static string ShowTaskLog(string exepath)
        {
            if (exepath == string.Empty)
                return "err未提交任务数据";
            Dal dbaccess = Dal.Default;
            var ret = dbaccess.FindTaskLog(exepath);
            return Common.XmlSerializeToStr(ret);
        }

        /// <summary>
        /// 对程序立即进行的启动或停止操作
        /// </summary>
        /// <param name="strArgs"></param>
        /// <returns></returns>
        static string ImmediateProcess(string strArgs)
        {
            // string args = ((int) type).ToString() + "\n" + path + "\n" + exepara;
            string[] args = strArgs.Split('\n');
            if (args.Length < 3)
            {
                return "参数不足3个";
            }
            int imtype;
            if (!int.TryParse(args[0], out imtype))
            {
                return "无效的临时类型";
            }
            string exepath = args[1];
            // 防止出现 c:\\\\a.exe 或 c:/a.exe这样的路径,统一格式化成：c:\a.exe形式
            exepath = Path.Combine(Path.GetDirectoryName(exepath) ?? "", Path.GetFileName(exepath) ?? "");
            if (!File.Exists(exepath))
            {
                return "文件不存在:" + exepath;
            }

            var processes = ProcessItem.GetProcessByPath(exepath);

            string exepara = args[2];
            int ret;
            switch ((ImmediateType)imtype)
            {
                default:
                    return "不存在的临时类型";
                case ImmediateType.Start:
                    // 查找进程是否运行中，不在则启动
                    ret = CheckAndStartProcess(exepath, exepara);
                    if (ret > 0)
                    {
                        return exepath + " 成功启动, pid:" + ret.ToString();
                    }
                    else
                    {
                        return exepath + " 运行中，无需启动";
                    }
                case ImmediateType.Stop:
                    ret = KillProcesses(processes);
                    if (ret > 0)
                    {
                        return exepath + " 成功关闭个数:" + ret.ToString();
                    }
                    else
                    {
                        return exepath + " 未运行，无需停止";
                    }
                case ImmediateType.ReStart:
                    string restartMsg;
                    // 杀死进程
                    ret = KillProcesses(processes);
                    if (ret > 0)
                    {
                        restartMsg = exepath + " 成功关闭个数:" + ret.ToString();
                    }
                    else
                    {
                        restartMsg = exepath + " 未启动";
                    }
                    // 查找进程是否运行中，不在则启动
                    ret = CheckAndStartProcess(exepath, exepara);
                    if (ret > 0)
                    {
                        return restartMsg + " 重启完成,pid:" + ret.ToString();
                    }
                    else
                    {
                        return restartMsg + " 进程已存在";
                    }
            }
        }
        #endregion

        #region 其它操作
        /// <summary>
        /// 执行dll里的类方法，该方法必须是静态，且无参数
        /// </summary>
        /// <param name="args">命名空间.类名.静态方法名,dll路径</param>
        /// <returns></returns>
        static string LoadAndRunMethod(string args)
        {
            if (args == null || (args = args.Trim()) == string.Empty)
                return "参数为空";
            int idx1 = args.IndexOf(',');// 第2个参数的起始位置
            if (idx1 < 0 || idx1 + 1 == args.Length)
            {
                return "必须提供dll路径";
            }

            string path = AppDomain.CurrentDomain.BaseDirectory;
            string dllpath;
            string className;
            string methodName;
            string methodPara = null;
            //string[] arr = args.Split(',');

            string para1 = args.Substring(0, idx1).Trim();
            int idx2 = args.IndexOf(',', idx1 + 1);// 第3个参数的起始位置
            if (idx2 < 0)
            {
                dllpath = args.Substring(idx1 + 1).Trim();
            }
            else
            {
                dllpath = args.Substring(idx1 + 1, idx2 - idx1 - 1).Trim();
                if (idx2 + 1 < args.Length)
                {
                    methodPara = args.Substring(idx2 + 1);
                }
            }
            if (string.IsNullOrEmpty(dllpath))
            {
                return "必须提供dll路径..";
            }

            int methodStart = para1.LastIndexOf('.');
            // 没点，或第一个是点，或最后一位是点，退出
            if (methodStart <= 0 || methodStart == para1.Length - 1)
                return "参数有误";

            className = para1.Substring(0, methodStart).Trim();
            methodName = para1.Substring(methodStart + 1).Trim();


            if (dllpath.IndexOf(':') != 1)// c:\a.dll，冒号在第二位
            {
                // 没提供物理路径时
                dllpath = Path.Combine(path, dllpath);
            }
            if (!dllpath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                dllpath += ".dll";


            if (string.IsNullOrEmpty(dllpath) || string.IsNullOrEmpty(dllpath) || string.IsNullOrEmpty(dllpath))
            {
                return "未输入dll相关信息";
            }
            if (!File.Exists(dllpath))
            {
                return "dll文件不存在:" + dllpath;
            }

            try
            {
                // 直接LoadFile会导致这个dll无法释放，不考虑
                //Assembly dll = Assembly.LoadFile(dllpath);
                //if (dll == null)
                //{
                //    return "加载dll失败:" + dllpath;
                //}

                using (FileStream stream = new FileStream(dllpath, FileMode.Open))
                using (MemoryStream memStream = new MemoryStream())
                {
                    byte[] b = new byte[4096];
                    while (stream.Read(b, 0, b.Length) > 0)
                    {
                        memStream.Write(b, 0, b.Length);
                    }
                    Assembly dll = Assembly.Load(memStream.ToArray());//ReflectionOnlyLoad
                    Type type = dll.GetType(className);
                    if (type == null)
                    {
                        return "加载类型失败:" + className;
                    }
                    MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
                    if (method == null)
                    {
                        return "获取公共静态方法失败:" + className + " 方法:" + methodName;
                    }
                    var invokePara = methodPara == null ? null : new object[] { methodPara };
                    return Convert.ToString(method.Invoke(null, invokePara));
                }
            }
            catch (Exception exp)
            {
                return exp.Message;
            }
        }

        /// <summary>
        /// 返回所有进程信息
        /// </summary>
        /// <returns></returns>
        static string GetProcesses()
        {
            var ret = new StringBuilder(10000);
            List<ProcessItem> processes = ProcessItem.GetProcessesAndCache();
            // 按名称排序
            foreach (var process in processes.OrderBy(item => item.name))
            {
                ret.AppendFormat("{0}|||{1}|||{2}|||{4}|||{3}|||{5}|||{6}|/|/|/",
                    process.pid.ToString(),
                    process.name,
                    process.memory.ToString(),
                    process.memoryVirtual.ToString(),
                    process.memoryPage.ToString(),
                    process.createDate,
                    process.commandLine);

            }
            return ret.ToString();
        }
        #endregion

        #region 文件管理相关操作
        /// <summary>
        /// 返回指定目录下所有子目录和文件
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static string DirShow(string args)
        {
            if (string.IsNullOrEmpty(args))
                return "err未提供参数";

            string[] arrArgs = args.Split('|');
            string dirPath = arrArgs[0];//.Trim();
            if (dirPath == "")
                return "err目录未提供";
            if (!Directory.Exists(dirPath))
                return "err" + dirPath + "目录不存在";

            SortType sort = SortType.Name;
            if (arrArgs.Length > 1)
            {
                int tmpSort;
                if (int.TryParse(arrArgs[1], out tmpSort))
                    sort = (SortType)tmpSort;
            }

            // 返回文件前是否要计算MD5
            bool showMd5 = arrArgs.Length > 2 && arrArgs[2] == "1";

            if (!dirPath.EndsWith("\\"))
                dirPath += "\\";
            DirectoryInfo dirShow = new DirectoryInfo(dirPath);

            DirectoryInfo[] arrDir;
            FileInfo[] arrFile;
            try
            {
                arrDir = dirShow.GetDirectories("*");
                arrFile = dirShow.GetFiles("*.*");
            }
            catch (Exception exp)
            {
                return "err" + dirPath + " 子目录或文件列表获取失败\r\n" + exp;
            }

            #region 排序

            Array.Sort(arrDir, delegate (DirectoryInfo a, DirectoryInfo b)
            {
                switch (sort)
                {
                    default:// 目录名正序
                        return String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                    case SortType.NameDesc:
                        return -String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                    case SortType.ModifyTime:
                        return a.LastWriteTime.CompareTo(b.LastWriteTime);
                    case SortType.ModifyTimeDesc:
                        return -a.LastWriteTime.CompareTo(b.LastWriteTime);
                }
            });
            Array.Sort(arrFile, delegate (FileInfo a, FileInfo b)
            {
                switch (sort)
                {
                    default: // 文件名正序
                        return String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                    case SortType.NameDesc:
                        return -String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                    case SortType.Extention:
                        return String.Compare(Path.GetExtension(a.Name), Path.GetExtension(b.Name), StringComparison.OrdinalIgnoreCase);
                    case SortType.ExtentionDesc:
                        return -String.Compare(Path.GetExtension(a.Name), Path.GetExtension(b.Name), StringComparison.OrdinalIgnoreCase);
                    case SortType.Size:
                        return a.Length.CompareTo(b.Length);
                    case SortType.SizeDesc:
                        return -a.Length.CompareTo(b.Length);
                    case SortType.ModifyTime:
                        return a.LastWriteTime.CompareTo(b.LastWriteTime);
                    case SortType.ModifyTimeDesc:
                        return -a.LastWriteTime.CompareTo(b.LastWriteTime);
                }
            });
            #endregion

            var dirs = arrDir.Select(info => new FileItem()
            {
                Name = info.Name,
                IsFile = false,
                LastModifyTime = info.LastWriteTime,
            });
            var files = arrFile.Select(info => new FileItem()
            {
                Name = info.Name,
                IsFile = true,
                LastModifyTime = info.LastWriteTime,
                Size = info.Length,
                FileMd5 = showMd5 ? Common.GetFileMD5(info.FullName) : "",
            });

            var ret = new FileResult()
            {
                Dir = dirPath,
                SubDirs = dirs.ToArray(),
                SubFiles = files.ToArray(),
                ServerTime = DateTime.Now,
                ServerIp = Common.GetServerIpList(),
                Others = GetDriveInfo(dirPath),
            };
            return Common.XmlSerializeToStr(ret);
        }

        /// <summary>
        /// 移动指定目录下的指定的子目录和文件
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static string DirMove(string args)
        {
            if (string.IsNullOrEmpty(args))
                return "err未提供参数";

            string[] arrArgs = args.Split('|');
            if (arrArgs.Length < 4)
                return "err参数不足";
            string dirPath = arrArgs[0];//.Trim();
            if (dirPath == "")
                return "err目录未提供";
            if (!Directory.Exists(dirPath))
                return "err" + dirPath + "目录不存在";

            string dirTo = arrArgs[1];
            if (dirPath == "")
                return "err目录未提供";
            if (!dirPath.EndsWith("\\"))
                dirPath += "\\";

            if (!Directory.Exists(dirTo))
                Directory.CreateDirectory(dirTo);
            if (!dirTo.EndsWith("\\"))
                dirTo += "\\";

            string[] files = arrArgs[2].Split('*');
            string[] dirs = arrArgs[3].Split('*');
            int fileCnt = 0;
            int dirCnt = 0, dirFileCnt = 0;

            foreach (string file in files)
            {
                if (string.IsNullOrEmpty(file))
                    continue;
                string mf = Path.Combine(dirPath, file);
                if (!File.Exists(mf))
                    continue;
                string to = Path.Combine(dirTo, file);
                if (File.Exists(to))
                    File.Delete(to);
                File.Move(mf, to);
                fileCnt++;
            }

            foreach (string dir in dirs)
            {
                if (string.IsNullOrEmpty(dir))
                    continue;
                string mf = Path.Combine(dirPath, dir);
                if (!Directory.Exists(mf))
                    continue;
                dirCnt++;
                string to = Path.Combine(dirTo, dir);
                string msg = string.Empty;
                int dirFiles = DirMove(mf, to, ref msg);
                if (dirFiles < 0)
                    return msg;
                dirFileCnt += dirFiles;
            }
            return fileCnt.ToString() + "|" + dirCnt.ToString() + "|" + dirFileCnt.ToString();
        }


        /// <summary>
        /// 移动单个目录
        /// </summary>
        /// <param name="dirFrom">要移动的目录</param>
        /// <param name="dirTo">移动到的父目录</param>
        /// <param name="msg">出错信息</param>
        /// <returns></returns>
        static int DirMove(string dirFrom, string dirTo, ref string msg)
        {
            int cntFile = 0;
            if (!Directory.Exists(dirFrom))
            {
                msg = "err" + dirFrom + "目录不存在";
                return -1;
            }
            try
            {
                // 判断目标目录是否存在，不存在则创建
                if (!Directory.Exists(dirTo))
                    Directory.CreateDirectory(dirTo);

                DirectoryInfo objDir = new DirectoryInfo(dirFrom);
                FileSystemInfo[] sfiles = objDir.GetFileSystemInfos();
                if (sfiles.Length > 0)
                {
                    foreach (FileSystemInfo t1 in sfiles)
                    {
                        string movName = Path.GetFileName(t1.FullName);
                        if (t1.Attributes == FileAttributes.Directory)
                        {
                            // 递归移动子目录
                            int tmp = DirMove(t1.FullName, Path.Combine(dirTo, movName), ref msg);
                            if (tmp == -1)
                                return -1;
                            cntFile += tmp;
                        }
                        else
                        {
                            string to = Path.Combine(dirTo, movName);
                            if (File.Exists(to))
                                File.Delete(to);
                            File.Move(t1.FullName, to);
                            cntFile++;
                        }
                    }
                }
                // 删除当前目录
                Directory.Delete(dirFrom);
            }
            catch (Exception exp)
            {
                msg = "err" + dirFrom + " 目录移动失败\r\n<br />\r\n" + exp;
                return -1;
            }
            return cntFile;
        }

        /// <summary>
        /// 删除子目录或文件
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static string DirDel(string args)
        {
            if (string.IsNullOrEmpty(args))
                return "err未提供参数";

            string[] arrArgs = args.Split('|');
            if (arrArgs.Length < 3)
                return "err参数不足";
            string dirPath = arrArgs[0];//.Trim();
            if (dirPath == "")
                return "err目录未提供";
            if (!dirPath.EndsWith("\\"))
                dirPath += "\\";
            if (!Directory.Exists(dirPath))
                return "err" + dirPath + "目录不存在";

            string[] files = arrArgs[1].Split('*');
            string[] dirs = arrArgs[2].Split('*');
            int fileCnt = 0;
            int dirCnt = 0;

            foreach (string file in files)
            {
                if (string.IsNullOrEmpty(file))
                    continue;
                string mf = Path.Combine(dirPath, file);
                if (!File.Exists(mf))
                    continue;
                File.Delete(mf);
                fileCnt++;
            }

            foreach (string dir in dirs)
            {
                if (string.IsNullOrEmpty(dir))
                    continue;
                string mf = Path.Combine(dirPath, dir);
                if (!Directory.Exists(mf))
                    continue;
                dirCnt++;
                Directory.Delete(mf, true);
            }
            return fileCnt.ToString() + "|" + dirCnt.ToString();
        }

        /// <summary>
        /// 创建新目录
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static string DirCreate(string args)
        {
            if (string.IsNullOrEmpty(args))
                return "err未提供参数";
            string newdir = args;
            if (Directory.Exists(newdir))
                return newdir + "目录已存在";
            Directory.CreateDirectory(newdir);
            return "创建成功";
        }

        /// <summary>
        /// 目录改名
        /// </summary>
        /// <param name="args"></param>
        /// <param name="isdir"></param>
        /// <returns></returns>
        static string DirRename(string args, bool isdir)
        {
            if (string.IsNullOrEmpty(args))
                return "err未提供参数";
            string[] arrArgs = args.Split('|');
            if (arrArgs.Length < 2)
                return "err参数不足";

            string nameOld = arrArgs[0];
            if (isdir && !Directory.Exists(nameOld))
                return nameOld + "目录不存在";
            else if (!isdir && !File.Exists(nameOld))
                return nameOld + "文件不存在";
            // ReSharper disable AssignNullToNotNullAttribute
            string nameNew = Path.Combine(Path.GetDirectoryName(nameOld), arrArgs[1]);
            // ReSharper restore AssignNullToNotNullAttribute
            if (File.Exists(nameNew) || Directory.Exists(nameNew))
            {
                return nameNew + " 同名文件或目录已经存在";
            }
            if (isdir)
                Directory.Move(nameOld, nameNew);
            else
                File.Move(nameOld, nameNew);
            return "改名成功";
        }

        /// <summary>
        /// 对指定的zip文件进行解压
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static string FileUnZip(string args)
        {
            if (string.IsNullOrEmpty(args))
                return "err未提供参数";
            string[] arrArgs = args.Split('|');
            if (arrArgs.Length < 2)
                return "参数不足";

            string zipName = arrArgs[0];
            if (!File.Exists(zipName))
                return zipName + "文件不存在";
            string unzipDir = arrArgs[1];

            Common.UnZipFile(zipName, unzipDir);
            return "解压成功";
        }

        /// <summary>
        /// 获取指定目录大小
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static string DirSizeGet(string args)
        {
            if (string.IsNullOrEmpty(args))
                return "err未提供参数";
            string dir = args;
            if (!Directory.Exists(dir))
                return dir + "目录不存在";

            int cntFile = 0;
            int cntDir = 0;
            long size = GetDirSize(dir, ref cntFile, ref cntDir);
            return size.ToString() + "|" + cntDir.ToString() + "|" + cntFile.ToString();
        }
        /// <summary>
        /// 获取指定目录大小
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="cntFile">文件个数</param>
        /// <param name="cntDir">目录个数</param>
        static long GetDirSize(string dir, ref int cntFile, ref int cntDir)
        {
            long ret = 0;
            // 递归访问全部子目录
            foreach (string subdir in Directory.GetDirectories(dir))
            {
                ret += GetDirSize(subdir, ref cntFile, ref cntDir);
                cntDir++;
            }
            // 访问全部文件
            foreach (string subfile in Directory.GetFiles(dir))
            {
                ret += new FileInfo(subfile).Length;
                cntFile++;
            }
            return ret;
        }


        /// <summary>
        /// 下载指定的单个文件
        /// </summary>
        /// <param name="args">要下载的文件路径</param>
        /// <param name="sendFilePath">要下载的文件路径，赋值给外部委托调用时使用</param>
        /// <returns></returns>
        static string FileDownload(string args, out string sendFilePath)
        {
            sendFilePath = null;
            if (string.IsNullOrEmpty(args))
                return "err未提供参数";
            string downFileName = args;
            if (!File.Exists(downFileName))
                return "err指定的文件不存在";
            sendFilePath = downFileName;
            return "ok";
        }

        /// <summary>
        /// 打包下载多个文件和目录
        /// </summary>
        /// <param name="args"></param>
        /// <param name="sendFilePath">打包后待下载的文件路径，赋值给外部委托调用时使用</param>
        /// <returns></returns>
        static string DirDownloadZip(string args, out string sendFilePath)
        {
            sendFilePath = null;
            if (string.IsNullOrEmpty(args))
                return "err未提供参数";
            string[] arrArgs = args.Split('|');
            if (arrArgs.Length < 3)
                return "err参数不足";
            string dirPath = arrArgs[0];//.Trim();
            if (dirPath == "")
                return "err目录未提供";
            if (!dirPath.EndsWith("\\"))
                dirPath += "\\";
            if (!Directory.Exists(dirPath))
                return "err" + dirPath + "目录不存在";

            string[] files = arrArgs[1].Split('*');
            string[] dirs = arrArgs[2].Split('*');
            List<string> arr = new List<string>(dirs);
            arr.AddRange(files);
            string zipName = Path.Combine(SocketCommon.TmpDir, DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".zip");
            Common.ZipDirs(zipName, dirPath, arr.ToArray());
            sendFilePath = zipName;
            return "ok";
        }

        /// <summary>
        /// 上传单个文件
        /// </summary>
        /// <param name="args"></param>
        /// <param name="recievedFilePath">接收到的临时文件全路径</param>
        /// <returns></returns>
        static string FileUpload(string args, string recievedFilePath)
        {
            if (string.IsNullOrEmpty(args))
                return "err未提供参数";
            string[] arrArgs = args.Split('|');
            if (arrArgs.Length < 2)
                return "err未提供参数不足";
            string dir = arrArgs[0];
            if (!Directory.Exists(dir))
                return "err指定的上传目录不存在";
            string savePath = Path.Combine(arrArgs[0], arrArgs[1]);
            if (Directory.Exists(savePath) || File.Exists(savePath))
                return "err指定的文件名已存在";
            if (!File.Exists(recievedFilePath))
                return "err未获利上传文件";
            File.Move(recievedFilePath, savePath);
            return "ok";
        }

        /// <summary>
        /// 返回指定目录的分区信息
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        static string GetDriveInfo(string dirPath)
        {
            DriveInfo info = new DriveInfo(dirPath);
            return string.Format("{2} free/total:{0}/{1}",
                CountSize(info.AvailableFreeSpace),
                CountSize(info.TotalSize),
                info.DriveFormat);
        }
        static string CountSize(long size)
        {
            if (size <= 0)
                return "0B";
            string[] unit = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            int idxUnit = 0;
            double dsize = size;
            while (dsize >= 1024 && idxUnit < unit.Length - 1)
            {
                dsize = dsize / 1024;
                idxUnit++;
            }
            string showsize = dsize.ToString("F2").TrimEnd('0').TrimEnd('.');
            return string.Format("{0}{1}", showsize, unit[idxUnit]);
        }
        #endregion


        #endregion



        public static void Output(StringBuilder msg, string suffix = null)
        {
            Output(msg.ToString(), suffix);
        }
        public static void Output(string msg, string suffix = null)
        {
            suffix = suffix ?? "run";
            string day = DateTime.Now.ToString("yyyyMMdd");
            LogHelper.WriteCustom(msg, day + "\\" + suffix, false);
            //Console.WriteLine(msg);
        }
        public static void Output(string msg, Exception exp)
        {
            msg += Environment.NewLine + exp;
            LogHelper.WriteCustom(msg, "exception\\", "err", false);
            //Console.WriteLine(msg);
        }
    }

    class ProcessItem
    {
        private static DateTime _cacheTime = DateTime.MinValue;
        private static List<ProcessItem> _cacheProcess = null;
        private const int CACHE_MS = 2000;
        static object _lock = new object();

        /// <summary>
        /// 返回所有进程列表，并缓存2秒，避免频繁读取服务器进程影响性能
        /// </summary>
        /// <returns></returns>
        public static List<ProcessItem> GetProcessesAndCache()
        {
            var now = DateTime.Now;
            // bug测试代码
            //string str = string.Format("cache::::{0} {1} {2}", 
            //    now.ToString("mm:ss.fff"), _cacheTime.ToString("mm:ss.fff"), (now - _cacheTime).TotalMilliseconds.ToString());
            //TaskService.Output(str);

            if (_cacheProcess == null || (now - _cacheTime).TotalMilliseconds > CACHE_MS)
            {
                lock (_lock)
                {
                    if (_cacheProcess == null || (now - _cacheTime).TotalMilliseconds > CACHE_MS)
                    {
                        //TaskService.Output(str + " ============");
                        // 通过bug测试代码，发现必须先设置_cacheProcess，设置完成，才能设置_cacheTime，否则会出bug
                        // 比如2个任务同时进行时，前一任务设置了_cacheTime，后一任务就用旧进程列表去了，导致bug
                        _cacheProcess = GetAllProcesses();
                        _cacheTime = now;
                    }
                }
            }
            return _cacheProcess;
        }

        static List<ProcessItem> GetAllProcesses()
        {
            var ret = new List<ProcessItem>();

            //http://msdn.microsoft.com/en-us/library/windows/desktop/aa394372(v=vs.85).aspx
            var scope = new ManagementScope(@"\\.\root\cimv2");
            var query = new SelectQuery("SELECT * FROM Win32_Process"); // where Name = 'w3wp.exe'
            using (var searcher = new ManagementObjectSearcher(scope, query))
            {
                foreach (var o in searcher.Get())
                {
                    var process = (ManagementObject)o;
                    //StringBuilder sbb = new StringBuilder(10000);
                    //foreach (PropertyData property in process.Properties)
                    //{
                    //    sbb.Append(property.Name + ":" + property.Value + "\r\n");
                    //}
                    var name = Convert.ToString(process["name"]);
                    var exePath = Convert.ToString(process["ExecutablePath"]);
                    var commandLine = Convert.ToString(process["CommandLine"]);
                    var pid = Convert.ToInt32(process["ProcessId"]);
                    var startDate = Convert.ToString(process["CreationDate"]);
                    var mem = Convert.ToInt64(process["WorkingSetSize"]);
                    var memVirtual = Convert.ToInt64(process["VirtualSize"]);
                    var memPage = Convert.ToInt64(process["PagefileUsage"]);

                    ret.Add(new ProcessItem()
                    {
                        commandLine = commandLine,
                        createDate = startDate,
                        exePath = exePath,
                        memory = mem,
                        memoryPage = memPage,
                        memoryVirtual = memVirtual,
                        name = name,
                        pid = pid
                    });
                    //ret.AppendFormat("{0},{1},{2},{4},{3},{5},{6}\r\n",
                    //    pid,
                    //    name,
                    //    mem.ToString(),
                    //    memVirtual.ToString(),
                    //    memPage.ToString(),
                    //    startDate,
                    //    commandLine);
                }
            }
            return ret;
        }

        /// <summary>
        /// 判断指定进程是否运行中
        /// </summary>
        /// <param name="exepath">exe全路径</param>
        /// <returns></returns>
        public static bool Exists(string exepath)
        {
            // 经测试，name不区分大小写，斜杠要做转义
            exepath = exepath.Replace("'", "").Replace(@"\", @"\\");
            //http://msdn.microsoft.com/en-us/library/windows/desktop/aa394372(v=vs.85).aspx
            var scope = new ManagementScope(@"\\.\root\cimv2");
            var query = new SelectQuery("SELECT * FROM Win32_Process where ExecutablePath = '" + exepath + "'");
            using (var searcher = new ManagementObjectSearcher(scope, query))
            {
                return searcher.Get().Count > 0;
            }
        }


        /// <summary>
        /// 返回指定路径的进程
        /// </summary>
        /// <param name="exepath">exe全路径</param>
        /// <returns></returns>
        public static List<ProcessItem> GetProcessByPath(string exepath)
        {
            var ret = new List<ProcessItem>();
            // 经测试，name不区分大小写，斜杠要做转义
            exepath = exepath.Replace("'", "").Replace(@"\", @"\\");
            //http://msdn.microsoft.com/en-us/library/windows/desktop/aa394372(v=vs.85).aspx
            var scope = new ManagementScope(@"\\.\root\cimv2");
            var query = new SelectQuery("SELECT * FROM Win32_Process where ExecutablePath = '" + exepath + "'");
            using (var searcher = new ManagementObjectSearcher(scope, query))
            {
                foreach (var o in searcher.Get())
                {
                    var process = (ManagementObject)o;
                    var name = Convert.ToString(process["name"]);
                    var exePath = Convert.ToString(process["ExecutablePath"]);
                    var commandLine = Convert.ToString(process["CommandLine"]);
                    var pid = Convert.ToInt32(process["ProcessId"]);
                    var startDate = Convert.ToString(process["CreationDate"]);
                    var mem = Convert.ToInt64(process["WorkingSetSize"]);
                    var memVirtual = Convert.ToInt64(process["VirtualSize"]);
                    var memPage = Convert.ToInt64(process["PagefileUsage"]);
                    ret.Add(new ProcessItem()
                    {
                        commandLine = commandLine,
                        createDate = startDate,
                        exePath = exePath,
                        memory = mem,
                        memoryPage = memPage,
                        memoryVirtual = memVirtual,
                        name = name,
                        pid = pid
                    });
                }
            }
            return ret;
        }

        public int pid;
        public string name;
        public string exePath;
        public string commandLine;
        public long memory;
        public long memoryVirtual;
        public long memoryPage;
        public string createDate;
        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3} {4} {5} {6}",
                pid.ToString(),
                name,
                commandLine,
                memory.ToString("N0"),
                memoryVirtual.ToString("N0"),
                memoryPage.ToString("N0"),
                createDate);
        }
    }
}
