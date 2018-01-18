using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace PlanServerService
{
    //SQLite参数化时，不能用SqlDbType.Varchar，会默认变成Int32，要用DbType.String或者DbType.String
    public class Dal
    {

        // 数据库连接字符串
        static string _constr;
        static string Constr
        {
            get
            {
                if (string.IsNullOrEmpty(_constr))
                {
                    _constr = Common.GetSetting("sqliteFilePath");
                    if (string.IsNullOrEmpty(_constr))
                    {
                        _constr = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sqlite.db");
                    }
                    else if (!Common.IsPhysicalPath(_constr))
                    {
                        _constr = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _constr);
                    }
                }
                return _constr;
            }
        }


        private static Dal _default;
        public static Dal Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new Dal();
                    _default.CheckDb();
                }
                return _default;
            }
        }
        // 用于解决“The database file is locked”问题
        // Sqlite多进程可以同时打开同一个数据库，也可以同时 SELECT 。但只有一个进程可以立即改数据库。
        private static readonly object lockobj = new object();

        private Dal()
        {
        }

        void CheckDb()
        {
            if (!File.Exists(Constr))
            {
                CreateDb(Constr);
            }
        }

        static void CreateDb(string dbFilename)
        {
            if (!File.Exists(dbFilename))
            {
                string dir = Path.GetDirectoryName(dbFilename);
                if (string.IsNullOrEmpty(dir))
                    throw new Exception("数据库路径配置有误：" + dbFilename);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                SQLiteConnection.CreateFile(dbFilename);
                // 创建任务表,COLLATE NOCASE表示这个字段不区分大小写（默认区分）
                string sql = @"create table tasks(
id INTEGER PRIMARY KEY autoincrement, 
desc varchar(200) null COLLATE NOCASE, 
exepath varchar(500) not null COLLATE NOCASE, 
exepara varchar(500) null COLLATE NOCASE, 
runtype int default 0 not null, 
taskpara varchar(500) null COLLATE NOCASE, 
runcount int default 0 not null,
pid int default 0 not null,
pidtime TIMESTAMP null,
instime TIMESTAMP not null default (datetime('now', 'localtime')),
exestatus int default 0 not null,
immediate int default 0 not null
)";//alter table tasks add immediate int default 0 not null
                SQLiteHelper.ExecuteNonQuery(dbFilename, sql);
                // 创建唯一索引，一个exepath只能用一次
                sql = "create unique index unq_exepath on tasks(exepath)";
                SQLiteHelper.ExecuteNonQuery(dbFilename, sql);

                // 创建运行时间表，每一轮检查完成时，更新为当前系统时间，用于前端判断计划任务主程序是否存活
                sql = "create table lastrun(dt varchar(50))";
                SQLiteHelper.ExecuteNonQuery(dbFilename, sql);
                sql = "insert into lastrun values('1979-1-12')";
                SQLiteHelper.ExecuteNonQuery(dbFilename, sql);

                // 创建进程定时运行参数表，用于记录定时运行的结束时间，以便判断
                sql = "create table TaskPara(tid int,wd int, shour int, smin int, runmin int, starttime varchar(50))";
                SQLiteHelper.ExecuteNonQuery(dbFilename, sql);

                // 创建进程状态变更日志表
                sql = @"create table TaskLog(
    id INTEGER PRIMARY KEY autoincrement, 
    exepath varchar(500) not null COLLATE NOCASE, 
    log varchar(500) not null, 
    instime TIMESTAMP not null default (datetime('now', 'localtime'))
)";
                SQLiteHelper.ExecuteNonQuery(dbFilename, sql);

                // 创建索引 exepath
                sql = "create index idx_exepath on TaskLog(exepath)";
                SQLiteHelper.ExecuteNonQuery(dbFilename, sql);
            }
        }
        //// 为任务表添加进程管理器里的起始时间字段
        //static void AddProcessTimeCol(string dbFilename)
        //{
        //    string sql = "select sql from sqlite_master where name='tasks' and type='table'";

        //}

        #region 维护程序访问任务专用的方法
        /// <summary>
        /// 获取最近检查时间
        /// </summary>
        /// <returns></returns>
        public string GetLastRuntime()
        {
            string sql = "select dt from lastrun";
            return Convert.ToString(SQLiteHelper.ExecuteScalar(Constr, sql));
        }

        public bool AddTask(TaskItem task)
        {
            string sql = @"INSERT INTO [tasks]
    ([exepath]
    ,[exepara]
    ,[runtype]
    ,[taskpara]
    ,[desc])
VALUES
    (@exepath
    ,@exepara
    ,@runtype
    ,@taskpara
    ,@desc)";

            SQLiteParameter[] para = new[] {
                new SQLiteParameter("@exepath",DbType.String){Value = task.exepath},
                new SQLiteParameter("@exepara",DbType.String){Value = task.exepara},
                new SQLiteParameter("@runtype",DbType.Int32){Value = task.runtype},
                new SQLiteParameter("@taskpara",DbType.String){Value = task.taskpara},
                new SQLiteParameter("@desc",DbType.String){Value = task.desc},
            };
            lock (lockobj)
            {
                return SQLiteHelper.ExecuteNonQuery(Constr, sql, para) > 0;
            }
        }

        public bool UpdateTask(TaskItem task)
        {
            string sql = @"UPDATE [tasks]
SET [exepath] = @exepath
    ,[exepara] = @exepara
    ,[runtype] = @runtype
    ,[taskpara] = @taskpara
    ,[desc] = @desc
WHERE id = @id
";
            SQLiteParameter[] para = new[] {
                new SQLiteParameter("@exepath",DbType.String){Value = task.exepath},
                new SQLiteParameter("@exepara",DbType.String){Value = task.exepara},
                new SQLiteParameter("@runtype",DbType.Int32){Value = (int)task.runtype},
                new SQLiteParameter("@taskpara",DbType.String){Value = task.taskpara},
                new SQLiteParameter("@desc",DbType.String){Value = task.desc},
                new SQLiteParameter("@id",DbType.Int64){Value = task.id},
            };
            int i;
            lock (lockobj)
            {
                i = SQLiteHelper.ExecuteNonQuery(Constr, sql, para);
            }
            if (i <= 0)
                return false;
            // 更新时，需要重置任务参数
            ClearTimePara(task.id);
            return true;
        }

        public bool DelTaskById(int id)
        {
            string sql = @"DELETE FROM [tasks] WHERE id = @id";
            SQLiteParameter[] para = new[] {
                new SQLiteParameter("@id",DbType.Int64){Value = id},
            };
            lock (lockobj)
            {
                return SQLiteHelper.ExecuteNonQuery(Constr, sql, para) > 0;
            }
        }

        public int ExecuteSql(string sql)
        {
            return SQLiteHelper.ExecuteNonQuery(Constr, sql);
        }

        public DataSet ExecuteDataset(string sql)
        {
            return SQLiteHelper.ExecuteDataset(Constr, sql);
        }
        #endregion


        #region 计划任务专用的方法
        /// <summary>
        /// 更新最近检查时间
        /// </summary>
        /// <returns></returns>
        public bool UpdateLastRuntime()
        {
            string sql = "update lastrun set dt = @dt";
            SQLiteParameter[] para = new[] {
                new SQLiteParameter("@dt",DbType.String){Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},
            };
            lock (lockobj)
            {
                return SQLiteHelper.ExecuteNonQuery(Constr, sql, para) > 0;
            }
        }

        /// <summary>
        /// 把只运行一次的任务更新为停止
        /// </summary>
        /// <param name="taskid"></param>
        /// <param name="fromtype"></param>
        /// <param name="totype"></param>
        /// <returns></returns>
        public bool UpdateTaskType(int taskid, RunType fromtype, RunType totype)
        {
            string sql = @"UPDATE [tasks] SET runtype = @totype WHERE id = @id and runtype = @fromtype";
            SQLiteParameter[] para = new[] {
                new SQLiteParameter("@id",DbType.Int64){Value = taskid},
                new SQLiteParameter("@totype",DbType.Int32){Value = (int)totype},
                new SQLiteParameter("@fromtype", DbType.Int32){Value = (int)fromtype},
            };
            lock (lockobj)
            {
                return SQLiteHelper.ExecuteNonQuery(Constr, sql, para) > 0;
            }
        }

        /// <summary>
        /// 任务运行起来时，把任务的进程id更新到数据库
        /// </summary>
        /// <param name="taskid"></param>
        /// <param name="pid"></param>
        /// <returns></returns>
        public bool UpdateTaskProcessId(int taskid, int pid)
        {
            string sql = @"UPDATE [tasks] SET [pid] = @pid, pidtime = @time,[runcount] = runcount+1 WHERE id = @id";
            SQLiteParameter[] para = new[] {
                new SQLiteParameter("@id",DbType.Int64){Value = taskid},
                new SQLiteParameter("@pid",DbType.Int32){Value = pid},
                new SQLiteParameter("@time", DbType.DateTime){Value = DateTime.Now},
            };
            lock (lockobj)
            {
                return SQLiteHelper.ExecuteNonQuery(Constr, sql, para) > 0;
            }
        }

        /// <summary>
        /// 更新任务的状态
        /// </summary>
        /// <param name="task"></param>
        /// <param name="status"></param>
        /// <param name="pidMsg"></param>
        /// <param name="needlog">是否强行记录日志</param>
        /// <returns></returns>
        public bool UpdateTaskExeStatus(TaskItem task, ExeStatus status, string pidMsg, bool needlog = false)
        {
            int taskid = task.id;
            string sql = @"UPDATE [tasks] SET [exestatus] = @status WHERE id = @id";
            SQLiteParameter[] para = new[] {
                new SQLiteParameter("@id",DbType.Int64){Value = taskid},
                new SQLiteParameter("@status",DbType.Int32){Value = status},
            };
            bool ret;
            lock (lockobj)
            {
                ret = SQLiteHelper.ExecuteNonQuery(Constr, sql, para) > 0;
            }
            if (ret && (task.status != status || needlog))
            {
                // 添加状态变更日志
                pidMsg = task.status.ToString() + "=>" + status.ToString() + " " + pidMsg;
                AddTaskLog(task.exepath, pidMsg);
            }
            return ret;
        }

        /// <summary>
        /// 添加 指定任务的任务结束参数表
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="tpara"></param>
        /// <returns></returns>
        public bool AddTimePara(int taskId, TimePara tpara)
        {
            string sql = @"INSERT INTO TaskPara(tid, wd, shour, smin, runmin, starttime)
VALUES(@tid, @wd, @shour, @smin, @runmin, @starttime)";
            SQLiteParameter[] para = new[]
            {
                new SQLiteParameter("@tid", DbType.Int32) {Value = taskId},
                new SQLiteParameter("@wd", DbType.Int32) {Value = tpara.WeekOrDay},
                new SQLiteParameter("@shour", DbType.Int32) {Value = tpara.StartHour},
                new SQLiteParameter("@smin", DbType.Int32) {Value = tpara.StartMin},
                new SQLiteParameter("@runmin", DbType.Int32) {Value = tpara.RunMinute},
                new SQLiteParameter("@starttime", DbType.String, 50) {Value = tpara.StartTime.ToString("yyyy-MM-dd HH:mm:ss")},
            };
            lock (lockobj)
            {
                return SQLiteHelper.ExecuteNonQuery(Constr, sql, para) > 0;
            }
        }

        /// <summary>
        /// 删除 指定任务的任务结束参数表
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="tpara"></param>
        /// <returns></returns>
        public DateTime GetTimePara(int taskId, TimePara tpara)
        {
            string sql = @"SELECT starttime FROM TaskPara 
WHERE tid=@tid 
  AND wd=@wd 
  AND shour=@shour 
  AND smin=@smin 
  AND runmin=@runmin";
            SQLiteParameter[] para = new[]
            {
                new SQLiteParameter("@tid", DbType.Int32) {Value = taskId},
                new SQLiteParameter("@wd", DbType.Int32) {Value = tpara.WeekOrDay},
                new SQLiteParameter("@shour", DbType.Int32) {Value = tpara.StartHour},
                new SQLiteParameter("@smin", DbType.Int32) {Value = tpara.StartMin},
                new SQLiteParameter("@runmin", DbType.Int32) {Value = tpara.RunMinute},
            };
            object obj;
            lock (lockobj)
            {
                obj = SQLiteHelper.ExecuteScalar(Constr, sql, para);
            }
            if (obj == null || obj == DBNull.Value)
                return default(DateTime);
            return DateTime.Parse(obj.ToString());
        }

        /// <summary>
        /// 删除 指定任务的任务结束参数表
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="tpara"></param>
        /// <returns></returns>
        public bool DelTimePara(int taskId, TimePara tpara)
        {
            string sql = @"DELETE FROM TaskPara 
WHERE tid=@tid 
  AND wd=@wd 
  AND shour=@shour 
  AND smin=@smin 
  AND runmin=@runmin";
            SQLiteParameter[] para = new[]
            {
                new SQLiteParameter("@tid", DbType.Int32) {Value = taskId},
                new SQLiteParameter("@wd", DbType.Int32) {Value = tpara.WeekOrDay},
                new SQLiteParameter("@shour", DbType.Int32) {Value = tpara.StartHour},
                new SQLiteParameter("@smin", DbType.Int32) {Value = tpara.StartMin},
                new SQLiteParameter("@runmin", DbType.Int32) {Value = tpara.RunMinute},
            };
            lock (lockobj)
            {
                return SQLiteHelper.ExecuteNonQuery(Constr, sql, para) > 0;
            }
        }

        public bool ClearTaskImmediate(int taskid)
        {
            string sql = @"UPDATE [tasks] SET [immediate] = 0 WHERE id = @id";
            SQLiteParameter[] para = new[] {
                new SQLiteParameter("@id",DbType.Int64){Value = taskid},
            };
            lock (lockobj)
            {
                return SQLiteHelper.ExecuteNonQuery(Constr, sql, para) > 0;
            }
        }


        /// <summary>
        /// 添加任务运行日志
        /// </summary>
        /// <param name="exepath"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public bool AddTaskLog(string exepath, string log)
        {
            string sql = @"INSERT INTO [TaskLog] ([exepath],[log]) VALUES(@exepath, @log)";
            SQLiteParameter[] para = new[] {
                new SQLiteParameter("@exepath",DbType.String){Value = exepath},
                new SQLiteParameter("@log",DbType.String){Value = log},
            };
            lock (lockobj)
            {
                return SQLiteHelper.ExecuteNonQuery(Constr, sql, para) > 0;
            }
        }
        /// <summary>
        /// 查找任务运行日志，倒序返回
        /// </summary>
        /// <param name="exepath"></param>
        /// <returns></returns>
        public List<TaskLog> FindTaskLog(string exepath)
        {
            var ret = new List<TaskLog>();
            string sql = @"SELECT * FROM [TaskLog] WHERE [exepath]=@exepath ORDER BY id DESC";
            SQLiteParameter[] para = new[] {
                new SQLiteParameter("@exepath",DbType.String){Value = exepath},
            };
            lock (lockobj)
            {
                using (var reader = SQLiteHelper.ExecuteReader(Constr, sql, para))
                {
                    while (reader.Read())
                    {
                        var item = new TaskLog
                        {
                            id = (int)(long)reader["id"],
                            exepath = Convert.ToString(reader["exepath"]).Trim(),
                            log = Convert.ToString(reader["log"]).Trim(),
                            instime = (DateTime)reader["instime"],
                        };
                        ret.Add(item);
                    }
                }
            }
            return ret;
        }
        #endregion



        /// <summary>
        /// 清空全部 或 指定任务的任务结束参数表
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public int ClearTimePara(int taskId = 0)
        {
            string sql = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='TaskPara'";
            if (Convert.ToInt32(SQLiteHelper.ExecuteScalar(Constr, sql)) <= 0)
            {
                sql = "create table TaskPara(tid int,wd int, shour int, smin int, runmin int, starttime varchar(50))";
                lock (lockobj)
                {
                    SQLiteHelper.ExecuteNonQuery(Constr, sql);
                }
            }
            sql = "delete from TaskPara";

            SQLiteParameter[] para = null;
            if (taskId > 0)
            {
                sql += " where tid = @tid";
                para = new[]
                           {
                               new SQLiteParameter("@tid", DbType.Int32)
                                   {Value = taskId},
                           };
            }
            lock (lockobj)
            {
                return SQLiteHelper.ExecuteNonQuery(Constr, sql, para);
            }
        }

        /// <summary>
        /// 获取所有任务
        /// </summary>
        /// <returns></returns>
        public List<TaskItem> GetAllTask()
        {
            string sql = @"SELECT * FROM [tasks] ORDER BY [id]";// WHERE [runtype] > 0
            List<TaskItem> ret = new List<TaskItem>();
            lock (lockobj)
            {
                using (var reader = SQLiteHelper.ExecuteReader(Constr, sql))
                {
                    List<string> colnames = GetColNames(reader);
                    while (reader.Read())
                    {
                        var task = new TaskItem
                        {
                            id = (int)(long)reader["id"],
                            exepath = Convert.ToString(reader["exepath"]).Trim(),
                            exepara = Convert.ToString(reader["exepara"]).Trim(),
                            runtype = (RunType)(int)reader["runtype"],
                            taskpara = Convert.ToString(reader["taskpara"]).Trim(),
                            desc = Convert.ToString(reader["desc"]).Trim(),
                            runcount = (int)reader["runcount"],
                            pid = (int)reader["pid"],
                            instime = (DateTime)reader["instime"],
                            status = (ExeStatus)reader["exestatus"],
                            immediate = colnames.Contains("immediate")
                                ? (ImmediateType)reader["immediate"]
                                : ImmediateType.None,
                        };
                        object pidtime = reader["pidtime"];
                        if (pidtime == null || pidtime == DBNull.Value)
                            task.pidtime = DateTime.MinValue;
                        else
                            task.pidtime = (DateTime)pidtime;
                        ret.Add(task);
                    }
                }
            }
            return ret;
        }


        public static List<string> GetColNames(IDataReader reader)
        {
            List<string> ret = new List<string>();
            if (reader == null)// || !reader.HasRows)
                return ret;
            for (int i = 0; i < reader.FieldCount; i++)
            {
                ret.Add(reader.GetName(i).ToLower());
            }
            return ret;
        }

    }
}

