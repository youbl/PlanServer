using System;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using PlanServerService;

namespace PlanServerExtend
{
    /// <summary>
    /// 给计划任务主程序的Socket接口调用的类
    /// </summary>
    public static class ExtendClass
    {
        public static string RunSql(string sql)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return "sql不能为空";
            }
            if (sql.Length < 8)
            {
                return "sql长度没超过8";
            }
            string db = @"e:\upload\planserver\planserver.db";
            if (sql[1] == ':')
            {
                //第二个字符是冒号，表示前面是数据库路径
                int idx = sql.IndexOf(',');
                if (idx <= 0)
                {
                    return "无效的参数";
                }
                db = sql.Substring(0, idx);
                sql = sql.Substring(idx + 1);
            }
            try
            {
                if(!File.Exists(db))
                {
                    return "数据库不存在";
                }
                //string sql = @"SELECT * FROM [tasks] ORDER BY [id]";// WHERE [runtype] > 0
                DataSet ds = SQLiteHelper.ExecuteDataset(db, sql);
                string ret = Common.XmlSerializeToStr(ds);
                return ret;
            }
            catch (Exception exp)
            {
                return exp.ToString();
            }
        }

        #region 测试用的方法
        
        public static string GetServerIpList()
        {
            try
            {
                StringBuilder ips = new StringBuilder();
                IPHostEntry IpEntry = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ipa in IpEntry.AddressList)
                {
                    if (ipa.AddressFamily == AddressFamily.InterNetwork)
                        ips.AppendFormat("{0};", ipa.ToString());
                }
                if (ips.Length > 0)
                    ips.Remove(ips.Length - 1, 1);
                return ips.ToString();
            }
            catch (Exception exp)
            {
                //LogHelper.WriteCustom("获取本地ip错误" + ex, @"zIP\", false);
                return exp.Message;
            }
        }

        public static string test(string args)
        {
            return args == null ? "空值" : "你提交的是：" + args;
        }
        #endregion

    }
}
