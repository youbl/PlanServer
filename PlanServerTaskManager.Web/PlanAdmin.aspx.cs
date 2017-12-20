using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Security;
using PlanServerService;
using PlanServerService.FileAdmin;

namespace PlanServerTaskManager.Web
{
    public partial class PlanAdmin : System.Web.UI.Page
    {
        //下面替换为你需要用的md5值，比如a3de83c477b3b24d52a1c8bebbc7747b是sj.91.com
        protected string _pwd;   // ip在白名单里时，我们部门进入此页面的密码md5值
        protected string _pwdOther; // ip在白名单里时，其它部门进入此页面的密码md5值，tqnd.91.

        protected string _pwdAdminInner;   // 内网管理员进入此页面的密码md5值
        protected string _pwdAdminOuter;   // 外网管理员进入此页面的密码md5值

        protected const bool _needProxy = false;                            // 是否需要通过代理访问此页面

        private static string _logDir;

        protected string m_currentUrl;
        protected string m_localIp, m_remoteIp, m_remoteIpLst;

        protected static bool m_needLogin = !(ConfigurationManager.AppSettings["PlanNoPwd"] ?? "").Equals("true", StringComparison.OrdinalIgnoreCase);
        protected static bool m_isAdmin = !m_needLogin;
        protected static bool m_enableSql = false;

        protected static string m_domain = ConfigurationManager.AppSettings["PlanDomainName"] ?? "aaa.bbb.com";


        protected void Page_Load(object sender, EventArgs e)
        {
            if (_logDir == null)
            {
                if(labLogDir != null)
                    _logDir = labLogDir.Text;
                if (string.IsNullOrEmpty(_logDir))
                {
                    _logDir = @"e:\weblogs\planserver\";
                }
            }
            // ClearDir(SocketCommon.TmpDir);

            _pwd = labCommon.Text;
            _pwdOther = labCommonOther.Text;
            _pwdAdminInner = labMainInner.Text;
            _pwdAdminOuter = labMainOuter.Text;

            Response.Cache.SetNoStore(); // 这一句会导致Response.WriteFile 无法下载
            
            m_localIp = GetServerIpList();
            m_remoteIp = GetRemoteIp();
            m_remoteIpLst = GetRemoteIpLst();
            m_currentUrl = GetUrl(false);
            //if (string.IsNullOrEmpty(_pwd))
            //{
            //    Response.Write("未设置密码，请修改页面源码以设置密码\r\n" +
            //                   m_remoteIp + ";" + m_localIp);
            //    Response.End();
            //    return;
            //}

//#if !DEBUG
            if (!IsLogined(m_remoteIp))
            {
                Response.End();
                return;
            }
//#endif
            m_enableSql = !string.IsNullOrEmpty(Request.QueryString["sql"]);

            string flg = Request.Form["flg"] ?? Request.QueryString["flg"];
            if (!string.IsNullOrEmpty(flg))
            {
                int opType;
                if (!int.TryParse(flg, out opType))
                {
                    Response.Write("没有提交正确的操作符");
                    Response.End();
                    return;
                }
                //if (!ValidServer())
                //{
                //    Response.Write("对指定的IP没有权限" + Request["tip"]);
                //    Response.End();
                //    return;
                //}
   

                //string ret = PlanServerService.SocketClient.SendBySocket(ip, int.Parse(port),
                //    "0096fbac1fb4381cf88f3243e5e03438_0");
                try
                {
                    string msg = HttpUtility.UrlDecode(Convert.ToString(Request.Form));
                    msg = Request.Url + Environment.NewLine + msg;
                    OperationType type = (OperationType)opType;
                    Log(msg, type.ToString() + "_" + flg, null);
                    switch (type)
                    {
                        default:
                            Response.Write(Request.QueryString +"<hr/>"+Request.Form);
                            break;

                        #region 任务管理
                        case OperationType.GetAllTasks:
                            ReadTasks();
                            break;
                        case OperationType.SaveTasks:
                            SaveTask();
                            break;
                        case OperationType.DelTasks:
                            DeleteById();
                            break;


                        case OperationType.Immediate:
                            ImmediateOperate();
                            break;

                        case OperationType.RunMethod:
                            RunDllMethod();
                            break;
                        #endregion


                        #region 服务器和权限管理
                        case OperationType.AddAdminIp:
                            AddAdminIp();
                            break;
                        case OperationType.DelAdminIp:
                            DelAdminIp();
                            break;
                        case OperationType.AddAdminServer:
                            AddAdminServer();
                            break;
                        case OperationType.DelAdminServer:
                            DelAdminServer();
                            break;
                        case OperationType.GetAdminServers:
                            GetAdminServers();
                            break;
                        case OperationType.GetAdminListServers:
                            GetAdminServerList();
                            break;
                        #endregion


                        #region 目录管理
                        case OperationType.DirShow:// 列目录
                            ShowDir();
                            break;
                        case OperationType.DirMove:
                            FileMove();
                            break;
                        case OperationType.DirDel:
                            FileDel();
                            break;
                        case OperationType.DirCreate:
                            DirCreate();
                            break;
                        case OperationType.DirRename:
                            DirRename(true);
                            break;
                        case OperationType.FileRename:
                            DirRename(false);
                            break;
                        case OperationType.FileDownload:
                            FileDownload();
                            break;
                        case OperationType.FileUpload:
                            FileUpload();
                            break;
                        case OperationType.FileUnZip:
                            FileUnZip();
                            break;
                        case OperationType.DirDownloadZip:
                            DirDownloadZip();
                            break;
                        case OperationType.DirSizeGet:
                            DirSizeGet();
                            break;
                        case OperationType.LocalFileDown:
                        case OperationType.LocalFileOpen:
                            LocalFileOperation(type);
                            break;
                        #endregion

                        case OperationType.LogOut:
                            LogOut();
                            break;

                        case OperationType.GetProcesses:
                            GetProcess();
                            break;

                        case OperationType.RunSql:
                            RunSql();
                            break;
                    }
                }
                catch (ThreadAbortException) { }
                catch (Exception exp)
                {
                    Response.Write(exp.ToString());
                }
                Response.End();
            }
        }


        #region 计划任务相关操作方法
        
        void ReadTasks()
        {
            string strServer = Request.Form["tip"];
            if (strServer == null || (strServer = strServer.Trim()) == string.Empty)
            {
                strServer = "127.0.0.1";
            }
            string[] tip = strServer.Split(new [] { ',', ';', ' ', '|' }, 
                StringSplitOptions.RemoveEmptyEntries);
            var all = new List<TaskManage>(tip.Length);
            var err = new StringBuilder();
            foreach (string ip in tip)
            {
                string msg;
                TaskManage tasks = TaskClient.GetAllTask(ip, 23244, out msg);
                if (tasks != null)
                {
                    all.Add(tasks);
                    //Response.Write(msg);
                    //return;
                }
                else
                {
                    err.AppendFormat("{0}<br/>\r\n", msg);
                }
            }
            OutPutTasks(all, err.ToString());
        }

        void DeleteById()
        {
            int id;
            if (!int.TryParse(Request.Form["id"], out id))
            {
                Response.Write("请输入id");
                return;
            }
            string msg = TaskClient.DelTaskById(Request.Form["server"], 23244, id);
            if (string.IsNullOrEmpty(msg))
                msg = "删除成功";
            Response.Write(msg);
        }

        void SaveTask()
        {
            TaskItem task = new TaskItem();
            int id;
            if (!int.TryParse(Request.Form["id"], out id))
            {
                id = -1;// 表示新增
            }
            task.id = id;
            task.desc = Request.Form["desc"];
            
            string exepath = Request.Form["exepath"];
// ReSharper disable AssignNullToNotNullAttribute
            // 防止出现 c:\\\\a.exe 或 c:/a.exe这样的路径,统一格式化成：c:\a.exe形式
            exepath = Path.Combine(Path.GetDirectoryName(exepath), Path.GetFileName(exepath));
// ReSharper restore AssignNullToNotNullAttribute
            task.exepath = exepath;

            task.exepara = Request.Form["exepara"];
            task.taskpara = Request.Form["taskpara"];
            int runtype;
            if (!int.TryParse(Request.Form["runtype"], out runtype))
            {
                runtype = 0;
            }
            task.runtype = (RunType)runtype;

            string msg;
            TaskManage tasks = TaskClient.SaveTask(Request.Form["server"], 23244, task, out msg);
            if (tasks == null)
            {
                Response.Write(msg);
                return;
            }
            OutPutTasks(tasks);
        }


        void OutPutTasks(TaskManage task)
        {
            OutPutTasks(new[] { task });
        }

        void OutPutTasks(IEnumerable<TaskManage> tasks, string err = null)
        {
            StringBuilder sb = new StringBuilder(2000);
            sb.AppendFormat(@"{0}
<table border='1' cellSpacing='0' cellPadding='2' id='tbData'>
<tr style='background-color:#96d9f9'><th>服务器</th>
<th style='width:120px;'>说明</th>
<th style='width:310px;'>exe路径</th>
<th style='width:35px;'>exe<br/>参数</th>
<th>运行类型</th>
<th style='width:60px;'>任务参数</th>
<th>操作</th>
<th>单次操作</th>
<th>当前状态</th>
<th>最近启动时间</th>
<th></th>
</tr>
", err);
            foreach (TaskManage task in tasks)
            {
                if (task.tasks != null)
                {
                    task.tasks.Sort((a, b) => String.Compare(a.desc, b.desc, StringComparison.OrdinalIgnoreCase));

                    sb.AppendFormat(@"<tr class='server'><td>{2}</td><td colspan='10'>
    当前时间:<span style='color:blue;'>{0}</span>　前次轮询:<span style='color:blue;'>{1}</span>
<input type='button' value='新增任务' onclick='addrow(this);' />
<input type='button' value='运行方法' onclick='runMethodOpen(this, 1024);' />　
</td></tr>",
                        task.serverTime, task.lastRunTime, task.ip);
                    int idx = 1;
                    foreach (TaskItem item in task.tasks)
                    {
                        string status = item.status.ToString();
                        switch (item.status)
                        {
                            case ExeStatus.Running:
                                status = "<span style='color:red;'>" + status + "<span>";
                                break;
                            case ExeStatus.Stopped:
                                break;
                            default:
                                status = "<span style='color:blue;'>" + status + "<span>";
                                break;
                        }
                        sb.AppendFormat(@"<tr onmouseover='onRowOver(this);' onmouseout='onRowOut(this);' onclick='onRowClick(this);'>
    <td>{11}</td>
    <td><div class='input-1'><input type='text' title='{1}' style='width:97%;' value='{1}' /></div></td>
    <td><div class='input-2'><input type='text' title='{2}' style='width:97%;' value='{2}' /></div></td>
    <td><input type='text' style='width:92%;' value='{3}' /></td>
    <td v='{4}'></td>
    <td><div class='input-3'><input type='text' title='{5}' style='width:97%;' value='{5}' onclick='setPara(this);' readonly='readonly' /></div></td>
    <td><a href='#{0}' onclick='saverow({0},this,1);'>存</a>|<a href='javascript:void(0);' onclick='delrow({0},this,2);'>删</a></td>
    <td><a href='#{0}' onclick='operateImm(this,1,3);'>启</a>|<a href='javascript:void(0);' onclick='operateImm(this,2,3);'>停</a>|<a href='javascript:void(0);' onclick='operateImm(this,3,3);' title='停止并重启任务'>重</a></td>
    <td title='最近pid:{7}；创建时间:{9}；累计启动次数:{6}'>{10}</td>
    <td>{8}</td>
    <th>{12}</th>
</tr>",
                                        item.id, item.desc, item.exepath, item.exepara, (int)item.runtype,
                                        item.taskpara, item.runcount, item.pid, item.pidtime, item.instime, status, task.ip, idx.ToString());
                        idx++;
                    }
                }
            }
            sb.Append("</table>");
            Response.Write(sb.ToString());
        }
        #endregion


        #region 文件管理操作用的方法
        void ShowDir()
        {
            string strServer = Request.Form["tip"];
            if (strServer == null || (strServer = strServer.Trim()) == string.Empty)
            {
                strServer = "127.0.0.1";
            }
            string maindir = (Request.Form["dir"] ?? @"E:\WebLogs").TrimEnd('\\', '/');

            bool showMd5 = (Request.Form["md5"] ?? "") == "1";
            int sortTmp;
            int.TryParse(Request.Form["sort"] ?? "0", out sortTmp);
            SortType sort = (SortType) sortTmp;

            string msg;
            FileResult result = TaskClient.GetDir(strServer, 23244, maindir, sort, showMd5, out msg);
            if (!string.IsNullOrEmpty(msg))
            {
                Response.Write(msg);
                return;
            }
            StringBuilder sbRet = new StringBuilder("<span style='font-weight:bold;color:red;'>");
            sbRet.AppendFormat("{0} dirs, {1} files. server time:{2}. {3}", 
                result.SubDirs.Length.ToString(),
                result.SubFiles.Length.ToString(),
                result.ServerTime.ToString("yyyy-MM-dd HH:mm:ss_fff"),
                result.Others);

            string parentDir = (Path.GetDirectoryName(maindir) ?? "").Replace(@"\", @"\\");
            string rootDir = maindir.Length > 3 ? maindir.Substring(0, 2) : "";
            string currentdir = maindir.Replace(@"\", @"\\");
            sbRet.AppendFormat(@"</span>
<table border='0' cellpadding='0' cellspacing='0' style='table-layout:fixed;' class='filetb' id='tbFileManager'>
<tr style='background-color:#96d9f9'>
<th style='width:130px; text-align:left;'>
    <label><input type='checkbox' onclick=""chgChkColor($('#divFileRet input[type=checkbox]'), this.checked);"" />全选</label>&nbsp;
    <a href='javascript:void(0);' onclick='OpenCheckOption();'>条件</a>
    <br />
    <label><input type='checkbox' onclick='CheckAllDir(this);' />目录</label>
    <label><input type='checkbox' onclick='CheckAllFile(this);' />文件</label>
</th>
<th style='width:370px; text-align:left;'>
    [<a href='javascript:void(0);' onclick='fileOpenDir(""{0}"");'>上级目录</a>]
    [<a href='javascript:void(0);' onclick='fileOpenDir(""{1}"");'>根目录</a>]
</th>
<th style='width:50px;'>扩展名</th>
<th style='width:70px;'>大小(byte)</th>
<th style='width:150px;'>修改日期</th>{2}<th>序<br/>号</th>
</tr>
", parentDir, rootDir, (showMd5 ? "<th>MD5</th>" : ""));
            int idx = 1;
            string[] colors = new string[] { "#ffffff", "#dadada" };//f0f0f0
            // 绑定目录列表
            foreach (FileItem dir in result.SubDirs)
            {
                sbRet.AppendFormat(@"
<tr style='height:20px;background-color:{3}' onmouseover='onRowOver(this);' onmouseout='onRowOut(this);' onclick='onRowClick(this);'>
<td style='text-align:left;'>
    <label><input type='checkbox' value='{0}' name='chkDirListBeinet' /></label>
    <a href='javascript:void(0);' onclick=""fileReName('{0}', 0);"" tabindex='-1'>改名</a>|<a 
href='javascript:void(0);' onclick=""fileDel('{0}', 0);"" tabindex='-1'>删除</a>|<a href='javascript:void(0);' 
onclick='dirZipDown(""{0}"");' tabindex='-1'>ZIP</a>
</td>
<td style='text-align:left; font-weight:bold;'><a href='javascript:void(0);' onclick=""fileOpenDir('{4}\\{0}');"" tabindex='-1'>{0}</a></td>
<td style='text-align:center;'>目录</td>
<td style='text-align:center;'><a href='javascript:void(0);' onclick=""countDirSize('{0}');"" tabindex='-1'>计算大小</a></td>
<td style='text-align:center;'>{1}</td>" + (showMd5 ? "<td></td>" : "") + @"<th>{2}</th>
</tr>
",
                dir.Name, dir.LastModifyTime.ToString("yyyy-MM-dd HH:mm:ss.fff"), idx, colors[idx % 2], currentdir);
                idx++;
            }
            // 绑定文件列表
            foreach (FileItem file in result.SubFiles)
            {
                sbRet.AppendFormat(@"
<tr style='height:22px;background-color:{6}' onmouseover='onRowOver(this);' onmouseout='onRowOut(this);' onclick='onRowClick(this);'>
<td style='text-align:left;'>
    <label><input type='checkbox' value='{0}' name='chkFileListBeinet' /></label>
    <a href='javascript:void(0);' onclick=""fileReName('{0}', 1);"" tabindex='-1'>改名</a>|<a href='javascript:void(0);' 
onclick=""fileDel('{0}', 1);"" tabindex='-1'>删</a>|<a href='javascript:void(0);' 
onclick='fileDownOpen(""{0}"");' tabindex='-1'>下</a>|<a href='javascript:void(0);' 
onclick='fileDownOpen(""{0}"",1);' tabindex='-1'>开</a>
</td>
<td style='text-align:left;'>{0}</td>
<td style='text-align:center;'>{1}</td>
<td style='text-align:right;'>{2}</td>
<td style='text-align:center;'>{3}</td>{4}
<th>{5}</th>
</tr>
",
                file.Name,
                Path.GetExtension(file.Name),
                file.Size.ToString("N0"),
                file.LastModifyTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                (showMd5 ? "<td style='text-align:right;'>" + file.FileMd5 + "</th>" : ""),
                idx, colors[idx % 2]);
                idx++;
            }
            Response.Write(sbRet.ToString());
        }

        void FileMove()
        {
            string strServer = Request.Form["tip"];
            if (strServer == null || (strServer = strServer.Trim()) == string.Empty)
            {
                strServer = "127.0.0.1";
            }
            string maindir = Request.Form["dir"] ?? @"E:\WebLogs";

            string[] files = (Request.Form["files"] ?? string.Empty).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string[] dirs = (Request.Form["dirs"] ?? string.Empty).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string dirTo = Request.Form["to"];
            if ((files.Length <= 0 && dirs.Length <= 0) || string.IsNullOrEmpty(dirTo))
            {
                Response.Write("错误：未提交参数");
                return;
            }

            string result = TaskClient.DirMove(strServer, 23244, maindir, dirTo, files, dirs);
            Response.Write(result);
        }

        void FileDel()
        {
            string strServer = Request.Form["tip"];
            if (strServer == null || (strServer = strServer.Trim()) == string.Empty)
            {
                strServer = "127.0.0.1";
            }
            string maindir = Request.Form["dir"] ?? @"E:\WebLogs";

            string[] files = (Request.Form["files"] ?? string.Empty).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string[] dirs = (Request.Form["dirs"] ?? string.Empty).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (files.Length <= 0 && dirs.Length <= 0)
            {
                Response.Write("错误：未提交参数");
                return;
            }

            string result = TaskClient.DirDel(strServer, 23244, maindir, files, dirs);
            Response.Write(result);
        }

        void DirCreate()
        {
            string strServer = Request.Form["tip"];
            if (strServer == null || (strServer = strServer.Trim()) == string.Empty)
            {
                strServer = "127.0.0.1";
            }
            string maindir = Request.Form["dir"];// ?? @"E:\WebLogs";
            if (string.IsNullOrEmpty(maindir))
            {
                Response.Write("错误：未提交参数");
                return;
            }

            string result = TaskClient.DirCreate(strServer, 23244, maindir);
            Response.Write(result);
        }

        void DirRename(bool isdir)
        {
            string strServer = Request.Form["tip"];
            if (strServer == null || (strServer = strServer.Trim()) == string.Empty)
            {
                strServer = "127.0.0.1";
            }
            string maindir = Request.Form["dir"];// ?? @"E:\WebLogs";
            if (string.IsNullOrEmpty(maindir))
            {
                Response.Write("错误：未提交参数");
                return;
            }
            string newname = Request.Form["newname"];
            if (string.IsNullOrEmpty(newname))
            {
                Response.Write("错误：未提交参数2");
                return;
            }

            string result;
            if (isdir)
                result = TaskClient.DirRename(strServer, 23244, maindir, newname);
            else
                result = TaskClient.FileRename(strServer, 23244, maindir, newname);
            Response.Write(result);
        }

        void FileDownload()
        {
            string strServer = Request.Form["tip"];
            if (strServer == null || (strServer = strServer.Trim()) == string.Empty)
            {
                strServer = "127.0.0.1";
            }
            string downfile = Request.Form["dir"];
            if (string.IsNullOrEmpty(downfile))
            {
                Response.Write("错误：未提交参数");
                return;
            }
            string filepath;
            string result = TaskClient.FileDownload(strServer, 23244, downfile, out filepath);
            if (string.IsNullOrEmpty(result) || result != "ok")
            {
                Response.Write(result??"出错了");
                return;
            }
            Response.Write("ok" + filepath);
        }

        void FileUpload()
        {
            //var Request = HttpContext.Current.Request;
            //var Response = HttpContext.Current.Response;
            Response.Clear();
            string strServer = Request.QueryString["tip"];
            if (strServer == null || (strServer = strServer.Trim()) == string.Empty)
            {
                strServer = "127.0.0.1";
            }
            string dir = Request.Form["fileUploadDir"];
            if (string.IsNullOrEmpty(dir))
            {
                Response.Write("未指定上传目录");
                Response.End();
                return;
            }
            
            try
            {
                if (Request.Files.Count == 0)
                {
                    Response.Write("未上传文件");
                    Response.End();
                    return;
                }
                string tmpFileName = Request.Files[0].FileName;
                if (string.IsNullOrEmpty(tmpFileName))
                    tmpFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff") + "upload.tmp";
                else
                    tmpFileName = Path.GetFileName(tmpFileName);
                string oldFilename = tmpFileName;
                tmpFileName = Path.Combine(SocketCommon.TmpDir, tmpFileName);
                if (File.Exists(tmpFileName))
                {
                    File.Delete(tmpFileName);
                }
                Request.Files[0].SaveAs(tmpFileName);
                //byte[] data = Request.BinaryRead(Request.ContentLength);

                //using (FileStream stream = GetWriteStream(serverFileName))
                //{
                //    stream.Write(data, 0, data.Length);
                //    stream.Flush();
                //    stream.Close();
                //}

                string result = TaskClient.FileUpload(strServer, 23244, dir, oldFilename, tmpFileName);
                Response.Write("<script type='text/javascript'>top.fileManager();alert('" +
                   result.Replace('\r', ' ').Replace('\n', ' ') + "');</" + "script>");
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception exp)
            {
                Response.Write(exp.ToString());
            }
            Response.End();
        }

        void FileUnZip()
        {
            string strServer = Request.Form["tip"];
            if (strServer == null || (strServer = strServer.Trim()) == string.Empty)
            {
                strServer = "127.0.0.1";
            }
            string zipfile = Request.Form["dir"];
            if (string.IsNullOrEmpty(zipfile))
            {
                Response.Write("错误：未提交参数");
                return;
            }

            string result = TaskClient.FileUnZip(strServer, 23244, zipfile, null);
            Response.Write(result);
        }

        void DirDownloadZip()
        {
            string strServer = Request.Form["tip"];
            if (strServer == null || (strServer = strServer.Trim()) == string.Empty)
            {
                strServer = "127.0.0.1";
            }
            string maindir = Request.Form["dir"] ?? @"E:\WebLogs";

            string[] files = (Request.Form["files"] ?? string.Empty).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string[] dirs = (Request.Form["dirs"] ?? string.Empty).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (files.Length <= 0 && dirs.Length <= 0)
            {
                Response.Write("错误：未提交参数");
                return;
            }

            string result = TaskClient.DirDownloadZip(strServer, 23244, maindir, files, dirs);
            Response.Write(result);
        }

        void DirSizeGet()
        {
            string strServer = Request.Form["tip"];
            if (strServer == null || (strServer = strServer.Trim()) == string.Empty)
            {
                strServer = "127.0.0.1";
            }
            string maindir = Request.Form["dir"];
            if (string.IsNullOrEmpty(maindir))
            {
                Response.Write("错误：未提交参数");
                return;
            }

            string result = TaskClient.DirSizeGet(strServer, 23244, maindir);
            Response.Write(result);
        }


        void LocalFileOperation(OperationType type)
        {
            string filepath = Request.QueryString["file"];
            if (string.IsNullOrEmpty(filepath) || !File.Exists(filepath))
                Response.Write("指定的文件不存在:" + filepath);
            switch (type)
            {
                case OperationType.LocalFileDown:
                    string name = Request.QueryString["name"];
                    if (string.IsNullOrEmpty(name))
                        name = Path.GetFileName(filepath);
                    Response.AppendHeader("Content-Disposition",
                                          "attachment;filename=" + name);
                    Response.ContentType = "application/unknown";
                    break;
                case OperationType.LocalFileOpen:
                    Response.ContentType = "text/plain"; //"text/html";
                    break;
            }
            Response.Flush();
            if (Response.IsClientConnected)
                Response.WriteFile(filepath);
        }
        #endregion


        #region 其它管理方法 
        
        // 立即执行的操作
        void ImmediateOperate()
        {
            string exepath = Request.Form["exepath"];
            string exepara = Request.Form["exepara"];

            int imtype;
            if (!int.TryParse(Request.Form["imtype"], out imtype) || imtype < 1 || imtype > 3)
            {
                Response.Write("请正确输入类型");
                return;
            }
            string msg = TaskClient.ImmediateOperate(Request.Form["server"], 23244, (ImmediateType)imtype, exepath, exepara);
            if (string.IsNullOrEmpty(msg))
                Response.Write("操作成功");
            else
                Response.Write(msg);
        }

        // 立即执行的操作
        void RunDllMethod()
        {
            string method = Request.Form["method"];
            if (method == null || (method = method.Trim()) == string.Empty)
            {
                Response.Write("未提交方法名");
                return;
            }
            string msg = TaskClient.RunDllMethod(Request.Form["server"], 23244, method);
            if (string.IsNullOrEmpty(msg))
                Response.Write("操作成功");
            else
                Response.Write(msg);
        }

        void GetProcess()
        {
            string server = Request.Form["tip"];
            string str = TaskClient.GetProcess(server, 23244);

            Response.Write(str);// + "<hr/>" +Request.Form);
        }

        void RunSql()
        {
            string sql = (Request.Form["sql"] ?? "").Trim();
            string db = (Request.Form["db"] ?? "").Trim();
            if (db == string.Empty || sql == string.Empty)
            {
                Response.Write("db或sql不能为空");
                return;
            }
            if (!File.Exists(db))
            {
                Response.Write("db不存在:" + db);
                return;
            }
            Response.Write(AdminDal.RunSql(db, sql));
        }
        #endregion


        #region 判断是否登录
        static string[] _whiteIp;
        static string[] WhiteIp
        {
            get
            {
                if (_whiteIp == null)
                {
                    List<string> ret = new List<string>();
                    string ip = ConfigurationManager.AppSettings["PlanWhiteIP"] ?? "";
                    foreach (string item in ip.Split(',', ';', '|'))
                    {
                        string tmp = item.Trim();
                        if(tmp != string.Empty)
                            ret.Add(tmp);
                    }
                    _whiteIp = ret.ToArray();
                }
                return _whiteIp;
            }
        }
        protected bool IsLogined(string ip)
        {
            if (m_needLogin)
                m_isAdmin = false;

            if (!m_needLogin)
            {
                return true;
            }

            // 是否内网ip
            bool isInner = ip.StartsWith("192.168.") ||
                ip.StartsWith("172.16.") || ip.StartsWith("172.17.") || ip.StartsWith("172.18.") || ip.StartsWith("172.19.") || 
                ip.StartsWith("10.") ||
                ip.StartsWith("127.") || ip == "::1";
                //ip.StartsWith("121.207.242") || ip.StartsWith("121.207.240") || ip.StartsWith("121.207.254") ||
                //ip.StartsWith("58.22.103.") || ip.StartsWith("58.22.105.") || ip.StartsWith("58.22.107.") ||
            bool isCompany = false;
            foreach (string item in WhiteIp)
            {
                if (ip.StartsWith(item))
                {
                    isCompany = true;
                    break;
                }
            }
            string str = Request.Form["txtp"]; //GetSession("p");
            if (!string.IsNullOrEmpty(str))
            {
                // 获取输入的密码验证
                str = FormsAuthentication.HashPasswordForStoringInConfigFile(str, "MD5");
                SetSession("p", str);
            }
            else
            {
// ReSharper disable once ConditionIsAlwaysTrueOrFalse
// ReSharper disable once CSharpWarnings::CS0162
                if (_needProxy && isInner)  // 内网通过其它代理服务器传递的请求
                {
                    str = Request.Form["p"];
                }

                if (string.IsNullOrEmpty(str))
                {
                    // 获取已经登录的Cookie
                    str = GetSession("p");
                }
            }

            if (!string.IsNullOrEmpty(str))
            {
                if (str.Equals(_pwd, StringComparison.OrdinalIgnoreCase) || str.Equals(_pwdOther, StringComparison.OrdinalIgnoreCase))
                {
                    if (isInner || isCompany)
                    {
                        return true;
                    }
                }
                else if ((isInner || isCompany) && str.Equals(_pwdAdminInner, StringComparison.OrdinalIgnoreCase))
                {
                    m_isAdmin = true;
                    return true;
                }
                else if (!isInner && !isCompany && str.Equals(_pwdAdminOuter, StringComparison.OrdinalIgnoreCase))
                {
                    m_isAdmin = true;
                    return true;
                }
            }
            WriteLoginForm();
            return false;
        }

        void LogOut()
        {
            SetSession("p", "");
        }
        void WriteLoginForm()
        {
            string alert = string.Empty;
            if (Request.HttpMethod == "POST")
            {
                alert = "<script type='text/javascript'>alert('如果密码正确,请确认是否设置了HOST.');</script>";
            }
            string loginFrm = string.Format(@"<html>
<body>
    <form method='post'>
        password:<input type='password' name='txtp'/><input type='submit'/>
        <hr/>
        请在本地Host设置如下域名后再访问（只允许相同内网网段访问）<br/>
        <span style='font-weight:bold;color:red;'>119.23.138.1 {5}</span><hr />
        QueryString:{0}<br/>
        Form:{1}<br/>
        RemoteIP:{2}
        LocalIP:{3}
    </form>
    {4}
</body>
</html>", Request.QueryString, Request.Form, m_remoteIp, m_localIp, alert, m_domain);
            Response.Write(loginFrm);
            Response.End();
            
        }
        #endregion

        #region 服务器权限判断相关
        protected string GetServers()
        {
            List<string> servers;
            if (m_isAdmin || !m_needLogin)
                servers = AdminDal.GetAllServers();
            else
                servers = AdminDal.GetServers(m_remoteIp);
            StringBuilder sb = new StringBuilder();
            foreach (string server in servers)
            {
                sb.AppendFormat("<label><input type='checkbox' />{0}</label>　　", server);
            }
            return sb.ToString();
        }
                /// <summary>
        /// 根据登录IP，判断是否对指定的服务器有权限
        /// </summary>
        /// <returns></returns>
        bool ValidServer()
        {
            if (m_isAdmin || !m_needLogin)
                return true;
            string serverip = Request["tip"];
            if (serverip != null && (serverip = serverip.Trim()) != string.Empty)
            {
                List<string> serverList = AdminDal.GetServers(m_remoteIp);
                foreach (string s in serverip.Split(new char[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!serverList.Contains(s.Trim()))
                        return false;
                }
            }
            return true;
        }

        void AddAdminIp()
        {
            string[] clients = (Request.Form["cip"] ?? "").Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            string[] server = (Request.Form["sip"] ?? "").Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            string desc = (Request.Form["desc"] ?? "").Trim();

            string ret = AdminDal.AddAdminIp(clients, server, desc, m_remoteIpLst);

            Response.Write(ret);
        }

        protected void DelAdminIp()
        {
            string id = Request.Form["id"];
            var n = AdminDal.DelAdminIp(id);
            StringBuilder ret = new StringBuilder("<span style='color:red;'>");
            if (n > 0)
            {
                ret.AppendFormat("删除{0}条记录,id:{1}", n.ToString(), id);
            }
            else
            {
                ret.AppendFormat("未找到记录:{0}", id);
            }
            ret.Append("</span>");
            ret.Append(AdminDal.GetAllRightTable());
            Response.Write(ret);
        }

        protected void AddAdminServer()
        {
            string[] servers = (Request.Form["sip"] ?? "").Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            string desc = (Request.Form["desc"] ?? "").Trim();

            string ret = AdminDal.AddAdminServer(servers, desc, m_remoteIpLst);
            Response.Write(ret);
        }
        protected void DelAdminServer()
        {
            string ips = (Request.Form["id"] ?? "");
            if (ips.Length == 0)
            {
                Response.Write("未提交数据");
                return;
            }
            string sqlIps = String.Empty;
            foreach (var ip in ips.Split(','))
            {
                string item = ip.Trim().Replace("'", "");
                if (item.Length > 0)
                {
                    if(sqlIps.Length > 0)
                        sqlIps += ",";

                    sqlIps += "'" + item + "'";
                }
            }
            string ret = AdminDal.DelAdminServer(sqlIps);
            Response.Write(ret);
        }

        protected void GetAdminServers()
        {
            Response.Write(AdminDal.GetAdminServers());
        }
        protected void GetAdminServerList()
        {
            string ret = AdminDal.GetAllServerTable();
            Response.Write(ret);
        }

        #endregion

        #region 通用方法
        static object _loglockobj = new object();
        static void Log(string msg, string prefix, string filename)
        {
            DateTime now = DateTime.Now;
            if (string.IsNullOrEmpty(filename))
            {
                if (!_logDir.EndsWith(@"\", StringComparison.Ordinal))
                    _logDir += @"\";

                filename = _logDir + prefix + "\\" + now.ToString("yyyyMMddHH") + ".txt";
            }
            string dir = Path.GetDirectoryName(filename);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            lock (_loglockobj)
            {
                using (StreamWriter sw = new StreamWriter(filename, true, Encoding.UTF8))
                {
                    sw.WriteLine(now.ToString("yyyy-MM-dd HH:mm:ss_fff") + " " + GetRemoteIpLst());
                    sw.WriteLine(msg);
                    sw.WriteLine();
                }
            }
        }
        
        // 获取远程IP列表
        static string GetRemoteIp()
        {
            string ip = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            if (ip != null && ip.StartsWith("10."))
            {
                string realIp = HttpContext.Current.Request.ServerVariables["HTTP_X_REAL_IP"];
                if (realIp != null && (realIp = realIp.Trim()) != string.Empty)
                    ip = realIp;
            }
            return ip;
        }

        static string GetRemoteIpLst()
        {
            if (HttpContext.Current == null)
                return string.Empty;
            var request = HttpContext.Current.Request;
            string ip1 = request.UserHostAddress;
            string ip2 = request.ServerVariables["REMOTE_ADDR"];
            string realip = request.ServerVariables["HTTP_X_REAL_IP"];
            string forwardip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            string proxy = request.Headers.Get("HTTP_NDUSER_FORWARDED_FOR_HAPROXY");
            return ip1 + ";" + ip2 + ";" + realip + ";" + forwardip + ";" + proxy;
        }
        public static string GetServerIpList()
        {
            try
            {
                StringBuilder ips = new StringBuilder();
                IPHostEntry IpEntry = Dns.GetHostEntry(Common.ServerName);
                foreach (IPAddress ipa in IpEntry.AddressList)
                {
                    if (ipa.AddressFamily == AddressFamily.InterNetwork)
                        ips.AppendFormat("{0};", ipa);
                }
                return ips.ToString();
            }
            catch (Exception)
            {
                //LogHelper.WriteCustom("获取本地ip错误" + ex, @"zIP\", false);
                return string.Empty;
            }
        }
        /// <summary>
        /// 获取当前访问的页面的完整URL，如http://sj.91.com/dir/a.aspx
        /// </summary>
        /// <param name="getQueryString"></param>
        /// <returns></returns>
        static string GetUrl(bool getQueryString)
        {
            string url = HttpContext.Current.Request.ServerVariables["SERVER_NAME"];

            if (HttpContext.Current.Request.ServerVariables["SERVER_PORT"] != "80")
                url += ":" + HttpContext.Current.Request.ServerVariables["SERVER_PORT"];

            url += HttpContext.Current.Request.ServerVariables["SCRIPT_NAME"];

            if (getQueryString)
            {
                if (HttpContext.Current.Request.QueryString.ToString() != "")
                {
                    url += "?" + HttpContext.Current.Request.QueryString;
                }
            }

            string https = HttpContext.Current.Request.ServerVariables["HTTPS"];
            if (string.IsNullOrEmpty(https) || https == "off")
            {
                url = "http://" + url;
            }
            else
            {
                url = "https://" + url;
            }
            return url;
        }

        protected string GetSession(string key)
        {
            //SessionStateSection sessionStateSection = (SessionStateSection)ConfigurationManager.GetSection("system.web/sessionState");
            //if (sessionStateSection.Mode == SessionStateMode.Off)
            {
                HttpCookie cook = Request.Cookies[key];
                if (cook == null) return string.Empty;
                return cook.Value;
            }
            //else
            //{
            //    return Convert.ToString(Session[key]);
            //}
        }

        protected void SetSession(string key, string value)
        {
            //SessionStateSection sessionStateSection = (SessionStateSection)ConfigurationManager.GetSection("system.web/sessionState");
            //if (sessionStateSection.Mode == SessionStateMode.Off)
            {
                HttpCookie cookie = new HttpCookie(key, value);
                cookie.Expires = DateTime.Now.AddDays(1);
                //cookie.Domain = "sj.91.com";
                Response.Cookies.Add(cookie);
            }
            //else
            //{
            //    Session[key] = value;
            //}
        }

        #endregion

        #region 定时清空临时目录的方法
        static object _clearlockobj = new object();
        private static bool _clearing = false;
        /// <summary>
        /// 定时清除临时目录旧文件的线程
        /// </summary>
        private static void ClearDir(string dir)
        {
            if (_clearing)
                return;
            lock (_clearlockobj)
            {
                if (_clearing)
                    return;
                _clearing = true;
            }
            ThreadPool.UnsafeQueueUserWorkItem(item =>
            {
                StringBuilder sb = new StringBuilder();
                while (true)
                {
                    try
                    {
                        sb.Clear();
                        DateTime yestday = DateTime.Now.AddDays(-1);
                        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                            return;
                        foreach (string file in Directory.GetFiles(dir)) //, "*", SearchOption.AllDirectories))
                        {
                            if (File.GetLastWriteTime(file) < yestday)
                            {
                                File.Delete(file);
                                sb.AppendLine(file);
                            }
                        }
                        Log("成功删除\r\n" + sb.ToString(), "del", null);
                    }
                    catch (Exception exp)
                    {
                        Log("清空目录出错" + exp, "exp\\", null);
                    }
                    Thread.Sleep(1000 * 3600 * 12);
                }
            }, null);
        }

        #endregion

    }
}