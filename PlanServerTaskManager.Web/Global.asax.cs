using System;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using PlanServerService;
using PlanServerService.Ext;

namespace PlanServerTaskManager.Web
{
    public class Global : System.Web.HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            // 程序池启动日志
            LogHelper.WriteCustom(DateTime.Now + " Application_Start", "AppStartEnd\\", false);
            // 注册完整GC通知，在gc回收时记录日志
            GCNotification.Register();

            // 初始化ip纯真库，用于ip地区判断
            // IPLocator.Initialize(Server.MapPath(@"qqwry.dat"));
        }


        void Application_End(object sender, EventArgs e)
        {
            string message = string.Empty;
            HttpRuntime runtime = (HttpRuntime)typeof(HttpRuntime).InvokeMember("_theRuntime", 
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField, null, null, null);
            if (runtime != null)
            {
                Type type = runtime.GetType();
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField;
                string shutDownMessage = (string)type.InvokeMember("_shutDownMessage", flags, null, runtime, null);
                string shutDownStack = (string)type.InvokeMember("_shutDownStack", flags, null, runtime, null);
                message = string.Format("\r\nshutDownMessage:{0}\r\nshutDownStack:\r\n:{1}", shutDownMessage, shutDownStack);
            }
            LogHelper.WriteCustom("Application_End " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + message, "AppStartEnd\\");

            // 站点停止时，写入pv
            if (pvCount > 0)
            {
                LogHelper.WriteCustom(DateTime.Now + " Application_End " + pvCount.ToString(), @"zPV\", false);
            }
        }

        /// <summary>
        /// 服务器ip list，用于日志记录
        /// </summary>
        public static string serverIpList = Common.GetServerIpList();
        /// <summary>
        /// 最近一次访问时间，用于服务器是否下线的判断
        /// </summary>
        public static DateTime LAST_ACCESS_TIME = DateTime.MinValue;


        void Application_BeginRequest(object sender, EventArgs e)
        {
            // Global的Application_EndRequest里收集，便于分析超时原因
            var msg = HttpContext.Current.Timestamp.ToString("HH:mm:ss.fff") + ";" +
                      DateTime.Now.ToString("HH:mm:ss.fff");
            HttpContext.Current.Items["caltime"] = msg;
        }

        void Application_EndRequest(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            string normalurl = Request.Url.ToString();
            string url = normalurl.ToLower();
            var accessType = GetUserAccessType(url);

            #region 超过2秒时，记录请求结束时间
            double m = (now - HttpContext.Current.Timestamp).TotalMilliseconds;
            // 监控页面输出时间
            if (url.SundaySearch("iswebmon=") >= 0)
            {
                string time2 = string.Format("\r\n{0} End:{1} use time:{2}ms, Post len:{3}, ip:{4}",
                    HttpContext.Current.Items["caltime"],
                    now.ToString("HH:mm:ss.fff"),
                    m.ToString("N0"),
                    Convert.ToString(Request.Form).Length,
                    serverIpList);
                Response.Write(time2);
            }
            if (m > 2000 && now.Hour != 5)
            {
                string time2 = string.Format("{0} End:{1} \r\nuse time:{2}ms, Post len:{3}\r\n",
                    HttpContext.Current.Items["caltime"],
                    now.ToString("HH:mm:ss.fff"),
                    m.ToString("N0"),
                    Convert.ToString(Request.Form).Length);

                LogHelper.WriteCustom(time2, "CalTime\\");// + GetFilename(Request.Url.ToString())); // + file);
            }
            #endregion
        
            PvCount(url);

            // 记录活动时间，用于判断lvs是否活动中
            if (accessType == AccessTypeOption.User)
            {
                LAST_ACCESS_TIME = now;
            }
        }

        void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError().InnerException ?? Server.GetLastError();
            if (ex is ThreadAbortException)
            {
                // 不记录Response.End引发的异常
                Thread.ResetAbort();
                HttpContext.Current.ClearError();
                return;
            }

            HttpException exp404 = ex as HttpException;
            if (exp404 != null)
            {
                int erCode = exp404.GetHttpCode();
                if (erCode == 404)
                {
                    LogHelper.WriteCustom(ex.Message, "404err\\");
                    HttpContext.Current.ClearError();
                    return;
                }
                if (erCode == 400)
                {
                    LogHelper.WriteCustom(ex.Message, "400err\\");
                    HttpContext.Current.ClearError();
                    return;
                }
            }

            string msg = string.Format("\r\nGlobal异常: Post数据:{0}\r\nHeaders:\r\n{1}",
                Request.Form, GetHeaders());

            HttpRequestValidationException validationExp = ex as HttpRequestValidationException;
            if (validationExp != null)
            {
                LogHelper.WriteCustom(msg, "expValidation\\");
                HttpContext.Current.ClearError();
                return;
            }

            LogHelper.WriteException(msg, ex);

#if !DEBUG
            HttpContext.Current.ClearError();
            Response.Redirect("http://suggestion.91.com/404.html", false);
            HttpContext.Current.ApplicationInstance.CompleteRequest();
#endif
        }

        //void Session_Start(object sender, EventArgs e)
        //{
        //    // Code that runs when a new session is started

        //}

        //void Session_End(object sender, EventArgs e)
        //{
        //    // Code that runs when a session ends. 
        //    // Note: The Session_End event is raised only when the sessionstate mode
        //    // is set to InProc in the Web.config file. If session mode is set to StateServer 
        //    // or SQLServer, the event is not raised.

        //}


        static string GetHeaders()
        {
            return GetHeaders(HttpContext.Current);
        }

        public static string GetHeaders(HttpContext context)
        {
            var headers = new StringBuilder();
            foreach (string header in context.Request.Headers)
            {
                if (header.Equals("cookie", StringComparison.OrdinalIgnoreCase))
                {
                    headers.AppendFormat("    {0}:\r\n", header);
                    var cookie = context.Request.Headers[header];
                    foreach (var c in cookie.Split(';'))
                    {
                        headers.AppendFormat("        {0}\r\n", c);
                    }
                }
                else
                {
                    headers.AppendFormat("    {0}:{1}\r\n", header, context.Request.Headers[header]);
                }
            }
            return headers.ToString();
        }


        #region 用于站点pv统计的代码
        public static int pvCount
        {
            get
            {
                return AccessTotal.GetNum((int)AccessTypeOption.User);
            }
        }
        static void PvCount(string url)
        {
            //if (CustomConfigHelper.GetBooleanWithType("EnableCountPV"))
            {
                AccessTypeOption act = GetUserAccessType(url);
                // 内存中统计，每分钟提交一次
                AccessTotal.IncGlobal(act);
            }
        }
        #endregion

        // 参数url必须已经ToLower了
        static AccessTypeOption GetUserAccessType(string url)
        {
            AccessTypeOption act;
            if (url.SundaySearch("iswebmon=") > 0)
            {
                act = AccessTypeOption.Monitor; // 站点监控程序访问
            }
            else if (url.SundaySearch("service.ashx") >= 0 ||
                  url.SundaySearch("service.svc") >= 0)
            {
                act = AccessTypeOption.User; // 表示正常访问
            }
            else
            {
                act = AccessTypeOption.Other; //表示测试访问
            }
            return act;
        }
    }
}
