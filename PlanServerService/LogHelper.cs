using System;
using System.Data.SqlClient;
using NLog;
using System.Text;
using System.Web;

namespace PlanServerService
{
    /// <summary>
    /// 日志记录类
    /// </summary>
    public class LogHelper
    {
        private static readonly bool isinit = false;

        static LogHelper()
        {
            if (isinit == false)
            {
                isinit = true;
                SetConfig();
            }
        }

        private static bool LogInfoEnable = false;
        private static bool LogErrorEnable = false;
        private static bool LogExceptionEnable = false;
        private static bool LogComplementEnable = false;
        private static bool LogDubugEnable = false;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly Logger loggerCustom = LogManager.GetLogger("LogCustom");


        /// <summary>
        /// 设置初始值。
        /// </summary>
        public static void SetConfig()
        {
            LogInfoEnable = logger.IsInfoEnabled;
            LogErrorEnable = logger.IsErrorEnabled;
            LogExceptionEnable = logger.IsErrorEnabled;
            LogComplementEnable = logger.IsTraceEnabled;
            LogDubugEnable = logger.IsDebugEnabled;

        }
        /// <summary>
        /// 写入普通日志消息
        /// </summary>
        /// <param name="info"></param>
        public static void WriteInfo(string info)
        {
            if (LogInfoEnable)
            {
                logger.Info(BuildMessage(info));
            }
        }
        /// <summary>
        /// 写入Debug日志消息
        /// </summary>
        /// <param name="info"></param>
        public static void WriteDebug(string info)
        {
            if (LogDubugEnable)
            {
                logger.Debug(BuildMessage(info));
            }
        }
        /// <summary>
        /// 写入错误日志消息
        /// </summary>
        /// <param name="info"></param>
        public static void WriteError(string info)
        {
            if (LogErrorEnable)
            {
                logger.Error(BuildMessage(info));
            }
        }

        /// <summary>
        /// 写入异常日志信息
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ex"></param>
        public static void WriteException(string info, Exception ex)
        {
            if (LogExceptionEnable)
            {
                logger.Error(BuildMessage(info, ex));
            }
        }

        /// <summary>
        /// 写入严重错误日志消息
        /// </summary>
        /// <param name="info"></param>
        public static void WriteFatal(string info)
        {
            if (LogErrorEnable)
            {
                logger.Fatal(BuildMessage(info));
            }
        }
        /// <summary>
        /// 写入补充日志
        /// </summary>
        /// <param name="info"></param>
        public static void WriteComplement(string info)
        {
            if (LogComplementEnable)
            {
                logger.Trace(BuildMessage(info));
            }
        }
        /// <summary>
        /// 写入补充日志
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ex"></param>
        public static void WriteComplement(string info, Exception ex)
        {
            if (LogComplementEnable)
            {
                logger.Trace(BuildMessage(info, ex));
            }
        }


        /// <summary>
        /// 补充一些常见的上下文日志，方便问题排查
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        static string BuildMessage(string info, Exception ex = null)
        {
            StringBuilder sb = new StringBuilder();
            HttpRequest request = null;
            try
            {
                if (HttpContext.Current != null)
                    request = HttpContext.Current.Request;
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
                // 在Web站点的Application_Start里，不允许访问 HttpContext.Current 上下文
            }
            sb.AppendFormat("Time:{0}-{1}\r\n", DateTime.Now, info);

            if (request != null)
            {
                // 存在http上下文时，把请求的url和ip等信息带上
                sb.AppendFormat("Url:{0}\r\n", request.Url);

                var post = Convert.ToString(request.Form);
                if (post.Length > 0)
                {
                    sb.AppendFormat("Post: {0}\r\n", post);
                }

                var header = Convert.ToString(request.Headers);
                if (header.Length > 0)
                {
                    sb.AppendFormat("Header: {0}\r\n", header);
                }

                var referer = Convert.ToString(request.UrlReferrer);
                if (referer.Length > 0)
                {
                    sb.AppendFormat("UrlReferrer:{0}\r\n", referer);
                }
                string realip = request.ServerVariables["HTTP_X_REAL_IP"];
                sb.AppendFormat("UserHostAddress:{0};{1}\r\n", request.UserHostAddress, realip);
                sb.AppendFormat("WebServer:{0}\r\n", request.ServerVariables["LOCAL_ADDR"]);
            }

            if (ex != null)
            {
                var sqlException = ex as SqlException;
                if (sqlException != null)
                    sb.AppendFormat("Database:{0}\r\n", sqlException.Server);
                sb.AppendFormat("Exception:{0}\r\n", ex);
            }
            sb.AppendLine();
            return sb.ToString();
        }


        /// <summary>
        /// 写入自定义日志到自定义目录,本方法对应的Nlog.config配置示例：
        ///  &lt;targets>
        ///    &lt;target name="LogCustom" xsi:type="File" layout="${message}"
        ///          fileName="${logDirectory}\${event-context:DirOrPrefix}${date:format=yyyyMMddHH}.txt">&lt;/target>
        ///  &lt;/targets>
        ///  &lt;rules>
        ///    &lt;logger name="LogCustom" level="Warn" writeTo="LogCustom" />
        /// </summary>
        /// <param name="message">要写入的消息</param>
        /// <param name="dirOrPrefix">
        /// 写入到的子目录或文件前缀，如果字符串包含\，则是子目录
        /// 比如 aa\bb 则写入的文件名为aa目录下的bb开头加日期
        /// </param>
        /// <param name="addIpUrl">是否要附加ip和url等信息</param>
        public static void WriteCustom(string message, string dirOrPrefix, bool addIpUrl = true)
        {
            WriteCustom(message, dirOrPrefix, null, addIpUrl);
        }

        /// <summary>
        /// 写入自定义日志到自定义目录,本方法对应的Nlog.config配置示例：
        ///  &lt;targets>
        ///    &lt;target name="LogCustom" xsi:type="File" layout="${message}"
        ///          fileName="${logDirectory}\${event-context:DirOrPrefix}${date:format=yyyyMMddHH}${event-context:Suffix}.txt">&lt;/target>
        ///  &lt;/targets>
        ///  &lt;rules>
        ///    &lt;logger name="LogCustom" level="Warn" writeTo="LogCustom" />
        /// </summary>
        /// <param name="message">要写入的消息</param>
        /// <param name="dirOrPrefix">
        /// 写入到的子目录或文件前缀，如果字符串包含\，则是子目录
        /// 比如 aa\bb 则写入的文件名为aa目录下的bb开头加日期
        /// </param>
        /// <param name="suffix">写入到的文件后缀</param>
        /// <param name="addIpUrl">是否要附加ip和url等信息</param>
        public static void WriteCustom(string message, string dirOrPrefix, string suffix, bool addIpUrl = true)
        {
            if (addIpUrl)
                message = BuildMessage(message);
            else
                message = "\r\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "-" + message;

            LogEventInfo logEvent = new LogEventInfo(LogLevel.Trace, loggerCustom.Name, message);
            logEvent.Context["DirOrPrefix"] = dirOrPrefix;
            if (suffix != null)
                logEvent.Context["Suffix"] = suffix;
            loggerCustom.Log(logEvent);
        }
    }
}
