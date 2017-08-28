using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;
using PlanServerService;

namespace PlanServerTaskManager.Web
{
    public delegate void EventHandler();
    /// <summary>
    /// 统计数据收集相关方法
    /// </summary>
    public static class AccessTotal
    {
        private const int SAVEDIFFTIME = 120;
        private static TimeSpan _saveDiffTime;
        /// <summary>
        /// 每隔多久存一次访问量,默认2分钟
        /// </summary>
        public static int SaveDiffSecond
        {
            get { return (int)_saveDiffTime.TotalSeconds; }
            set
            {
                _saveDiffTime = TimeSpan.FromSeconds(value <= 0 ? SAVEDIFFTIME : value);
            }
        }

        static AccessTotal()
        {
            SaveDiffSecond = SAVEDIFFTIME;

            // 构造函数里启动统计数据入库job
            ThreadPool.UnsafeQueueUserWorkItem(LoopAndSave, null);
        }

        private static string _totalUrl;
        private static string _totalUrlIp;
        /// <summary>
        /// 要提交pv统计的url地址，必须配置，不然统计时会出错
        /// </summary>
        static string TotalUrl
        {
            get
            {
                if (string.IsNullOrEmpty(_totalUrl))
                {
                    string tmp = Common.GetSetting("AccessTotalProxy");
                    if (string.IsNullOrEmpty(tmp))
                    {
                        tmp = "10.79.137.54";
                    }
                    _totalUrlIp = tmp;

                    tmp = Common.GetSetting("AccessTotalUrl");
                    if (string.IsNullOrEmpty(tmp))
                    {
                        tmp = "http://messageadmin.91.com/accessTotal.ashx";
                    }

                    int idx = tmp.IndexOf('#');
                    if (idx > 0)
                        tmp = tmp.Substring(0, idx);

                    if (tmp.IndexOf('?') > 0)
                        tmp += "&p=" + Project;
                    else
                        tmp += "?p=" + Project;

                    string serverIpList = Common.GetServerIpList();
                    tmp += "&sip=" + serverIpList;

                    _totalUrl = tmp;
                }
                return _totalUrl;
            }
        }

        /// <summary>
        /// 当前项目key，用于区分统计数据
        /// </summary>
        private static string Project
        {
            get
            {
                string tmp = Common.GetSetting("AccessTotalProject");
                if (string.IsNullOrEmpty(tmp))
                {
                    throw new Exception("请在Config文件中配置AccessTotalProject");
                }
                return tmp;
            }
        }
        
        static ConcurrentDictionary<int, int> _totals = new ConcurrentDictionary<int, int>();
        // 临时变量，用于保存数据用
        static ConcurrentDictionary<int, int> _totalSave = new ConcurrentDictionary<int, int>();

        /// <summary>
        /// 根据指定的参数类型，对该类型进行+1操作.
        /// act可以自行灵活配置，把平台之类也组合进来
        /// </summary>
        /// <param name="act"></param>
        public static void Increment(int act = 0)
        {
            _totals.AddOrUpdate(act, 1, (key, val) => val + 1);
            //Interlocked.Increment()
        }

        /// <summary>
        /// 专用于Global.asax.cs里的访问统计代码
        /// </summary>
        /// <param name="option"></param>
        public static void IncGlobal(AccessTypeOption option)
        {
            if (option != AccessTypeOption.Monitor && option != AccessTypeOption.Other && 
                option != AccessTypeOption.User && option != AccessTypeOption.AccessTotal)
                throw new Exception("此方法只能统计通用类型数据:Monitor,Other,User");
            Increment((int)option);
        }

        /// <summary>
        /// 获取指定类型的访问量
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public static int GetNum(int act = 0)
        {
            int num;
            _totals.TryGetValue(act, out num);
            return num;
        }

        private static void LoopAndSave(object state)
        {
            // 这个循环，是确保统计从整的双数分钟开始
            while (true)
            {
                var now = DateTime.Now;
                if (now.Second == 0 && now.Minute % 2 == 0)
                    break;
                Thread.Sleep(100);
            }

            while (true)
            {
                try
                {
                    SaveAll();

                    if (Update != null)
                    {
                        Update();
                    }

                    if (OtherStat != null)
                    {
                        OtherStat();
                    }
                }
                catch (Exception exp)
                {
                    LogHelper.WriteException("保存统计异常1:" + exp.Message, null);
                }

                Thread.Sleep(_saveDiffTime);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        /// <summary>
        /// 项目自定义的更新方法，默认2分钟调用一次
        /// </summary>
        public static event EventHandler Update;
        /// <summary>
        /// 项目自定义的统计方法，默认2分钟调用一次
        /// </summary>
        public static event EventHandler OtherStat;

        /// <summary>
        /// 序列化统计数据，并发送到指定url保存
        /// </summary>
        private static void SaveAll()
        {
            if (string.IsNullOrEmpty(TotalUrl) || _totals.Count <= 0)
                return;

            try
            {
                // 交换临时变量
                _totalSave.Clear();
                var tmp = _totalSave;
                _totalSave = _totals;
                _totals = tmp;

                // todo: 未完成功能
                byte[] data = null;//Core.ThirdPartyReference.SerializeHelper.ProtobufSerialize(_totalSave);
                if (data == null || data.Length <= 0)
                    return;

                GetPage(TotalUrl, data, _totalUrlIp);
                //LogHelper.WriteCustom(TotalUrl + "\r\n"+ str, "xxx\\");
            }
            catch (Exception exp)
            {
                LogHelper.WriteException("访问统计异常:" + exp.Message, null);
            }
        }

        // POST获取网页内容
        static void GetPage(string url, byte[] param, string proxy)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");
            //request.Headers.Add("Accept-Charset", "utf-8");
            request.UserAgent = "AccessTotal";
            request.AllowAutoRedirect = false; //出现301或302之类的转向时，是否要转向，默认true
            if (!string.IsNullOrEmpty(proxy))
            {
                string[] tmp = proxy.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                int port = 80;
                if (tmp.Length >= 2)
                    if (!int.TryParse(tmp[1], out port))
                        port = 80;
                request.Proxy = new WebProxy(tmp[0], port);
            }
            request.Method = "POST";

            // 正常POST数据用这个
            //request.ContentType = "application/x-www-form-urlencoded";
            // POST二进制，比如文件用这个
            request.ContentType = "application/octet-stream";

            // 设置提交的数据
            if (param != null && param.Length > 0)
            {
                request.ContentLength = param.Length;
                // 必须先设置ContentLength，才能打开GetRequestStream
                // ContentLength设置后，reqStream.Close前必须写入相同字节的数据，否则Request会被取消
                using (Stream newStream = request.GetRequestStream())
                {
                    newStream.Write(param, 0, param.Length);
                    newStream.Close();
                }
            }
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            {
                if (stream == null)
                    return;// new byte[0];
                stream.Close();

                //using (var sr = new StreamReader(stream, Encoding.UTF8))
                //{
                //    return sr.ReadToEnd();
                //}
                //List<byte> ret = new List<byte>(10000);
                //byte[] arr = new byte[10000];
                //int readcnt;
                //while ((readcnt = stream.Read(arr, 0, arr.Length)) > 0)
                //{
                //    for (int i = 0; i < readcnt; i++)
                //        ret.Add(arr[i]);
                //    //ret.AddRange(arr.Take(readcnt));
                //}
                //return ret.ToArray();
            }
        }

    }
}
