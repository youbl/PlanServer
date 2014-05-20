using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using PlanServerService.FileAdmin;

namespace PlanServerService
{
    /// <summary>
    /// 管理程序或页面用到的方法集
    /// </summary>
    public static class TaskClient
    {
        #region 计划任务相关方法
        
        /// <summary>
        /// 获取所有任务
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static TaskManage GetAllTask(string ip, int port, out string msg)
        {
            // 连接网络数据库，返回全部任务
            string str = SendToServer(ip, port, OperationType.GetAllTasks, null);
            if (string.IsNullOrEmpty(str))
            {
                msg = ("数据为空");
                return null;
            }
            string isDebug = ConfigurationManager.AppSettings["PlanDebug"];
            if (!string.IsNullOrEmpty(isDebug) && isDebug == "1")
            {
                HttpContext.Current.Response.AppendHeader("Content-Disposition", "attachment;filename=tmp");
                HttpContext.Current.Response.ContentType = "application/unknown";
                HttpContext.Current.Response.Write(str);
                HttpContext.Current.Response.End();
                msg = null;
                return null;
            }

            if (str.StartsWith("err", StringComparison.OrdinalIgnoreCase))
            {
                msg = (str.Substring(3));
                return null;
            }

            var ret = new TaskManage();

            // 分拆出时间数据
            int lastRunSplit = str.IndexOf('|');
            if (lastRunSplit > 0 && lastRunSplit < 40)
            {
                string[] tmp = str.Substring(0, lastRunSplit).Split(',');
                ret.serverTime = tmp[0];
                ret.lastRunTime = tmp.Length > 1 ? tmp[1] : string.Empty;
                str = str.Substring(lastRunSplit + 1);
            }
            ret.tasks = Common.XmlDeserializeFromStr<List<TaskItem>>(str);
            ret.ip = ip;

            msg = null;
            return ret;
        }

        public static TaskManage SaveTask(string ip, int port, TaskItem task, out string msg)
        {
            return SaveTasks(ip, port, new [] { task }, out msg);
        }

        public static TaskManage SaveTasks(string ip, int port, IEnumerable<TaskItem> tasks, out string msg)
        {
            string strTask = Common.XmlSerializeToStr(tasks);

            string str = SendToServer(ip, port, OperationType.SaveTasks, strTask);
            if (str.StartsWith("err", StringComparison.OrdinalIgnoreCase))
            {
                msg = str.Substring(3);
                return null;
            }
            var ret = new TaskManage();
            // 分拆出时间数据
            int lastRunSplit = str.IndexOf('|');
            if (lastRunSplit > 0 && lastRunSplit < 40)
            {
                string[] tmp = str.Substring(0, lastRunSplit).Split(',');
                ret.serverTime = tmp[0];
                ret.lastRunTime = tmp.Length > 1 ? tmp[1] : string.Empty;
                str = str.Substring(lastRunSplit + 1);
            }

            ret.tasks = Common.XmlDeserializeFromStr<List<TaskItem>>(str);
            ret.ip = ip;
            
            msg = null;
            return ret;
        }

        public static string DelTaskById(string ip, int port, int id)
        {
            // 通过Socket删除
            string str = SendToServer(ip, port, OperationType.DelTasks, id.ToString());
            if (str.StartsWith("err", StringComparison.OrdinalIgnoreCase))
            {
                return str.Substring(3);
            }
            return null;
        }
        #endregion


        #region 文件管理相关方法
        /// <summary>
        /// 显示目录列表
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="dir">目录完整路径</param>
        /// <param name="sort">排序方法</param>
        /// <param name="showMd5">是否返回文件md5</param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static FileResult GetDir(string ip, int port, string dir, SortType sort, bool showMd5, out string msg)
        {
            string args = dir + "|" + ((int) sort).ToString() + "|" + (showMd5 ? "1" : "0");
            string str = SendToServer(ip, port, OperationType.DirShow, args);
            if (str.StartsWith("err", StringComparison.OrdinalIgnoreCase))
            {
                msg = str.Substring(3);
                return null;
            }
            FileResult ret = Common.XmlDeserializeFromStr<FileResult>(str);
            msg = null;
            return ret;
        }

        /// <summary>
        /// 移动指定目录下的指定目录和文件到目标目录
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="parentDir"></param>
        /// <param name="toDir"></param>
        /// <param name="files"></param>
        /// <param name="dirs"></param>
        /// <returns></returns>
        public static string DirMove(string ip, int port, string parentDir, string toDir,
            string[] files, string[] dirs)
        {
            string movefiles = "";
            if (files != null && files.Length > 0)
            {
                movefiles = files.Aggregate(movefiles, (current, f) => current + (f + "*"));
            }
            string movedirs = "";
            if (dirs != null && dirs.Length > 0)
            {
                movedirs = dirs.Aggregate(movedirs, (current, s) => current + (s + "*"));
            }
            string args = parentDir + "|" + toDir + "|" + movefiles + "|" + movedirs;
            string str = SendToServer(ip, port, OperationType.DirMove, args);
            if (str.StartsWith("err", StringComparison.OrdinalIgnoreCase))
            {
                return str.Substring(3);
            }
            string[] arrNum = str.Split('|');
            if (arrNum.Length == 3)
                return "移动了" + arrNum[0] + "个文件，" + arrNum[1] + "个目录下的" + arrNum[2] + "个文件";
            return str;
        }

        /// <summary>
        /// 删除指定目录下的指定目录和文件到目标目录
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="parentDir"></param>
        /// <param name="files"></param>
        /// <param name="dirs"></param>
        /// <returns></returns>
        public static string DirDel(string ip, int port, string parentDir,
            string[] files, string[] dirs)
        {
            string movefiles = "";
            if (files != null && files.Length > 0)
            {
                movefiles = files.Aggregate(movefiles, (current, f) => current + (f + "*"));
            }
            string movedirs = "";
            if (dirs != null && dirs.Length > 0)
            {
                movedirs = dirs.Aggregate(movedirs, (current, s) => current + (s + "*"));
            }
            string args = parentDir + "|" + movefiles + "|" + movedirs;
            string str = SendToServer(ip, port, OperationType.DirDel, args);
            if (str.StartsWith("err", StringComparison.OrdinalIgnoreCase))
            {
                return str.Substring(3);
            }
            string[] arrNum = str.Split('|');
            if (arrNum.Length == 2)
                return "删除了" + arrNum[0] + "个文件，" + arrNum[1] + "个目录";
            return str;
        }

        public static string DirCreate(string ip, int port, string newDir)
        {
            string str = SendToServer(ip, port, OperationType.DirCreate, newDir);
            if (str.StartsWith("err", StringComparison.OrdinalIgnoreCase))
            {
                return str.Substring(3);
            }
            return str;
        }

        public static string DirRename(string ip, int port, string old, string newname)
        {
            string args = old + "|" + newname;
            string str = SendToServer(ip, port, OperationType.DirRename, args);
            if (str.StartsWith("err", StringComparison.OrdinalIgnoreCase))
            {
                return str.Substring(3);
            }
            return str;
        }
        public static string FileRename(string ip, int port, string old, string newname)
        {
            string args = old + "|" + newname;
            string str = SendToServer(ip, port, OperationType.FileRename, args);
            if (str.StartsWith("err", StringComparison.OrdinalIgnoreCase))
            {
                return str.Substring(3);
            }
            return str;
        }
        public static string FileDownload(string ip, int port, string downfile, out string recievefile)
        {
            string args = downfile;
            recievefile = null;
            string str = SendToServer(ip, port, OperationType.FileDownload, args, ref recievefile);
            //if (str != "ok")
            //{
            //    return str;//.Substring(3);
            //}
            return str;
        }
        public static string FileUpload(string ip, int port, string uploadDir, string fileName, string sendFilePath)
        {
            string args = uploadDir + "|" + fileName;
            string str = SendToServer(ip, port, OperationType.FileUpload, args, sendFilePath);
            //if (str != "ok")
            //{
            //    return str;//.Substring(3);
            //}
            return str;
        }
        public static string FileUnZip(string ip, int port, string zipfile, string unzipdir)
        {
            string args = zipfile + "|" + unzipdir;
            string str = SendToServer(ip, port, OperationType.FileUnZip, args);
            if (str.StartsWith("err", StringComparison.OrdinalIgnoreCase))
            {
                return str.Substring(3);
            }
            return str;
        }

        /// <summary>
        /// 打包下载指定目录下的指定目录和文件到目标目录
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="parentDir"></param>
        /// <param name="files"></param>
        /// <param name="dirs"></param>
        /// <returns></returns>
        public static string DirDownloadZip(string ip, int port, string parentDir,
            string[] files, string[] dirs)
        {
            string zipfiles = "";
            if (files != null && files.Length > 0)
            {
                zipfiles = files.Aggregate(zipfiles, (current, f) => current + (f + "*"));
            }
            string zipdirs = "";
            if (dirs != null && dirs.Length > 0)
            {
                zipdirs = dirs.Aggregate(zipdirs, (current, s) => current + (s + "*"));
            }
            string args = parentDir + "|" + zipfiles + "|" + zipdirs;
            string recieveFilePath = null;
            string str = SendToServer(ip, port, OperationType.DirDownloadZip, args, ref recieveFilePath);
            if (str.StartsWith("err", StringComparison.OrdinalIgnoreCase))
            {
                return str.Substring(3);
            }
            if (!string.IsNullOrEmpty(recieveFilePath) && File.Exists(recieveFilePath))
                return "ok" + recieveFilePath;
            return str;
        }

        /// <summary>
        /// 打包下载指定目录下的指定目录和文件到目标目录
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static string DirSizeGet(string ip, int port, string dir)
        {
            string str = SendToServer(ip, port, OperationType.DirSizeGet, dir);
            if (str.StartsWith("err", StringComparison.OrdinalIgnoreCase))
            {
                return str.Substring(3);
            }
            string[] arr = str.Split('|');
            if(arr.Length != 3)
                return str;
            long size;
            int cntFile, cntDir;
            if (!long.TryParse(arr[0], out size) || !int.TryParse(arr[1], out cntDir) || !int.TryParse(arr[2], out cntFile))
                return str;
            return size.ToString("N0") + "字节(" + cntDir.ToString("N0") + "个子目录 " + cntFile.ToString("N0") + "个文件)";
        }

        #endregion



        public static string ImmediateOperate(string ip, int port, ImmediateType type, string path, string exepara)
        {
            // 通过Socket处理
            string args = ((int) type).ToString() + "\n" + path + "\n" + exepara;
            string str = SendToServer(ip, port, OperationType.Immediate, args);
            //if (str.StartsWith("err", StringComparison.OrdinalIgnoreCase))
            //{
            //    return str.Substring(3);
            //}
            return str;
        }

        public static string RunDllMethod(string ip, int port, string method)
        {
            // 通过Socket处理
            string str = SendToServer(ip, port, OperationType.RunMethod, method);
            return str;
        }

        public static string GetProcess(string ip, int port)
        {
            string str = SendToServer(ip, port, OperationType.GetProcesses);
            return str;
        }

// ReSharper disable RedundantAssignment
        public static string SendToServer(string ip, int port, OperationType type, string arg = null, string sendFilePath = null)
        {
            return SendToServer(ip, port, type, arg, ref sendFilePath);
        }
        // ReSharper restore RedundantAssignment

        public static string SendToServer(string ip, int port, OperationType type, string arg, ref string recievefile)
        {
            string strType = ((int)type).ToString();
            string checkcode = Common.GetCheckCode(strType, arg);
            string args = checkcode + "_" + strType + "_" + arg;
            return SocketClient.SendBySocket(ip, port, args, ref recievefile);
        }

    }

    public class TaskManage
    {
        public List<TaskItem> tasks { get; set; }
        public string lastRunTime { get; set; }
        public string serverTime { get; set; }
        public string ip { get; set; }
    }
}
