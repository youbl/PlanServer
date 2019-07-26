using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace PlanServerService.Hook
{
    /// <summary>
    /// 需要发送通知的特性类
    /// </summary>
    public class DingHookAttribute : BaseHook
    {
        /// <summary>
        /// 要发送Hook通知的url
        /// </summary>
        public static List<string> Url { get; } = new List<string>();

        public string Title { get; private set; }
        public DingHookAttribute() : this("Job操作")
        {
        }

        public DingHookAttribute(string title)
        {
            Title = title;
        }

        /// <summary>
        /// 发送钉钉消息
        /// </summary>
        /// <param name="message"></param>
        public override void Hook(string message)
        {
            // curl "https://oapi.dingtalk.com/robot/send?access_token=aaa" -d '{"msgtype":"link","link":{"text":"test","title":"testtitle","picUrl":"","messageUrl":"http://baidu.com"}}' -H 'content-type: application/json'
            // curl "https://oapi.dingtalk.com/robot/send?access_token=aaa" -d '{"msgtype":"markdown","markdown":{"text":"# test","title":"testtitle"}}' -H 'content-type: application/json'

            //            var msg = "{\"msgtype\":\"link\",\"link\":{\"text\":\"" + ProcessChar(message) +
            //                      "\",\"title\":\"" + ProcessChar(Title) +
            //                      "\",\"picUrl\":\"\",\"messageUrl\":\"http://www.baidu.com\"}}";
            var msg = "{\"msgtype\":\"markdown\",\"markdown\":{\"text\":\"" + ProcessChar(message) +
                      "\",\"title\":\"" + ProcessChar(Title) +
                      "\"}}";
            foreach (var url in Url)
            {
                try
                {
                    var ret = GetPage(url, msg);
                    LogHelper.WriteInfo("Hook: " + ret);
                }
                catch (Exception exp)
                {
                    LogHelper.WriteException("Hook error", exp);
                }
            }
        }

        static string ProcessChar(string message)
        {
            return message.Replace("\"", "\\\"").Replace("\\", "\\\\");
        }
        
        static string GetPage(string url, string jsonMsg)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");
            request.Headers.Add("Accept-Charset", "utf-8");
            request.UserAgent = "beinet1.0";
            request.AllowAutoRedirect = true; //出现301或302之类的转向时，是否要转向
            request.Method = "POST";
            request.ContentType = "application/json";

            // 设置提交的数据
            if (!string.IsNullOrEmpty(jsonMsg))
            {
                // 把数据转换为字节数组
                byte[] l_data = Encoding.UTF8.GetBytes(jsonMsg);
                request.ContentLength = l_data.Length;
                // 必须先设置ContentLength，才能打开GetRequestStream
                // ContentLength设置后，reqStream.Close前必须写入相同字节的数据，否则Request会被取消
                using (Stream newStream = request.GetRequestStream())
                {
                    newStream.Write(l_data, 0, l_data.Length);
                    newStream.Close();
                }
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            {
                if (stream == null)
                    return null;
                using (var sr = new StreamReader(stream, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}
