using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace PlanServerService
{
    [DataContract]
    public class TaskItem
    {
        /// <summary>
        /// 任务自增id
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int id { get; set; }
        /// <summary>
        /// 任务的程序文件所在路径
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string exepath { get; set; }
        /// <summary>
        /// 任务的程序文件所需参数
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string exepara { get; set; }
        /// <summary>
        /// 任务运行类型
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public RunType runtype { get; set; }
        /// <summary>
        /// 定时运行参数,对应runtype的PerDay,PerWeek,PerMonth不同而不同
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string taskpara { get; set; }
        /// <summary>
        /// 任务描述
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string desc { get; set; }
        /// <summary>
        /// 从上次更新以来的运行次数
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int runcount { get; set; }
        /// <summary>
        /// 最后一次任务启动时的进程id
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int pid { get; set; }
        /// <summary>
        /// 最后一次任务启动时间
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public DateTime pidtime { get; set; }
        /// <summary>
        /// 任务创建时间
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public DateTime instime { get; set; }
        /// <summary>
        /// 任务当前状态
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public ExeStatus status { get; set; }
        /// <summary>
        /// 任务需要立即处理的状态
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public ImmediateType immediate { get; set; }

        private List<TimePara> _taskPara;
        public List<TimePara> TaskPara
        {
            get
            {
                if (_taskPara == null && (runtype == RunType.PerDay || runtype == RunType.PerWeek || runtype == RunType.PerMonth))
                {                    
                    _taskPara = GetPara(runtype, taskpara);
                }
                return _taskPara;
            }
        }

        public static List<TimePara> GetPara(RunType runtype, string runtypepara)
        {
            var tmpret = new List<TimePara>();

            string tmppara = runtypepara;
            if (tmppara != null && (tmppara = tmppara.Trim()) != string.Empty)
            {
                //格式: 1-1:12,3:45|60;3-13:11,23:45
                string[] tmp = tmppara.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string item1 in tmp)
                {
                    string item = item1;
                    // 每天运行时，加上1-，便于统一处理
                    if (runtype == RunType.PerDay)
                        item = "1-" + item;

                    string[] itemtmp = item.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                    if (itemtmp.Length != 2)
                        continue;

                    int weekOrDay;
                    // key必须是数字，即周几 或 每月几号
                    if (!int.TryParse(itemtmp[0], out weekOrDay))
                        continue;
                    // 每周运行，必须是周日到周六之间
                    if (runtype == RunType.PerWeek && (weekOrDay < 0 || weekOrDay > 6))
                        continue;
                    // 每月运行，必须是1到31号之间
                    if (runtype == RunType.PerMonth && (weekOrDay < 1 || weekOrDay > 31))
                        continue;

                    string[] valuetmp = itemtmp[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string valueitem in valuetmp)
                    {
                        //时间格式为 1:12 或 3:45|3600，没有竖线表示启动但不停止，竖线后面表示运行时长（分钟）
                        //如3:45|60表示，3点45启动，到4点45结束程序
                        string[] startEnd = valueitem.Split(new[] { '|' }, StringSplitOptions.None);

                        // 启动时间处理
                        string[] timetmp = startEnd[0].Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        // 必须配置启动时间
                        if (timetmp.Length != 2)
                            continue;
                        uint hourStart, minStart;
                        if (!uint.TryParse(timetmp[0].Trim(), out hourStart) ||
                            !uint.TryParse(timetmp[1].Trim(), out minStart))
                            continue;

                        // 运行时长
                        int runtime;
                        if (startEnd.Length < 2 || !int.TryParse(startEnd[1], out runtime))
                        {
                            runtime = 0;
                        }

                        TimePara timepara = new TimePara
                        {
                            WeekOrDay = weekOrDay,
                            StartHour = (int)hourStart,
                            StartMin = (int)minStart,
                            RunMinute = runtime,
                        };
                        tmpret.Add(timepara);
                    }
                }
            }
            return tmpret;
        }
    }

    [DataContract]
    public class TaskLog
    {
        /// <summary>
        /// 任务自增id
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int id { get; set; }

        /// <summary>
        /// 任务的程序文件所在路径
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string exepath { get; set; }

        /// <summary>
        /// 日志详情
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string log { get; set; }

        /// <summary>
        /// 日志插入时间
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public DateTime instime { get; set; }
    }

    // 只允许2种配置：只配置开始时间 或 开始结束时间都配置
    public class TimePara
    {
        /// <summary>
        /// 星期几或几号
        /// </summary>
        public int WeekOrDay { get; set; }

        /// <summary>
        /// 几点启动
        /// </summary>
        public int StartHour { get; set; }

        /// <summary>
        /// 几分启动
        /// </summary>
        public int StartMin { get; set; }

        /// <summary>
        /// 运行时长，单位分钟
        /// 0表示不判断结束
        /// </summary>
        public int RunMinute { get; set; }

        /// <summary>
        /// 运行时使用，记录下每次的任务启动时间
        /// </summary>
        public DateTime StartTime { get; set; }
    }

    public enum RunType
    {
        /// <summary>
        /// 不启动，进程存在时，不停止
        /// </summary>
        [Description("不启动,不强制终止")]
        Stop = 0,
        /// <summary>
        /// 只启动1次,进程运行中时,等到进程自行终止后再启动
        /// </summary>
        [Description("只启动1次")]
        OneTime = 1,
        /// <summary>
        /// 始终运行
        /// </summary>
        [Description("保持运行,退出时自动重启")]
        Always = 2,
        /// <summary>
        /// 重启一次,并重置为Always
        /// </summary>
        [Description("重启,并重置为Always")]
        Restart = 3,
        /// <summary>
        /// 停止,并等待1分钟后重启,然后重置为Always
        /// </summary>
        [Description("终止,等1分钟重启,并重置为Always")]
        StopAndWait1Min = 4,
        /// <summary>
        /// 每天定时运行,对应参数taskpara格式: 1:12,3:45,21:32(表示每天的1点12分、3点45分、21点32分各执行一次)
        /// </summary>
        [Description("每天定时运行")]
        PerDay = 5,
        /// <summary>
        /// 每周定时运行,对应参数taskpara格式: 1-1:12,3:45;3-13:11,23:45(表示每周一的1点12分、3点45分各启动一次,每周三的13点11分、23点45分各启动一次,0表示周日)
        /// </summary>
        [Description("每周定时运行")]
        PerWeek = 6,
        /// <summary>
        /// 每月定时运行,对应参数taskpara格式: 1-1:12,3:45;13-13:11,23:45(表示每月1号的1点12分、3点45分各启动一次,每月13号的13点11分、23点45分各启动一次)
        /// </summary>
        [Description("每月定时运行")]
        PerMonth = 7,
        /// <summary>
        /// 不启动,进程存在时,强行停止
        /// </summary>
        [Description("终止进程，不再启动")]
        ForceStop = 8,
    }

    public enum ExeStatus
    {
        /// <summary>
        /// 停止状态
        /// </summary>
        Stopped = 0,
        /// <summary>
        /// 运行中状态
        /// </summary>
        Running = 1,
        /// <summary>
        /// 任务文件不存在状态
        /// </summary>
        NoFile = 7,
        /// <summary>
        /// 任务是定时运行，但是参数未配置
        /// </summary>
        NoPara = 8,
        /// <summary>
        /// 未知状态
        /// </summary>
        Unknown = 9
    }

    public enum ImmediateType
    {
        /// <summary>
        /// 无操作
        /// </summary>
        None = 0,
        /// <summary>
        /// 立即启动（启动后按现在RunType处理）
        /// </summary>
        Start = 1,
        /// <summary>
        /// 立即终止（终止后按现在RunType处理）
        /// </summary>
        Stop = 2,
        /// <summary>
        /// 立即重启（重启后按现在RunType处理）
        /// </summary>
        ReStart = 3
    }
}
