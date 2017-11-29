<%@ Page Language="C#" EnableViewState="false" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Data.SqlClient" %>
<%@ Import Namespace="System.Diagnostics" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Net" %>
<%@ Import Namespace="System.Net.Sockets" %>
<%@ Import Namespace="System.Reflection" %>
<%@ Import Namespace="System.Threading" %>
<%@ Import Namespace="System.Xml" %>
<%@ Import Namespace="System.Web.Configuration" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<title>工具页</title>

<!-- 要自定义的变量列表 -->    
<script language="c#" runat="server">
    //下面替换为你需要用的md5值，比如a3de83c477b3b24d52a1c8bebbc7747b是sj.91.com
    private const string _pwd = "4c3e1ec04215f69d6a8e9c023c9e4572"; // ip在白名单里时，进入此页面的密码md5值
    protected string _xxx = "8f13267a0d8a9ea0b964e7b5e7b36eb5";     // ip不在白名单时，进入此页面的密码md5值
    //要显示在 配置文本框里的ip列表
    private string m_ipLst = "127.0.0.1;";
</script>
<script language="C#" runat="server">

    private string m_currentUrl;
    private string m_localIp, m_remoteIp, m_remoteIpLst;
    protected override void OnInit(EventArgs e)
    {
        try
        {
            m_localIp = GetServerIpList();
            m_remoteIp = GetRemoteIp();
            m_remoteIpLst = GetRemoteIpLst();
            m_currentUrl = GetUrl(false);

            Log("客户端ip：" + m_remoteIpLst +
                "\r\n服务器ip：" + m_localIp +
                "\r\nUrl：" + Request.Url +
                "\r\nPost：" + Request.Form,
                "", null);

            if (string.IsNullOrEmpty(_pwd))
            {
                Response.Write("未设置密码，请修改页面源码以设置密码\r\n" +
                               m_remoteIp + ";" + m_localIp);
                Response.End();
                return;
            }

            // 检查md5不需要密码flg == "clientdirmd5"
            if ((Request.Form["f"] ?? "") != "clientdirmd5" && !IsLogined(m_remoteIp))
            {
                Response.Write(Request.QueryString + "\r\n<hr />\r\n" + Request.Form + "\r\n<hr />\r\n" +
                               m_remoteIp + ";" + m_localIp);
                Response.End();
                return;
            }

            // 如果提交了ip参数，表示是请求Proxy
            string ip = Request.Form["ip"];
            if(!string.IsNullOrEmpty(ip))
            {
                DoProxy(ip);
                return;
            }

            string flg = Request.Form["flg"];
            if(!string.IsNullOrEmpty(flg))
            {
                flg = flg.Trim().ToLower();
                switch (flg)
                {
                    case "showconfig":
                        RefreshOrShowConfig();
                        break;
                    case "telnet":
                        Telnet();
                        break;
                    case "sql":
                        SqlRun();
                        break;
                    case "redis":
                        Redis();
                        break;
                }
                Response.End();
            }
        }
        catch (ThreadAbortException) { }
        catch (Exception exp)
        {
            Response.Write("客户ip：" + m_remoteIpLst + "；服务器：" + m_localIp + "\r\n" + exp);
            Response.End();
        }
    }

    // 判断是否登录
    protected bool IsLogined(string ip)
    {
        // 是否内网ip
        bool isInner = (ip == "218.85.23.101" || ip == "110.80.152.72" ||
            ip.StartsWith("192.168.") || ip.StartsWith("172.16.") || ip.StartsWith("10.") ||
            ip.StartsWith("127.") || ip == "::1");

        bool redirect = false;
        string str = Request.QueryString["p"];
        if (!string.IsNullOrEmpty(str))
            redirect = true;

        if (!string.IsNullOrEmpty(str))
        {
            str = FormsAuthentication.HashPasswordForStoringInConfigFile(str, "MD5");
            SetSession("p", str);
        }
        else
            str = GetSession("p");

        // ip proxy通过Form提交加密好的密码,proxy只允许是内网ip
        if (string.IsNullOrEmpty(str) && isInner)
            str = Request.Form["p"];

        if (string.IsNullOrEmpty(str))
            return false;
        if (str.Equals(_pwd, StringComparison.OrdinalIgnoreCase))
        {
            if (isInner)
            {
                if (redirect)
                {
                    Response.Redirect(m_currentUrl);
                }
                return true;
            }
        }
        else if (str.Equals(_xxx, StringComparison.OrdinalIgnoreCase))
        {
            if (redirect)
            {
                Response.Redirect(m_currentUrl);
            }
            return true;
        }
        return false;
    }

    static object lockobj = new object();
    static void Log(string msg, string prefix, string filename)
    {
        DateTime now = DateTime.Now;
        if (string.IsNullOrEmpty(filename))
        {
            filename = @"e:\weblogs\zzCustomConfigLog\" + prefix + "\\" + now.ToString("yyyyMMddHH") + ".txt";
        }
        string dir = Path.GetDirectoryName(filename);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        lock (lockobj)
        {
            using (StreamWriter sw = new StreamWriter(filename, true, Encoding.UTF8))
            {
                sw.WriteLine(now.ToString("yyyy-MM-dd HH:mm:ss_fff"));
                sw.WriteLine(msg);
                sw.WriteLine();
            }
        }
    }

    void DoProxy(string ip)
    {
        string[] tmp = ip.Split('_');
        ip = tmp[0];
        string proxyurl;
        if (tmp.Length >= 2)
        {
            proxyurl = "http://" + tmp[1] + "/";
            if (tmp.Length >= 3)
                proxyurl += tmp[2];
            else
                proxyurl += m_currentUrl.Substring(m_currentUrl.LastIndexOf('/') + 1);
        }
        else
            proxyurl = m_currentUrl;
        string para = HttpUtility.UrlDecode(Request.Form.ToString());
        para = System.Text.RegularExpressions.Regex.Replace(para, @"(?:^|&)ip=[^&]+", "");
        para += "&p=" + GetSession("p") + "&cl=" + m_remoteIp;
        byte[] arrBin = GetPage(proxyurl, para, ip);
        string contentType = Request.QueryString["contentType"];
        if (contentType != null)
        {
            if(contentType == "down")
            {
                Response.AppendHeader("Content-Disposition", "attachment;filename=tmp");
                Response.ContentType = "application/unknown";
            }
            else if (contentType == "text")
            {
                Response.ContentType = "text/plain"; //"text/html";
            }
        }
        Response.BinaryWrite(arrBin);
        Response.End();
    }
    </script>
    
<!-- 配置相关的方法集 -->    
<script language="c#" runat="server">
    void RefreshOrShowConfig()
    {
        string type = Request.Form["f"];
        if (string.IsNullOrEmpty(type))
            type = "2";

        string ret = "（客户ip：" + m_remoteIpLst + "；服务器：" + m_localIp + "）" + "\r\n";
        if (type == "2")
        {
            string classname = Request.Form["className"];
            ret += ShowConfigs(classname);
        }
        else if (type == "3")
        {
            string cachename = Request.Form["cache"];
            ret += GetCache(cachename);
        }
        else if (type == "4")
        {
            string cachename = Request.Form["cache"];
            ret += ClearCache(cachename);
        }
        Response.Write(ret);
    }

    static string ShowConfigs(string classname)
    {
        StringBuilder sb = new StringBuilder();
        if(!string.IsNullOrEmpty(classname))
        {
            sb.Append("<a href='javascript:void(0)' onclick='showHide(this);'>============================="
                      + classname + "静态属性列表：========================</a>\r\n<span>");
            Dictionary<string, string> props = GetAllProp(classname);
            if (props == null)
            {
                sb.AppendFormat("  {0} 未找到对应的类定义 \r\n", classname);
            }
            else if (props.Count == 0)
            {
                sb.AppendFormat("  {0} 未找到静态的field或property定义 \r\n", classname);
            }
            else
            {
                foreach (KeyValuePair<string, string> pair in props)
                {
                    sb.AppendFormat("  {0}={1}\r\n", pair.Key.PadRight(31), pair.Value);
                }
            }
            sb.Append("</span>\r\n\r\n");
            return sb.ToString();
        }

        NameValueCollection nameValues = ConfigurationManager.AppSettings;
        if (nameValues.Count > 0)
        {
            sb.Append("<a href='javascript:void(0)' onclick='showHide(this);'>=============================Web.config AppSetting配置列表：========================</a>\r\n<span>");
            SortedList<string, string> appsetting = new SortedList<string, string>();
            foreach (string key in nameValues.AllKeys)
            {
                appsetting.Add(key, nameValues[key]);
            }
            foreach (KeyValuePair<string, string> pair in appsetting)
            {
                sb.AppendFormat("  {0}={1}\r\n", pair.Key.PadRight(31), pair.Value);
            }
            sb.Append("</span>\r\n\r\n");
        }

        List<string> sections = GetConfigSections();
        if (sections != null && sections.Count > 0)
        {
            sb.Append("<a href='javascript:void(0)' onclick='showHide(this);'>=============================Web.config Sections配置列表：========================</a>\r\n<span>");
            foreach (string key in sections)
            {
                sb.AppendFormat("  {0}\r\n", key);
            }
            sb.Append("</span>\r\n\r\n");
        }

        sb.Append("<a href='javascript:void(0)' onclick='showHide(this);'>=============================GC配置：========================</a>\r\n<span>");
        sb.AppendFormat("  gcServer enabled={0}　　　　", System.Runtime.GCSettings.IsServerGC);
        sb.AppendFormat("  GCLatencyMode={0}\r\n", System.Runtime.GCSettings.LatencyMode.ToString());
        sb.Append("</span>\r\n\r\n");

        sb.Append("<a href='javascript:void(0)' onclick='showHide(this);'>=============================运行时相关：========================</a>\r\n<span>");
        sb.AppendFormat("  当前进程内存占用        :{0} 兆 \r\n", (Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0).ToString("N2"));
        sb.AppendFormat("  HttpRuntime.Cache个数   :{0} \r\n", HttpRuntime.Cache.Count.ToString());
        int availableWorkerThreads, availableCompletionPortThreads, maxWorkerThreads, maxPortThreads;
        ThreadPool.GetAvailableThreads(out availableWorkerThreads, out availableCompletionPortThreads);
        ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxPortThreads);

        sb.AppendFormat("  最大线程数:{0}； 最大异步I/O线程数:{1}\r\n", maxWorkerThreads.ToString().PadRight(8), maxPortThreads.ToString());
        sb.AppendFormat("  已用线程数:{0}； 已用异步I/O线程数:{1}\r\n", (maxWorkerThreads - availableWorkerThreads).ToString().PadRight(8), (maxPortThreads - availableCompletionPortThreads).ToString());
        sb.AppendFormat("  空闲线程数:{0}； 空闲异步I/O线程数:{1}\r\n", availableWorkerThreads.ToString().PadRight(8), availableCompletionPortThreads.ToString());
        sb.Append("</span>\r\n\r\n");

        return sb.ToString();
    }


    static Dictionary<string, string> GetAllProp(string classname)
    {
        Type type = GetType(classname);
        if (type == null)
        {
            return null;
        }
        Dictionary<string, string> ret = new Dictionary<string, string>();
        FieldInfo[] arrfield = type.GetFields(BindingFlags.Static | BindingFlags.Public);
        foreach (FieldInfo info in arrfield)
        {
            ret.Add(info.Name, Convert.ToString(info.GetValue(null)));
        }
        arrfield = type.GetFields(BindingFlags.Static | BindingFlags.NonPublic);
        foreach (FieldInfo info in arrfield)
        {
            ret.Add(info.Name, Convert.ToString(info.GetValue(null)));
        }
        PropertyInfo[] arrprop = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
        foreach (PropertyInfo info in arrprop)
        {
            ret.Add(info.Name, Convert.ToString(info.GetValue(null, null)));
        }
        arrprop = type.GetProperties(BindingFlags.Static | BindingFlags.NonPublic);
        foreach (PropertyInfo info in arrprop)
        {
            ret.Add(info.Name, Convert.ToString(info.GetValue(null, null)));
        }
        return ret;
    }

    static Type GetType(string classname)
    {
        try
        {
            Assembly assembly = null;
            // 循环命名空间，找到对应的Assembly
            int idx = classname.LastIndexOf(".", StringComparison.Ordinal);
            while (idx > 0)
            {
                string assemName = classname.Substring(0, idx);
                try
                {
                    assembly = Assembly.Load(assemName);
                    break;
                }
                catch
                {
                    idx = classname.LastIndexOf(".", idx - 1, StringComparison.Ordinal);
                }
            }
            if (assembly == null)
            {
                return null;
            }
            return assembly.GetType(classname);
        }
        catch
        {
            return null;
        }
    }

    // 遍历configSections配置
    static List<string> GetConfigSections()
    {
        List<string> ret = new List<string>();
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(HttpContext.Current.Server.MapPath("Web.config"));
        XmlNode sectionNode = xmlDoc.SelectSingleNode("configuration//configSections");
        if (sectionNode == null)
            return ret;

        XmlNodeList nodeList = sectionNode.ChildNodes;
        //遍历所有子节点   
        foreach (XmlNode xn in nodeList)
        {
            //将子节点类型转换为XmlElement类型       
            XmlElement xe = xn as XmlElement;
            if (xe == null || xe.Name != "section")
                continue;
            string secName = xe.GetAttribute("name");
            try
            {
                object section = ConfigurationManager.GetSection(secName);
                if (section == null)
                    continue;
                ret.Add(secName + "\r\n" + GetValue(section, true));
            }
            catch(Exception exp)
            {
                ret.Add(secName + "\r\n加载失败： " + exp.Message);
            }
        }
        return ret;
    }

    // 在obj对象里查找公共属性，并返回属性的名值
    static string GetValue(object obj, bool newline)
    {
        StringBuilder l_ret = new StringBuilder();
        try
        {
            BindingFlags flags =
                BindingFlags.GetProperty |
                BindingFlags.Public |
                BindingFlags.Instance;

            PropertyInfo[] infos = obj.GetType().GetProperties(flags);
            foreach (PropertyInfo info in infos)
            {
                if (info.Name == "Item")
                    continue;
                object val = info.GetValue(obj, null);
                if (val == null)
                    continue;
                string strVal = GetValueDetail(val);
                if (string.IsNullOrEmpty(strVal))
                    continue;
                if (newline)
                    l_ret.AppendFormat("\t{0}:{1}\r\n", info.Name, strVal);
                else
                    l_ret.AppendFormat(" {0}:{1};", info.Name, strVal);
            }
        }
        catch(Exception exp)
        {
            return exp.ToString();
        }
        return l_ret.ToString();
    }

    static string GetValueDetail(object val)
    {
        if (val is string)
            return val.ToString();

        string strVal = string.Empty;
        if (val is ValueType)
        {
            strVal = val.ToString();
            // 不返回默认值，比如0，False等
            if (strVal == Activator.CreateInstance(val.GetType()).ToString())
                return string.Empty;
            return strVal;
        }

        ICollection arr = val as ICollection;
        if (arr != null)
        {
            foreach (object o in arr)
            {
                strVal += GetValueDetail(o) + ";";
            }
        }
        else if (val.ToString() == "Res91com.ResourceDataAccess.MongoDB.ServerConfiguration")
        {
            strVal = GetValue(val, false) + "|---|";
        }
        return strVal;
    }

    static string GetCache(string cachename)
    {
        StringBuilder sb = new StringBuilder();
        int cnt = 0;
        try
        {
            if (!string.IsNullOrEmpty(cachename))
            {
                var obj = HttpRuntime.Cache[cachename];
                if (obj != null)
                {
                    cnt++;
                    sb.AppendFormat("  {0}={1}\r\n", cachename.PadRight(31), Convert.ToString(obj));
                }
            }
            else
            {
                IDictionaryEnumerator cache = HttpRuntime.Cache.GetEnumerator();
                while (cache.MoveNext())
                {
                    sb.AppendFormat("  {0}={1}\r\n", Convert.ToString(cache.Key).PadRight(31), Convert.ToString(cache.Value));
                    cnt++;
                }
            }
        }
        catch (Exception exp)
        {
            sb.AppendFormat("  遍历缓存出错：{0}\r\n", exp.Message);
        }
        sb.Insert(0, "<a href='javascript:void(0)' onclick='showHide(this);'>=============================缓存列表(" + cnt.ToString() + "个)：========================</a>\r\n<span>");
        sb.Append("</span>\r\n\r\n");
        return sb.ToString();
    }

    static string ClearCache(string cachename)
    {
        int cnt = 0;
        try
        {
            if (!string.IsNullOrEmpty(cachename))
            {
                if (HttpRuntime.Cache.Remove(cachename) != null)
                    cnt++;
            }
            else
            {
                IDictionaryEnumerator cache = HttpRuntime.Cache.GetEnumerator();
                while (cache.MoveNext())
                {
                    if (HttpRuntime.Cache.Remove(Convert.ToString(cache.Key)) != null)
                        cnt++;
                }
            }
            return "  清空" + cnt.ToString() + "个";
        }
        catch (Exception exp)
        {
            return "  清空" + cnt.ToString() + "个, 遍历缓存出错： " + exp.Message;
        }
    }
</script>

<!-- Telnet配置相关的方法集 -->    
<script language="c#" runat="server">
    void Telnet()
    {
        StringBuilder sb = new StringBuilder();
        string ips = Request.Form["tip"] ?? "";
        IPAddress ip;
        int port;
        string[] tmp;
        foreach (string item in ips.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            tmp = item.Trim().Split(new char[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tmp.Length != 2)
            {
                sb.AppendFormat("{0} 不是ip:端口\r\n", item);
                continue;
            }
            if(!IPAddress.TryParse(tmp[0], out ip) || !int.TryParse(tmp[1], out port))
            {
                sb.AppendFormat("{0} ip格式错误或端口不是数字\r\n", item);
                continue;
            }
            DateTime start = DateTime.Now;
            try
            {
                IPEndPoint serverInfo = new IPEndPoint(ip, port);
                using (Socket socket = new Socket(serverInfo.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    //socket.BeginConnect(serverInfo, CallBackMethod, socket);
                    socket.Connect(serverInfo);
                    if (socket.Connected)
                    {
                        sb.AppendFormat("{0} 连接正常({1:N0}ms)\r\n", item, (DateTime.Now - start).TotalMilliseconds);
                    }
                    else
                    {
                        sb.AppendFormat("{0} 连接失败({1:N0}ms)\r\n", item, (DateTime.Now - start).TotalMilliseconds);
                    }
                    socket.Close();
                }
            }
            catch (Exception exp)
            {
                sb.AppendFormat("{0} 连接出错({2:N0}ms) {1}\r\n", item, exp.Message, (DateTime.Now - start).TotalMilliseconds);
            }
        }
        sb.AppendFormat("\r\n（客户ip：{0}；服务器：{1}）", m_remoteIpLst, m_localIp);
        Response.Write(sb.ToString());
    }
</script>

<!-- Sql测试方法集 -->    
<script language="C#" runat="server">
    void SqlRun()
    {
        string prefix = "（客户ip：" + m_remoteIpLst + "；服务器：" + m_localIp + "）<br />\r\n";
        string sql = Request.Form["sql"];
        string constr = Request.Form["constr"];
        if (!string.IsNullOrEmpty(sql) && !string.IsNullOrEmpty(constr))
        {
            //if (!sql.StartsWith("select ", StringComparison.OrdinalIgnoreCase))
            if (!Regex.IsMatch(sql, @"^(?i)select\s+top(?:\s|\()") || Regex.IsMatch(sql, @"^(?i)(update|delete|insert)"))
            {
                Response.Write(prefix + "只允许Select语句，且必须包含Top子句");
                return;
            }
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(constr))
            using (SqlCommand command = con.CreateCommand())
            using (SqlDataAdapter dataAdapter = new SqlDataAdapter(command))
            {
                con.Open();
                command.CommandText = sql;
                dataAdapter.Fill(dt);
                con.Close();
            }
            GridView gv1 = new GridView();
            gv1.DataSource = dt;
            gv1.DataBind();
            Response.Write(prefix + GetHtml(gv1));
        }
        else
        {
            Response.Write(prefix + "没有输入Sql或连接串");
        }
    }
    /// <summary>
    /// 返回 Web服务器控件的HTML 输出
    /// </summary>
    /// <param name="ctl"></param>
    /// <returns></returns>
    static string GetHtml(Control ctl)
    {
        if (ctl == null)
            return string.Empty;

        using (StringWriter sw = new StringWriter())
        using (HtmlTextWriter htw = new HtmlTextWriter(sw))
        {
            ctl.RenderControl(htw);
            return sw.ToString();
        }
    }
</script>

<!-- Redis管理方法集 -->    
<script language="c#" runat="server">
    void Redis()
    {
        string tip = Request.Form["tip"] ?? "";
        int pwdSplit = tip.LastIndexOf('@');
        string pwd = null;
        if (pwdSplit >= 0)
        {
            pwd = tip.Substring(0, pwdSplit);
            tip = tip.Substring(pwdSplit + 1);
        }
        string[] tmparr = tip.Split(':');
        string ipStr, portStr;
        if (tmparr.Length == 2)
        {
            ipStr = tmparr[0];
            portStr = tmparr[1];
        }
        else
        {
            ipStr = tmparr[0];
            portStr = "6379";
        }
        IPAddress ip;
        int port;
        if (!IPAddress.TryParse(ipStr, out ip) || !int.TryParse(portStr, out port))
        {
            Response.Write(tip + " ip格式错误或端口不是数字");
            return;
        }

        string searchCmd = (Request.Form["cm"] ?? "info").Trim();
        if (searchCmd.IndexOf("get", StringComparison.OrdinalIgnoreCase) != 0 
            && searchCmd.IndexOf("hget", StringComparison.OrdinalIgnoreCase) != 0
            && searchCmd.IndexOf("info", StringComparison.OrdinalIgnoreCase) != 0)
        {
            Response.Write(" 安全起见，暂时只允许get 和 hget、info命令: " + searchCmd);
            return;
        }

        List<byte> arrAll = new List<byte>(1024);
        IPEndPoint serverInfo = new IPEndPoint(ip, port);
        using (Socket socket = new Socket(serverInfo.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
        {
            //socket.BeginConnect(serverInfo, CallBackMethod, socket);
            socket.Connect(serverInfo);
            if (socket.Connected)
            {
                byte[] bytesReceived = new byte[1024];
                byte[] command;
                if(!string.IsNullOrEmpty(pwd))
                {
                    command = Encoding.UTF8.GetBytes("auth " + pwd + "\r\n");
                    socket.Send(command);
                    socket.Receive(bytesReceived, bytesReceived.Length, 0);
                }
                command = Encoding.UTF8.GetBytes(searchCmd + "\r\n");
                socket.Send(command);

                int zeroCnt = 0;
                while (true)
                {
                    int tmp = socket.Receive(bytesReceived, bytesReceived.Length, 0);
                    if (tmp <= 0)
                    {
                        zeroCnt++;// 总共5次0字节时，退出
                        if (zeroCnt > 5)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (tmp == bytesReceived.Length)
                        {
                            arrAll.AddRange(bytesReceived);
                        }
                        else
                        {
                            byte[] tarr = new byte[tmp];
                            Array.Copy(bytesReceived, tarr, tmp);
                            arrAll.AddRange(tarr);
                            break;
                        }
                    }
                }
            }
            socket.Close();
        }
        Response.Write(Encoding.UTF8.GetString(arrAll.ToArray()));

        //sb.AppendFormat("\r\n（客户ip：{0}；服务器：{1}）", m_remoteIpLst, m_localIp);
    }
</script>

<!-- 通用方法集 -->    
<script language="C#" runat="server">
    // POST获取网页内容
    static byte[] GetPage(string url, string param, string proxy)
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");
        request.Headers.Add("Accept-Charset", "utf-8");
        request.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0;)";
        request.AllowAutoRedirect = true; //出现301或302之类的转向时，是否要转向
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
        request.ContentType = "application/x-www-form-urlencoded";
        // 设置提交的数据
        if (!string.IsNullOrEmpty(param))
        {
            // 把数据转换为字节数组
            byte[] l_data = Encoding.UTF8.GetBytes(param);
            request.ContentLength = l_data.Length;
            // 必须先设置ContentLength，才能打开GetRequestStream
            // ContentLength设置后，reqStream.Close前必须写入相同字节的数据，否则Request会被取消
            using (Stream newStream = request.GetRequestStream())
            {
                newStream.Write(l_data, 0, l_data.Length);
                newStream.Close();
            }
        }
        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        using (Stream stream = response.GetResponseStream())
        {
            if (stream == null)
                return new byte[0];
            //using (var sr = new StreamReader(stream, Encoding.UTF8))
            //{
            //    return sr.ReadToEnd();
            //}
            List<byte> ret = new List<byte>(10000);
            byte[] arr = new byte[10000];
            int readcnt;
            while ((readcnt = stream.Read(arr, 0, arr.Length)) > 0)
            {
                for (int i = 0; i < readcnt; i++)
                    ret.Add(arr[i]);
                //ret.AddRange(arr.Take(readcnt));
            }
            return ret.ToArray();
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
        string isvia = request.ServerVariables["HTTP_VIA"];
        string forwardip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
        string proxy = request.Headers.Get("HTTP_NDUSER_FORWARDED_FOR_HAPROXY");
        return ip1 + ";" + ip2 + ";" + realip + ";" + isvia + ":" + forwardip + ";" + proxy;
    }
    // 获取本机IP列表
    static string GetServerIpList()
    {
        try
        {
            StringBuilder ips = new StringBuilder();
            IPHostEntry IpEntry = Dns.GetHostEntry(Dns.GetHostName());
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
            
    // 获取Session，如果禁用Session时，获取Cookie
    static string GetSession(string key)
    {
        SessionStateSection sessionStateSection = (SessionStateSection)ConfigurationManager.GetSection("system.web/sessionState");
        if (sessionStateSection.Mode == SessionStateMode.Off)
        {
            HttpCookie cook = HttpContext.Current.Request.Cookies[key];
            if (cook == null) return string.Empty;
            return cook.Value;
        }
        else
            return Convert.ToString(HttpContext.Current.Session[key]);
    }
    // 设置Session，如果禁用Session时，设置Cookie
    static void SetSession(string key, string value)
    {
        SessionStateSection sessionStateSection = (SessionStateSection)ConfigurationManager.GetSection("system.web/sessionState");
        if (sessionStateSection.Mode == SessionStateMode.Off)
            HttpContext.Current.Response.Cookies.Add(new HttpCookie(key, value));
        else
            HttpContext.Current.Session[key] = value;
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
</script>

    <script type="text/javascript" src="https://ascdn.bdstatic.com/fz_act/js/jq_125bece.js"></script>
    <script type="text/javascript" src="https://ascdn.bdstatic.com/fz_act/js/ui.tabs_d89ad1a.js"></script>
    <link rel="stylesheet" href="https://ascdn.bdstatic.com/fz_act/css/ui.tabs_2b0cf63.css" type="text/css" />

    <link rel="stylesheet" href="https://ascdn.bdstatic.com/fz_act/js/jquery-ui-min.css">
    <script type="text/javascript" src="https://ascdn.bdstatic.com/fz_act/js/jquery-ui-min.js"></script>

    <script type="text/javascript" src="https://ascdn.bdstatic.com/fz_act/js/rowColor_666490a.js"></script>
    <style type="text/css">
        .filetb { border-collapse: collapse;}
        .filetb td,th{ border: black 1px solid;padding: 2px 2px 2px 2px}
        #divret a{ TEXT-DECORATION: none; }
    </style>
    <script type="text/javascript">

        $(document).ready(function () {
            // 初始化标签
            var s = new UI_TAB();
            s.init("container-1");
            
            // 初始化弹出的对话框
            $('#dialog').dialog({
                autoOpen: false,
                modal: true
            });

            refreshConfig(null, 2);
        });

        function doSubmit(callback) {
            $("#divret").html("");
            var ips = getIps();
            for (var i = 0; i < ips.length; i++) {
                var ip = ips[i];
                $("#divret").append("<div id='div" + i + "' style='border:solid 1px blue;'>" + ip + "处理中……<br /></div>");
                callback(ip, i);
            }
        }

        function getIps() {
            var ips = $("#txtIp").val();
            var ret = [];
            if (ips.length > 0) {
                var iparr = ips.split(';');
                for (var i = 0, j = iparr.length; i < j; i++) {
                    var ip = $.trim(iparr[i]);
                    if (ip.length > 0)
                        ret.push(ip);
                }
            }
            if (ret.length <= 0) {
                $("#txtIp").val("127.0.0.1");
                ret.push("127.0.0.1");
            }
            return ret;
        }

        function ajaxSend(para, callback) {
            var url = '<%=m_currentUrl %>' + "?" + new Date();
            $.ajax({
                url: url,
                //dataType: "json",
                type: "POST",
                data: para,
                success: callback,
                error: ajaxError
            });
        }
        // ajax失败时的回调函数
        function ajaxError(httpRequest, textStatus, errorThrown) {
            // 通常 textStatus 和 errorThrown 之中, 只有一个会包含信息
            //this; // 调用本次AJAX请求时传递的options参数
            alert(textStatus + errorThrown);
        }

        function showHide(obj, forceShow) {
            obj = $(obj).next("span:eq(0)");
            if (forceShow == undefined)
                forceShow = !obj.is(":visible");
            if (forceShow) {
                obj.show();
            } else {
                obj.hide();
            }
        }
    </script>
</head>
<body style="font-size:12px;">
<div>
    要测试的服务器IP列表:<input type="text" id="txtIp" style="width:800px;" value="<%=m_ipLst %>" /><br/>
    (remote ip:<span style="color:blue;"><%=m_remoteIpLst %></span>　local ip:<span style="color:blue;"><%=m_localIp %></span>)
    <hr />
</div>
<div id="container-1">
    <ul class="ui-tabs-nav">
        <li class="ui-tabs-selected"><a href="#fragment1"><span>配置与内存相关</span></a></li>
        <li class=""><a href="#fragment2"><span>Telnet测试</span></a></li>
        <li class=""><a href="#fragment3"><span>Sql查询</span></a></li>
        <li class=""><a href="#fragment5"><span>Redis查询</span></a></li>
    </ul>

    <!-- 配置相关 -->
    <div style="display: block;" class="ui-tabs-panel ui-tabs-hide" id="fragment1">
        <table border="1" cellpadding="4" cellspacing="0">
            <tr>
                <td style="width:130px;text-align:right;">类全名：<br/>带命名空间</td>
                <td style="width:700px;text-align:left;">
                    <input type="text" id="txtClassName" style="width:80%;" value="" />
                    <input type="button" value="查看类数据" onclick="refreshConfig(this, 2);" />　
                    <br/>
                    如： Mike.Tools.Core.RedisLock
                </td>
            </tr>
            <tr>
            </tr>
            <tr><td style="text-align:right;">
                HttpRuntime.Cache：
            </td><td>
                <input type="text" id="txtCacheName" style="width:60%;" value="" />
                <input type="button" value="查看缓存明细" onclick="refreshConfig(this, 3);" />　
                <input type="button" value="清空内存缓存" onclick="refreshConfig(this, 4);" />　
            </td></tr>
            <tr><td style="text-align:center;" colspan="2">
            </td></tr>
        </table>
        <script type="text/javascript">
            function refreshConfig(btn, flg) {
                var className = $.trim($("#txtClassName").val());
                var cacheName = '';

                if (flg === 3) {
                    cacheName = $.trim($("#txtCacheName").val());
                    if (cacheName.length === 0 && !confirm("您确认要查看缓存明细吗？数据量大可能很慢，甚至卡住")) {
                        return;
                    }
                } else if (flg === 4) {
                    cacheName = $.trim($("#txtCacheName").val());
                    if (cacheName.length === 0 && !confirm("您确认要清空全部内存缓存吗？此操作后果比较严重哦")) {
                        return;
                    } else if (cacheName.length !== 0 && !confirm("您确认要删除内存缓存" + cacheName + "吗？")) {
                        return;
                    }
                }
                $("#divret").html("");

                $(btn).attr("disabled", "disabled");
                doSubmit(function (ip, idx) {
                    var para = {};
                    para.className = className;
                    para.ip = ip;
                    para.flg = 'showconfig';
                    para.f = flg;
                    para.cache = cacheName;
                    ajaxSend(para, function (msg) {
                        var obj = $("#div" + idx);
                        obj.html("<pre>" + ip + "处理完成，返回如下：(处理时间：" +
                            (new Date()).toString() + ")\r\n" + msg.replace(/<(?!\/?(span|a))/g, "&lt;") + "</pre>");
                    });
                });
                $(btn).removeAttr("disabled");
            }
        </script>
    </div>

    <!-- Telnet测试 -->
    <div class="ui-tabs-panel ui-tabs-hide" id="fragment2">
        <hr style="height: 5px; background-color: green" />
        目标服务器IP和端口列表（ip:端口 换行 ip:端口）：<br />
        <textarea id="txtTelnetIp" rows="5" cols="40">10.2.3.209:3389
10.2.3.209:1433</textarea><br/>
        <input type="button" value="测试" onclick="telnetTest(this);" style="width:200px;"/>
        <script type="text/javascript">
            function telnetTest(btn) {
                var ipTo = $.trim($("#txtTelnetIp").val());
                if (ipTo.length <= 0) {
                    alert("请输入测试IP和端口列表");
                    return;
                }
                $("#divret").html("");

                $(btn).attr("disabled", "disabled");
                doSubmit(function (ip, idx) {
                    var para = "flg=telnet&ip=" + ip + "&tip=" + ipTo;
                    ajaxSend(para, function (msg) {
                        $("#div" + idx).html("<pre>" + ip + "返回如下：(处理时间：" +
                            (new Date()).toString() + ")\r\n" + msg.replace(/<(?!\/?(span|a))/g, "&lt;") + "</pre>");
                    });
                });
                $(btn).removeAttr("disabled");
            }
        </script>
    </div>

    <!-- Sql查询 -->
    <div class="ui-tabs-panel ui-tabs-hide" id="fragment3">
        <hr style="height: 5px; background-color: green" />
        <div>说明：Sql查询只能测试单机，不能多服务器查询，多服务器请用Telnet测试</div>
        <div style="color: red;">注意：只允许使用select查询语句，不允许update等修改性语句，且select语句不是很耗性能的语句</div>
        <br />
        数据库连接串：<input type="text" id="txtSqlCon" value="server=10.2.3.209;database=db;uid=xx;pwd=xx" style="width:900px"/><br/>
        SQL：<textarea id="txtSql" rows="2" cols="20" style="height:200px;width:1000px;">select top 2 * 
  from softs with(nolock)
 where 1=1</textarea><br/>
        <input type="button" value="测试" onclick="sqlTest(this);" style="width:200px;"/>
        <script type="text/javascript">
            function sqlTest(btn) {
                var constr = $.trim($("#txtSqlCon").val());
                if (constr.length <= 0) {
                    alert("请输入数据库连接串");
                    return;
                }
                var sql = $.trim($("#txtSql").val());
                if (sql.length <= 0) {
                    alert("请输入SQL");
                    return;
                }
                $("#divret").html("查询中，请稍候……");

                $(btn).attr("disabled", "disabled");
                var para = "flg=sql&sql=" + sql + "&constr=" + constr;
                ajaxSend(para, function (msg) {
                    $("#divret").html(msg);
                });
                $(btn).removeAttr("disabled");
            }
        </script>
    </div>

    <!-- Redis管理 -->
    <div class="ui-tabs-panel ui-tabs-hide" id="fragment5">
        <hr style="height: 5px; background-color: green" />
        <table>
            <tr>
                <td>Redis IP和端口（格式：密码@ip:端口）：</td>
                <td>
                    <select onchange="$('#txtRedisServer').val($(this).val());">
                        <optgroup label="QA-Redis">
                            <option>10.2.0.174:6379</option>
                        </optgroup>
                        <optgroup label="Redis组1">
                            <option>10.2.3.209:6379</option>
                        </optgroup>
                    </select>
                    <input type="text" id="txtRedisServer" style="width: 200px" value="10.2.0.174:6379"/>
                </td>
            </tr>
            <tr>
                <td>命令：</td><td><input type="text" id="txtRedisCommand" style="width: 500px" value="info"/></td>
            </tr>
            <tr>
                <td><input type="button" value="提交" onclick="sendRedis(this);"/></td>
            </tr>
        </table>
        <script type="text/javascript">
            function sendRedis(btn) {
                var ipTo = $.trim($("#txtRedisServer").val());
                if (ipTo.length <= 0) {
                    alert("请输入RedisIP和端口");
                    return;
                }
                var sql = $.trim($("#txtRedisCommand").val());
                if (sql.length <= 0) {
                    alert("请输入命令");
                    return;
                }
                $("#divret").html("查询中，请稍候……");

                $(btn).attr("disabled", "disabled");
                var para = "flg=redis&cm=" + sql + "&tip=" + ipTo;
                ajaxSend(para, function (msg) {
                    $("#divret").html("<pre>" + msg + "</pre>");
                });
                $(btn).removeAttr("disabled");
            }
        </script>
    </div>
    <hr style="height: 5px; background-color: green" />
    <div id="divret"></div>
</div>    
</body>
</html>
