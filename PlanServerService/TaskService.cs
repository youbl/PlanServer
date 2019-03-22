using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using PlanServerService.FileAdmin;

namespace PlanServerService
{
    /// <summary>
    /// 监听并处理客户端请求的服务类
    /// </summary>
    public static class TaskService
    {

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
            var dictRunCnt = dbaccess.GetLogCntByTime();
            int cnt;
            foreach (var task in tasks)
            {
                if (dictRunCnt.TryGetValue(task.exepath, out cnt))
                {
                    // 统计最近5分钟运行次数，以便判断是否出异常了，导致频繁启动
                    task.RunsIn5Minute = cnt;
                }
            }
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
            if (exepath.IndexOf('/') < 0 && exepath.IndexOf('\\') < 0)
            {
                string tmp = FindExeFromAllJob(exepath);
                if (string.IsNullOrEmpty(tmp))
                    return "未找到对应job：" + exepath;
                exepath = tmp;
            }
            if (!File.Exists(exepath))
            {
                return "文件不存在:" + exepath;
            }

            string exepara = args[2];
            int ret;
            switch ((ImmediateType)imtype)
            {
                default:
                    return "不存在的临时类型";
                case ImmediateType.Start:
                    // 查找进程是否运行中，不在则启动
                    ret = ProcessHelper.CheckAndStartProcess(exepath, exepara);
                    if (ret > 0)
                    {
                        return exepath + " 成功启动, pid:" + ret.ToString();
                    }
                    else
                    {
                        return exepath + " 运行中，无需启动";
                    }
                case ImmediateType.Stop:
                    var processes1 = ProcessItem.GetProcessByPath(exepath);
                    ret = ProcessHelper.KillProcesses(processes1);
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
                    var processes2 = ProcessItem.GetProcessByPath(exepath);
                    ret = ProcessHelper.KillProcesses(processes2);
                    if (ret > 0)
                    {
                        restartMsg = exepath + " 成功关闭个数:" + ret.ToString();
                    }
                    else
                    {
                        restartMsg = exepath + " 未启动";
                    }
                    // 查找进程是否运行中，不在则启动
                    ret = ProcessHelper.CheckAndStartProcess(exepath, exepara);
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

        /// <summary>
        /// 根据exe文件名，在所有job中匹配到第一条记录，
        /// 用于api调用
        /// </summary>
        /// <param name="exeName"></param>
        /// <returns></returns>
        static string FindExeFromAllJob(string exeName)
        {
            return Dal.Default.GetByExeName(exeName);
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


    }
}
