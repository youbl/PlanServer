using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Web.UI.WebControls;

namespace PlanServerService
{
    public static class AdminDal
    {
        private const string _adminDbPath = @"e:\upload\planserver\admin.db";

        #region 服务器权限管理相关方法
        public static List<string> GetServers(string loginIp)
        {
            List<string> ips = new List<string>();
            if (loginIp == null || (loginIp = loginIp.Trim()) == string.Empty)
                return ips;
            string sql = @"select b.ip from IpRight a, Servers b where a.serverid = b.id and a.ip=@ip order by b.ip";
            var para = new SQLiteParameter("@ip", DbType.String, 15);
            para.Value = loginIp;
            using (var reader = SQLiteHelper.ExecuteReader(DbPath, sql, para))
            {
                while (reader.Read())
                {
                    ips.Add(Convert.ToString(reader["ip"]));
                }
            }
            return ips;
        }
        public static List<string> GetAllServers()
        {
            List<string> ips = new List<string>();
            string sql = @"select ip from Servers order by ip";
            using (var reader = SQLiteHelper.ExecuteReader(DbPath, sql))
            {
                while (reader.Read())
                {
                    ips.Add(Convert.ToString(reader["ip"]));
                }
            }
            return ips;
        }

        #endregion

        #region 根据登录IP获取有权限的服务器IP列表
        static string DbPath
        {
            get
            {
                CheckAndCreateDB(_adminDbPath);
                return _adminDbPath;
            }
        }



        /// <summary>
        /// 数据库不存在时，创建数据库和相关表结构
        /// </summary>
        /// <param name="dbFilename"></param>
        static void CheckAndCreateDB(string dbFilename)
        {
            if (!File.Exists(dbFilename))
            {
                string dir = Path.GetDirectoryName(dbFilename);
                if (string.IsNullOrEmpty(dir))
                    throw new Exception("数据库路径配置有误：" + dbFilename);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                SQLiteConnection.CreateFile(dbFilename);
                string sql;

                //                // 创建账号清单表
                //                sql = @"create table Users(
                //id INTEGER autoincrement, 
                //account varchar(20) not null COLLATE NOCASE, 
                //pwd varchar(20) not null, 
                //right INTEGER not null default(0),
                //instime TIMESTAMP not null default (datetime('now', 'localtime')),
                //insip varchar(15) not null,
                //PRIMARY KEY(id)
                //)";
                //                ExecuteNonQuery(dbFilename, sql, null);
                //                // 创建account的唯一索引
                //                sql = "create unique index unq_account on Users(account)";
                //                ExecuteNonQuery(dbFilename, sql, null);

                // 创建服务器列表
                sql = @"create table Servers(
id INTEGER PRIMARY KEY autoincrement, 
ip varchar(15) not null,
desc varchar(100) null COLLATE NOCASE,
instime TIMESTAMP not null default (datetime('now', 'localtime')),
insip varchar(100) not null
)";
                SQLiteHelper.ExecuteNonQuery(dbFilename, sql);
                // 创建ip的唯一索引
                sql = "create unique index unq_ip on Servers(ip)";
                SQLiteHelper.ExecuteNonQuery(dbFilename, sql);

                // 创建ip权限表,server为指定服务器ip
                sql = @"create table IpRight(
id INTEGER PRIMARY KEY autoincrement, 
ip varchar(15) not null,
serverid INTEGER not null, 
desc varchar(100) null COLLATE NOCASE,
instime TIMESTAMP not null default (datetime('now', 'localtime')),
insip varchar(100) not null
)";
                SQLiteHelper.ExecuteNonQuery(dbFilename, sql);

                // 创建ip和server的唯一索引
                sql = "create unique index unq_ip_server on IpRight(ip,serverid)";
                SQLiteHelper.ExecuteNonQuery(dbFilename, sql);

                //alter table tasks add immediate int default 0 not null
            }
        }

        #endregion


        #region 维护权限列表相关方法
        public static string GetAllServerTable()
        {
            string sql = @"select a.*,'' del from Servers a order by a.desc,a.ip";
            using (var reader = SQLiteHelper.ExecuteReader(DbPath, sql))
            {
                if (!reader.HasRows)
                    return "未找到数据";
                GridView gv1 = new GridView();
                gv1.DataSource = reader;
                gv1.DataBind();
                return Common.GetHtml(gv1);
            }
        }

        public static string GetAllRightTable()
        {
            string sql = @"select a.id,a.ip,b.ip server,a.desc,a.instime,a.insip,'' del
from IpRight a, Servers b where a.serverid = b.id order by a.desc,a.ip, b.ip";
            using (var reader = SQLiteHelper.ExecuteReader(DbPath, sql))
            {
                if (!reader.HasRows)
                    return "未找到数据";
                GridView gv1 = new GridView();
                gv1.DataSource = reader;
                gv1.DataBind();
                return Common.GetHtml(gv1);
            }
        }

        public static string AddAdminIp(string[] clients, string[] server, string desc, string remoteIp)
        {
            StringBuilder ret = new StringBuilder("<span style='color:red;'>");
            foreach (string citem in clients)
            {
                string client = citem.Trim();
                if (client == string.Empty)
                    continue;
                var paras = new SQLiteParameter[]
                {
                    new SQLiteParameter("@sip", DbType.String, 15),
                    new SQLiteParameter("@ip", DbType.String, 15) {Value = client},
                    new SQLiteParameter("@desc", DbType.String, 100) {Value = desc},
                    new SQLiteParameter("@insip", DbType.String, 100) {Value = remoteIp},
                };
                int num = 0;

                foreach (string sip in server)
                {
                    string sql =
                        "select count(1) from IpRight a, Servers b where a.serverid = b.id and a.ip=@ip and b.ip=@sip";
                    string tmp = sip.Trim();
                    if (tmp == "")
                        continue;
                    paras[0].Value = tmp;
                    if (Convert.ToInt32(SQLiteHelper.ExecuteScalar(DbPath, sql, paras)) <= 0)
                    {
                        sql = @"insert into IpRight(ip, serverid, desc, insip)
select @ip, id, @desc, @insip from Servers where ip=@sip";
                        num += SQLiteHelper.ExecuteNonQuery(DbPath, sql, paras);
                    }
                }
                if (num <= 0)
                    ret.AppendFormat("{0}权限都存在; ", citem);
                else
                    ret.AppendFormat("{1}添加{0}条权限; ", num.ToString(), citem);
            }
            ret.Append("</span>");
            ret.Append(GetAllRightTable());
            return ret.ToString();
        }

        public static int DelAdminIp(string id)
        {
            string sql = "delete from IpRight where id=@id";
            var para = new SQLiteParameter("@id", DbType.Int32) { Value = id };
            return SQLiteHelper.ExecuteNonQuery(DbPath, sql, para);
        }

        public static string AddAdminServer(string[] servers, string desc, string remoteIp)
        {
            StringBuilder ret = new StringBuilder("<span style='color:red;'>");
            foreach (string item in servers)
            {
                string server = item.Trim();
                if (server == string.Empty)
                    continue;
                var paras = new SQLiteParameter[]
                {
                    new SQLiteParameter("@desc", DbType.String, 100) {Value = desc},
                    new SQLiteParameter("@insip", DbType.String, 100) {Value = remoteIp},
                    new SQLiteParameter("@sip", DbType.String, 15) {Value = server},
                };
                string sql = "select count(1) from Servers where ip=@sip";
                int num = 0;
                if (Convert.ToInt32(SQLiteHelper.ExecuteScalar(DbPath, sql, paras)) <= 0)
                {
                    sql = @"insert into Servers(ip, desc,insip)values(@sip, @desc, @insip)";
                    num = SQLiteHelper.ExecuteNonQuery(DbPath, sql, paras);
                }
                if (num <= 0)
                    ret.AppendFormat("{0}已存在; ", server);
                else
                    ret.AppendFormat("{1}插入{0}条; ", num.ToString(), server);
            }
            ret.Append("</span>");
            ret.Append(GetAllServerTable());
            return ret.ToString();
        }

        public static string DelAdminServer(string ips)
        {
            StringBuilder ret = new StringBuilder("<span style='color:red;'>");

            string sql = "delete from IpRight where serverid in (select id from Servers where ip in (" + ips + "))";
            var n = SQLiteHelper.ExecuteNonQuery(DbPath, sql);
            ret.AppendFormat("删除{0}条权限记录,", n.ToString());

            sql = "delete from Servers where ip in (" + ips + ")";
            n = SQLiteHelper.ExecuteNonQuery(DbPath, sql);
            if (n > 0)
            {
                ret.AppendFormat("删除{0}条服务器记录:{1}", n.ToString(), ips);
            }
            else
            {
                ret.AppendFormat("未找到服务器记录:{0}", ips);
            }
            ret.Append("</span>");
            ret.Append(GetAllServerTable());
            return ret.ToString();
        }

        public static string GetAdminServers()
        {
            string sql = @"select a.ip,a.desc from Servers a order by a.desc,a.ip";
            StringBuilder str = new StringBuilder("[");
            using (var reader = SQLiteHelper.ExecuteReader(DbPath, sql))
            {
                while (reader.Read())
                {
                    str.AppendFormat("['{0}','{1}'],", reader["ip"], reader["desc"]);
                }
            }
            str.Remove(str.Length - 1, 1);
            str.Append("]");
            return str.ToString();
        }
        #endregion


        public static string RunSql(string sql)
        {
            using (var reader = SQLiteHelper.ExecuteReader(DbPath, sql))
            {
                if (!reader.HasRows)
                    return "无数据返回";
                GridView gv1 = new GridView();
                gv1.DataSource = reader;
                gv1.DataBind();
                return Common.GetHtml(gv1);
            }
        }
    }
}
