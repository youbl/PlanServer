using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Xml;
using ICSharpCode.SharpZipLib.Zip;

namespace PlanServerService
{
    public static class Common
    {
        static Common()
        {
            string tmp = ConfigurationManager.AppSettings["EnableFileAdmin"];
            if (string.IsNullOrEmpty(tmp) || (!tmp.Equals("true", StringComparison.OrdinalIgnoreCase) && tmp != "1"))
                EnableFileAdmin = false;
            else
                EnableFileAdmin = true;
        }


        // 获取本机机器名，用于后面查找进程
        public static readonly string ServerName = Dns.GetHostName();

        /// <summary>
        /// 是否允许文件管理
        /// </summary>
        public static bool EnableFileAdmin;

        /// <summary>
        /// 把对象序列化为Xml字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static String XmlSerializeToStr<T>(T obj)
        {
            var formatter = new DataContractSerializer(typeof(T));
            using (var memory = new MemoryStream())
            {
                formatter.WriteObject(memory, obj);
                memory.Seek(0, SeekOrigin.Begin);
                using (var sr = new StreamReader(memory, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// 把Xml字符串反序列化成对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static T XmlDeserializeFromStr<T>(string xml) where T : class
        {
            var xs = new DataContractSerializer(typeof(T));
            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            using (var xmlreader = new XmlTextReader(memoryStream))
            {
                // [\x0-\x8\x11\x12\x14-\x32]
                // 默认为true，如果序列化的对象含有比如0x1e之类的非打印字符，反序列化就会出错，因此设置为false http://msdn.microsoft.com/en-us/library/aa302290.aspx
                xmlreader.Normalization = false;
                xmlreader.WhitespaceHandling = WhitespaceHandling.Significant;
                xmlreader.XmlResolver = null;
                return xs.ReadObject(xmlreader) as T;
            }
        }

        /// <summary>
        /// 获取appSettings节点值，并转换为bool值
        /// </summary>
        /// <param name="key">节点名称</param>
        /// <returns></returns>
        public static bool GetBoolean(string key)
        {
            string tmp = GetSetting(key);
            return tmp.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取appSettings节点值，并转换为int值
        /// </summary>
        /// <param name="key">节点名称</param>
        /// <param name="defaultValue">节点不存在或不是数值时的默认值</param>
        /// <returns></returns>
        public static Int32 GetInt32(string key, Int32 defaultValue = 0)
        {
            string tmp = GetSetting(key);
            if (string.IsNullOrEmpty(tmp))
                return defaultValue;
            Int32 ret;
            if (Int32.TryParse(tmp, out ret))
                return ret;
            else
                return defaultValue;
        }

        /// <summary>
        /// 获取appSettings节点值
        /// </summary>
        /// <param name="key">节点名称</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>节点值</returns>
        public static string GetSetting(string key, string defaultValue = null)
        {
            defaultValue = defaultValue ?? string.Empty;
            try
            {
                //if (ConfigurationManager.AppSettings == null)
                //    return defaultValue;
                return ConfigurationManager.AppSettings[key] ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 是否为文件物理路径
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsPhysicalPath(string source)
        {
            return Regex.IsMatch(source, @"^[a-zA-Z]:[\\/]+(?:[^\<\>\/\\\|\:""\*\?\r\n]+[\\/]+)*[^\<\>\/\\\|\:""\*\?\r\n]*$");
        }

        /// <summary>
        /// 根据提供的参数值，进行校验码的计算
        /// </summary>
        /// <param name="paras"></param>
        /// <returns></returns>
        public static string GetCheckCode(params object[] paras)
        {
            if (paras == null || paras.Length == 0)
                throw new ArgumentException("参数不能为空");

            string spliter = "_&_";
            StringBuilder sb = new StringBuilder(200);
            foreach (object para in paras)
            {
                sb.AppendFormat("{0}{1}", para, spliter);
            }
            return MD5_Encrypt(sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// 标准MD5加密
        /// </summary>
        /// <param name="source">待加密字符串</param>
        /// <param name="encoding">编码方式</param>
        /// <returns></returns>
        public static string MD5_Encrypt(string source, Encoding encoding)
        {
            byte[] datSource = encoding.GetBytes(source);
            byte[] newSource;
            using (MD5 cryptProvider = new MD5CryptoServiceProvider())
            {
                newSource = cryptProvider.ComputeHash(datSource);
            }
            StringBuilder byte2String = new StringBuilder(newSource.Length * 2);
            foreach (byte by in newSource)
            {
                byte2String.Append(by.ToString("x2"));
            }
            return byte2String.ToString();
        }

        /// <summary>
        /// 计算指定文件的MD5值
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetFileMD5(string path)
        {
            try
            {
                using (MD5CryptoServiceProvider get_md5 = new MD5CryptoServiceProvider())
                using (FileStream get_file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return BitConverter.ToString(get_md5.ComputeHash(get_file)).Replace("-", "");
                }
            }
            catch (Exception exp)
            {
                return exp.Message;
            }
        }


        /// <summary>
        /// 获取本机IP列表
        /// </summary>
        /// <returns></returns>
        public static string GetServerIpList()
        {
            try
            {
                StringBuilder ips = new StringBuilder();
                IPHostEntry IpEntry = Dns.GetHostEntry(ServerName);
                foreach (IPAddress ipa in IpEntry.AddressList)
                {
                    if (ipa.AddressFamily == AddressFamily.InterNetwork)
                        ips.AppendFormat("{0};", ipa.ToString());
                }
                return ips.ToString();
            }
            catch (Exception)
            {
                //LogHelper.WriteCustom("获取本地ip错误" + ex, @"zIP\", false);
                return string.Empty;
            }
        }


        #region 压缩解压方法
        /// <summary>
        /// 把指定压缩包解压到指定文件夹，并返回解压文件数，文件夹为空时，解压到压缩包所在目录
        /// </summary>
        /// <param name="zipfilename"></param>
        /// <param name="unzipDir"></param>
        public static int UnZipFile(string zipfilename, string unzipDir = null)
        {
            int filecount = 0;
            if (string.IsNullOrEmpty(unzipDir))
            {
                unzipDir = Path.GetDirectoryName(zipfilename);
                if (unzipDir == null)
                    throw new ArgumentException("目录信息不存在", "zipfilename");
            }
            else if (!Directory.Exists(unzipDir))
            {
                //生成解压目录
                Directory.CreateDirectory(unzipDir);
            }
            using (ZipInputStream s = new ZipInputStream(File.OpenRead(zipfilename)))
            {
                ZipEntry theEntry;
                while ((theEntry = s.GetNextEntry()) != null)
                {
                    string path = Path.Combine(unzipDir, theEntry.Name);
                    if (theEntry.IsDirectory)
                    {
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);
                    }
                    else if (theEntry.IsFile)
                    {
                        filecount++;
                        string dir = Path.GetDirectoryName(path);
                        if (string.IsNullOrEmpty(dir))
                            throw new Exception("压缩文件有问题，有个文件没有目录" + path);
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                        //解压文件到指定的目录)
                        using (FileStream fs = File.Create(path))
                        {
                            byte[] data = new byte[2048];
                            while (true)
                            {
                                int size = s.Read(data, 0, data.Length);
                                if (size <= 0)
                                    break;
                                fs.Write(data, 0, size);
                            }
                            fs.Close();
                        }
                    }
                }
                s.Close();
            }
            return filecount;
        }

        /// <summary>
        /// 压缩指定的目录列表
        /// </summary>
        /// <param name="zipFilePath">要压缩到的文件路径名</param>
        /// <param name="parentDir">被压缩的文件或目录所在的父目录</param>
        /// <param name="dirOrFiles">要被压缩的目录或文件列表</param>
        public static void ZipDirs(string zipFilePath, string parentDir, params string[] dirOrFiles)
        {
            if (dirOrFiles == null || dirOrFiles.Length == 0)
                throw new ArgumentException("文件或目录列表不能为空", "dirOrFiles");

            if (parentDir[parentDir.Length - 1] != '\\')
                parentDir += "\\";
            // 避免出现c:\\//abc这样的多斜杠的路径，造成压缩路径显示错误
            parentDir = Path.GetFullPath(parentDir);// c://a\\\b.txt 变成c:\a\b.txt

            if (string.IsNullOrEmpty(zipFilePath))
                zipFilePath = Path.GetFileName(dirOrFiles[0]) + ".zip";

            using (ZipOutputStream zos = new ZipOutputStream(File.Create(zipFilePath)))
            {
                foreach (string item in dirOrFiles)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;

                    string fullpath = Path.Combine(parentDir, item);
                    if (File.Exists(fullpath))
                        AddFileEntry(zos, fullpath, parentDir);
                    else if (Directory.Exists(fullpath))
                        AddDirEntry(zos, fullpath, parentDir);
                }
            }
        }


        /// <summary>
        /// 把目录加入压缩包,返回压缩的目录和文件数
        /// </summary>
        /// <param name="zos"></param>
        /// <param name="dir"></param>
        /// <param name="rootDir">用于把文件名中不需要压缩的路径替换掉，避免压缩包里出现C:这样的目录结构</param>
        private static int AddDirEntry(ZipOutputStream zos, string dir, string rootDir)
        {
            string[] dirs = Directory.GetDirectories(dir);
            string[] files = Directory.GetFiles(dir);
            int ret = files.Length + dirs.Length;
            foreach (string subdir in dirs)
            {
                int tmp = AddDirEntry(zos, subdir, rootDir);
                if (tmp == 0)
                {
                    string strEntryName = subdir.Replace(rootDir, "");
                    ZipEntry entry = new ZipEntry(strEntryName + "\\_");
                    zos.PutNextEntry(entry);
                }
                ret += tmp;
            }
            foreach (string file in files)
            {
                AddFileEntry(zos, file, rootDir);
            }
            return ret;
        }

        /// <summary>
        /// 把文件加入压缩包
        /// </summary>
        /// <param name="zos"></param>
        /// <param name="file"></param>
        /// <param name="rootDir">用于把文件名中不需要压缩的路径替换掉，避免压缩包里出现C:这样的目录结构</param>
        private static void AddFileEntry(ZipOutputStream zos, string file, string rootDir)
        {
            //rootDir = Regex.Replace(rootDir, @"[/\\]+", @"\");// 把多个斜杠替换为一个
            //file = Regex.Replace(file, @"[/\\]+", @"\");// 把多个斜杠替换为一个
            if (!rootDir.EndsWith(@"\"))
                rootDir += @"\";
            using (FileStream fs = File.OpenRead(file))
            {
                string strEntryName = file.Replace(rootDir, "");
                ZipEntry entry = new ZipEntry(strEntryName);
                zos.PutNextEntry(entry);
                int size = 1024;
                byte[] array = new byte[size];
                while (fs.Position < fs.Length)
                {
                    int length = fs.Read(array, 0, size);
                    zos.Write(array, 0, length);
                }
                fs.Close();
            }
        }
        #endregion

        public static string GetHtml(Control ctl)
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
    }
}

