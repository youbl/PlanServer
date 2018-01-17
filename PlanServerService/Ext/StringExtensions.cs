using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Security.Cryptography;

namespace PlanServerService.Ext
{
    /// <summary>
    /// 字符串扩展方法
    /// </summary>
    public static class StringExtensions
    {
        #region 字符串操作
        /// <summary>
        /// 获取字符串的实际长度(按单字节)
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static int GetRealLength(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return 0;
            return Encoding.Default.GetByteCount(source);
        }

        /// <summary>
        /// 取得固定长度的字符串(按单字节截取)。
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="resultLength">截取长度</param>
        /// <param name="prefix">进行了截取操作时，要增加的后缀</param>
        /// <returns></returns>
        public static string SubString(this string source, int resultLength, string prefix)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;

            //判断字符串长度是否大于截断长度
            if (Encoding.Default.GetByteCount(source) > resultLength)
            {
                //初始化
                int i = 0, j = 0;

                //为汉字或全脚符号长度加2否则加1
                foreach (char newChar in source)
                {
                    if (newChar > 127)
                    {
                        i += 2;
                    }
                    else
                    {
                        i++;
                    }
                    if (i > resultLength)
                    {
                        source = source.Substring(0, j) + prefix;
                        break;
                    }
                    j++;
                }
            }
            return source;
        }

        /// <summary>
        /// 取得固定长度的字符串(按单字节截取)。
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="resultLength">截取长度</param>
        /// <returns></returns>
        public static string SubString(this string source, int resultLength)
        {
            return SubString(source, resultLength, string.Empty);
        }

        /// <summary>
        /// 取得固定长度字符的字符串，后面加上…(按单字节截取)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="resultLength"></param>
        /// <returns></returns>
        public static string SubStr(this string source, int resultLength)
        {
            return SubString(source, resultLength, "...");
        }
        #endregion

        #region 字符串格式验证
        /// <summary>
        /// 判断字符串是否为null或为空.判断为空操作前先进行了Trim操作。
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this string source)
        {
            if (source != null)
            {
                return source.Trim().Length < 1;
            }
            return true;
        }
        /// <summary>
        /// 判断字符串是否为整型
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsInteger(this string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return false;
            }
            return Int32.TryParse(source, out _);
        }

        /// <summary>
        /// Email 格式是否合法
        /// </summary>
        /// <param name="source"></param>
        public static bool IsEmail(this string source)
        {
            return Regex.IsMatch(source, @"^\w+((-\w+)|(\.\w+))*\@[A-Za-z0-9]+((\.|-)[A-Za-z0-9]+)*\.[A-Za-z0-9]+$");
        }

        /// <summary>
        /// 判断是否IP
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsIP(this string source)
        {
            return Regex.IsMatch(source, @"^(((25[0-5]|2[0-4][0-9]|19[0-1]|19[3-9]|18[0-9]|17[0-1]|17[3-9]|1[0-6][0-9]|1[1-9]|[2-9][0-9]|[0-9])\.(25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9][0-9]|[0-9]))|(192\.(25[0-5]|2[0-4][0-9]|16[0-7]|169|1[0-5][0-9]|1[7-9][0-9]|[1-9][0-9]|[0-9]))|(172\.(25[0-5]|2[0-4][0-9]|1[0-9][0-9]|1[0-5]|3[2-9]|[4-9][0-9]|[0-9])))\.(25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9][0-9]|[0-9])\.(25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9][0-9]|[0-9])$");
        }

        /// <summary>
        /// 检查字符串是否为A-Z、0-9及下划线以内的字符
        /// </summary>
        /// <param name="source">被检查的字符串</param>
        /// <returns>是否有特殊字符</returns>
        public static bool IsLetterOrNumber(this string source)
        {
            bool b = System.Text.RegularExpressions.Regex.IsMatch(source, @"\w");
            return b;
        }

        /// <summary>
        /// 验输入字符串是否含有“/\:.?*|$]”特殊字符
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsSpecialChar(this string source)
        {
            Regex r = new Regex(@"[/\<>:.?*|$]");
            return r.IsMatch(source);
        }

        /// <summary>
        /// 是否全为中文/日文/韩文字符
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns></returns>
        public static bool IsChineseChar(this string source)
        {
            //中文/日文/韩文: [\u4E00-\u9FA5]
            //英文:[a-zA-Z]
            return Regex.IsMatch(source, @"^[\u4E00-\u9FA5]+$");
        }

        /// <summary>
        /// 是否包含中文/日文/韩文字符
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns></returns>
        public static bool ContainChineseChar(this string source)
        {
            //中文/日文/韩文: [\u4E00-\u9FA5]
            //英文:[a-zA-Z]
            return Regex.IsMatch(source, @"[\u4E00-\u9FA5]");
        }

        /// <summary>
        /// 是否包含双字节字符(允许有单字节字符)
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsDoubleChar(this string source)
        {
            return Regex.IsMatch(source, @"[^\x00-\xff]");
        }

        /// <summary>
        /// 是否为日期型字符串
        /// </summary>
        /// <param name="source">日期字符串(2005-6-30)</param>
        /// <returns></returns>
        public static bool IsDate(this string source)
        {
            return Regex.IsMatch(source, @"^((((1[6-9]|[2-9]\d)\d{2})-(0?[13578]|1[02])-(0?[1-9]|[12]\d|3[01]))|(((1[6-9]|[2-9]\d)\d{2})-(0?[13456789]|1[012])-(0?[1-9]|[12]\d|30))|(((1[6-9]|[2-9]\d)\d{2})-0?2-(0?[1-9]|1\d|2[0-8]))|(((1[6-9]|[2-9]\d)(0[48]|[2468][048]|[13579][26])|((16|[2468][048]|[3579][26])00))-0?2-29-))$");
        }


        /// <summary>
        /// 是否为时间型字符串
        /// </summary>
        /// <param name="source">时间字符串(15:00:00)</param>
        /// <returns></returns>
        public static bool IsTime(this string source)
        {
            return Regex.IsMatch(source, @"^((20|21|22|23|[0-1]?\d):[0-5]?\d:[0-5]?\d)$");
        }

        /// <summary>
        /// 是否为日期+时间型字符串
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsDateTime(this string source)
        {
            return Regex.IsMatch(source, @"^(((((1[6-9]|[2-9]\d)\d{2})-(0?[13578]|1[02])-(0?[1-9]|[12]\d|3[01]))|(((1[6-9]|[2-9]\d)\d{2})-(0?[13456789]|1[012])-(0?[1-9]|[12]\d|30))|(((1[6-9]|[2-9]\d)\d{2})-0?2-(0?[1-9]|1\d|2[0-8]))|(((1[6-9]|[2-9]\d)(0[48]|[2468][048]|[13579][26])|((16|[2468][048]|[3579][26])00))-0?2-29-)) (20|21|22|23|[0-1]?\d):[0-5]?\d:[0-5]?\d)$");
        }

        /// <summary>
        /// 是否为文件物理路径
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsPhysicalPath(this string source)
        {
            return Regex.IsMatch(source, @"^[a-zA-Z]:[\\/]+(?:[^\<\>\/\\\|\:""\*\?\r\n]+[\\/]+)*[^\<\>\/\\\|\:""\*\?\r\n]*$");
        }

        #endregion

        #region 字符串编码
        /// <summary>
        /// 将字符串使用base64算法加密
        /// </summary>
        /// <param name="source">待加密的字符串</param>
        /// <returns>加码后的文本字符串</returns>
        public static string ToBase64(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            return Convert.ToBase64String(Encoding.Default.GetBytes(source));
        }
        /// <summary>
        /// 从Base64编码的字符串中还原字符串，支持中文
        /// </summary>
        /// <param name="source">Base64加密后的字符串</param>
        /// <returns>还原后的文本字符串</returns>
        public static string FromBase64(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            return Encoding.Default.GetString(Convert.FromBase64String(source));
        }

        /// <summary>
        /// 将字符串使用base64算法加密
        /// </summary>
        /// <param name="source">待加密的字符串</param>
        /// <param name="encode">编码方式</param>
        /// <returns>加码后的文本字符串</returns>
        public static string ToBase64(this string source, Encoding encode)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            return Convert.ToBase64String(encode.GetBytes(source));
        }
        /// <summary>
        /// 从Base64编码的字符串中还原字符串，支持中文
        /// </summary>
        /// <param name="source">Base64加密后的字符串</param>
        /// <param name="encode">编码方式</param>
        /// <returns>还原后的文本字符串</returns>
        public static string FromBase64(this string source, Encoding encode)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            return encode.GetString(Convert.FromBase64String(source));
        }

        /// <summary>
        /// 将 GB2312 值转换为 UTF8 字符串(如：测试 -> 娴嬭瘯 )
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string FromGBToUTF8(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            return Encoding.GetEncoding("GB2312").GetString(Encoding.UTF8.GetBytes(source));
        }

        /// <summary>
        /// 将 UTF8 值转换为 GB2312 字符串 (如：娴嬭瘯 -> 测试)
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string FromUTF8ToGB(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            return Encoding.UTF8.GetString(Encoding.GetEncoding("GB2312").GetBytes(source));
        }


        /// <summary>
        /// 由16进制转为汉字字符串（如：B2E2 -> 测 ）
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string FromHex(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            byte[] oribyte = new byte[source.Length / 2];
            for (int i = 0; i < source.Length; i += 2)
            {
                //string str = Convert.ToInt32(source.Substring(i, 2), 16).ToString();
                oribyte[i / 2] = Convert.ToByte(source.Substring(i, 2), 16);
            }
            return Encoding.Default.GetString(oribyte);
        }

        /// <summary>
        /// 字符串转为16进制字符串（如：测 -> B2E2 ）
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToHex(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            int i = source.Length;
            string temp;
            string end = "";
            byte[] array;
            int i1, i2;
            for (int j = 0; j < i; j++)
            {
                temp = source.Substring(j, 1);
                array = Encoding.Default.GetBytes(temp);
                if (array.Length.ToString() == "1")
                {
                    i1 = Convert.ToInt32(array[0]);
                    end += Convert.ToString(i1, 16);
                }
                else
                {
                    i1 = Convert.ToInt32(array[0]);
                    i2 = Convert.ToInt32(array[1]);
                    end += Convert.ToString(i1, 16);
                    end += Convert.ToString(i2, 16);
                }
            }
            return end.ToUpper();
        }

        /// <summary>
        /// 字符串转为unicode字符串（如：测试 -> &#27979;&#35797;）
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToUnicode(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            StringBuilder sa = new StringBuilder();//Unicode
            string s1;
            string s2;
            for (int i = 0; i < source.Length; i++)
            {
                byte[] bt = Encoding.Unicode.GetBytes(source.Substring(i, 1));
                if (bt.Length > 1)//判断是否汉字
                {
                    s1 = Convert.ToString((short)(bt[1] - '\0'), 16);//转化为16进制字符串
                    s2 = Convert.ToString((short)(bt[0] - '\0'), 16);//转化为16进制字符串
                    s1 = (s1.Length == 1 ? "0" : "") + s1;//不足位补0
                    s2 = (s2.Length == 1 ? "0" : "") + s2;//不足位补0
                    sa.Append("&#" + Convert.ToInt32(s1 + s2, 16) + ";");
                }
            }

            return sa.ToString();
        }


        /// <summary>
        /// 字符串转为UTF8字符串（如：测试 -> \u6d4b\u8bd5）
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToUTF8(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            StringBuilder sb = new StringBuilder();//UTF8
            string s1;
            string s2;
            for (int i = 0; i < source.Length; i++)
            {
                byte[] bt = Encoding.Unicode.GetBytes(source.Substring(i, 1));
                if (bt.Length > 1)//判断是否汉字
                {
                    s1 = Convert.ToString((short)(bt[1] - '\0'), 16);//转化为16进制字符串
                    s2 = Convert.ToString((short)(bt[0] - '\0'), 16);//转化为16进制字符串
                    s1 = (s1.Length == 1 ? "0" : "") + s1;//不足位补0
                    s2 = (s2.Length == 1 ? "0" : "") + s2;//不足位补0
                    sb.Append("\\u" + s1 + s2);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 将字符串转为安全的Sql字符串，不建议使用。尽可能使用参数化查询来避免
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToSafeSql(this string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }
            else
            {
                return source.Replace("'", "''");
            }
        }

        /// <summary>
        /// 将字符串转换化安全的js字符串值（对字符串中的' "进行转义) 
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToSafeJsString(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            source = source.Replace("'", "\\'");
            source = source.Replace("\"", "\\\"");
            source = source.Replace("\r\n", "").Replace("\r", "").Replace("\n", "");
            return source;
        }

        /// <summary>
        /// 去除换行符（包括\r\n、\r、\n）
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string RemoveLineFeed(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            return source.Replace("\r\n", "").Replace("\r", "").Replace("\n", "");
        }

        /// <summary>
        /// 注释like操作字符串中出现的特殊符号
        /// </summary>
        /// <remarks>注意：如果like查询中本身有使用到特殊字符，请不要使用此方法</remarks>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToEscapeRegChars(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            //[符号要第一个替换
            source = source.Replace("[", "[[]");

            source = source.Replace("%", "[%]");
            source = source.Replace("_", "[_]");
            source = source.Replace("^", "[^]");
            return source;
        }

        /// <summary>
        /// 将字符串包装成 &lt;![CDATA[字符串]]&gt; 形式
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string WrapWithCData(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            return string.Format("<![CDATA[{0}]]>", source);
        }

        /// <summary>
        /// 将字符串转换化安全的XML字符串值
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToSafeXmlString(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            return source.Replace(">", "&gt;").Replace("<", "&lt;").Replace("&", "&amp;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }

        /// <summary>   
        /// 将字母，数字由全角转化为半角   
        /// </summary>   
        /// <returns></returns>   
        public static string NarrowToSmall(this string inputString)
        {
            char[] c = inputString.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                byte[] b = System.Text.Encoding.Unicode.GetBytes(c, i, 1);
                if (b.Length == 2)
                {
                    if (b[1] == 255)
                    {
                        b[0] = (byte)(b[0] + 32);
                        b[1] = 0;
                        c[i] = System.Text.Encoding.Unicode.GetChars(b)[0];
                    }
                }
            }
            string returnString = new string(c);
            return returnString;   // 返回半角字符   
        }
        
        /// <summary>   
        /// 将字母，数字由半角转化为全角   
        /// </summary>   
        /// <param name="inputString"></param>   
        /// <param name="lstTODOChar">需要处理的字符,默认全部处理</param>   
        /// <returns></returns>   
        public static string NarrowToBig(this string inputString, List<char> lstTODOChar = null)
        {
            char[] c = inputString.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (lstTODOChar != null && !lstTODOChar.Contains(c[i]))
                {
                    continue;
                }

                byte[] b = System.Text.Encoding.Unicode.GetBytes(c, i, 1);
                if (b.Length == 2)
                {
                    if (b[1] == 0)
                    {
                        b[0] = (byte)(b[0] - 32);
                        b[1] = 255;
                        c[i] = System.Text.Encoding.Unicode.GetChars(b)[0];
                    }
                }
            }
            string returnString = new string(c);
            return returnString;   // 返回全角字符   
        }
        #endregion

        #region 类型转换
        /// <summary>
        /// 将字符串转成Int32类型，如果转换失败，则返回-1
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns></returns>
        public static int ToInt32(this string source)
        {
            return source.ToInt32(-1);
        }
        /// <summary>
        /// 将字符串转成Int32类型
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="defaultValue">如果转换失败，返回的数值</param>
        /// <returns></returns>
        public static int ToInt32(this string source, int defaultValue)
        {
            if (!string.IsNullOrEmpty(source))
            {
                int result;
                if (Int32.TryParse(source, out result))
                {
                    return result;
                }
            }
            return defaultValue;
        }
        /// <summary>
        /// 将字符串转成Int64类型，如果转换失败，则返回-1
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns></returns>
        public static Int64 ToInt64(this string source)
        {
            return source.ToInt64(-1);
        }
        /// <summary>
        /// 将字符串转成Int64类型
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="defaultValue">如果转换失败，返回的数值</param>
        /// <returns></returns>
        public static Int64 ToInt64(this string source, Int64 defaultValue)
        {
            if (!string.IsNullOrEmpty(source))
            {
                Int64 result;
                if (Int64.TryParse(source, out result))
                {
                    return result;
                }
            }
            return defaultValue;
        }
        /// <summary>
        /// 将字符串转成double类型，如果转换失败，则返回-1
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns></returns>
        public static double ToDouble(this string source)
        {
            return source.ToDouble(-1.0);
        }
        /// <summary>
        /// 将字符串转成double类型
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="defaultValue">如果转换失败，返回的数值</param>
        /// <returns></returns>
        public static double ToDouble(this string source, double defaultValue)
        {
            if (!string.IsNullOrEmpty(source))
            {
                double result;
                if (Double.TryParse(source, out result))
                {
                    return result;
                }
            }
            return defaultValue;
        }
        /// <summary>
        /// 将字符串转成DateTime类型，如果转换失败，则返回当前时间
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns></returns>
        public static DateTime ToDateTime(this string source)
        {
            return source.ToDateTime(DateTime.Now);
        }
        /// <summary>
        /// 将字符串转成DateTime类型
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="defaultValue">如果转换失败，返回的默认时间</param>
        /// <returns></returns>
        public static DateTime ToDateTime(this string source, DateTime defaultValue)
        {
            if (!string.IsNullOrEmpty(source))
            {
                DateTime result;
                if (DateTime.TryParse(source, out result))
                {
                    return result;
                }
            }
            return defaultValue;
        }
        /// <summary>
        /// 将字符串转成Boolean类型，如果转换失败，则返回false
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns></returns>
        public static bool ToBoolean(this string source)
        {
            if (!string.IsNullOrEmpty(source))
            {
                Boolean result;
                if (Boolean.TryParse(source, out result))
                {
                    return result;
                }
            }
            return false;
        }
        /// <summary>
        /// 将字符串转成指定的枚举类型(字符串可以是枚举的名称也可以是枚举值)
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="source">源字符串</param>
        /// <param name="defaultValue">如果转换失败，返回默认的枚举项</param>
        /// <returns></returns>
        public static T ToEnum<T>(this string source, T defaultValue)
        {
            if (!string.IsNullOrEmpty(source))
            {
                try
                {
                    T value = (T)Enum.Parse(typeof(T), source, true);
                    if (Enum.IsDefined(typeof(T), value))
                    {
                        return value;
                    }
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 将字符串转成指定的枚举类型(字符串可以是枚举的名称也可以是枚举值)
        /// <remarks>支持枚举值的并集</remarks>
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="source">源字符串</param>
        /// <param name="defaultValue">如果转换失败，返回默认的枚举项</param>
        /// <returns></returns>
        public static T ToEnumExt<T>(this string source, T defaultValue)
        {
            if (!string.IsNullOrEmpty(source))
            {
                try
                {
                    return (T)Enum.Parse(typeof(T), source, true);
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
        #endregion

        /// <summary>
        /// 比较字符串是否相同（区分大小写）
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool EqualsOrdinal(this string source, string target)
        {
            return source.Equals(target, StringComparison.Ordinal);
        }
        /// <summary>
        /// 比较字符串是否相同（不区分大小写）
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool EqualsIgnoreCase(this string source, string target)
        {
            return source.Equals(target, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 查找字符串位置（区分大小写）
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static int IndexOrdinal(this string source, string target)
        {
            return source.IndexOf(target, StringComparison.Ordinal);
        }

        /// <summary>
        /// 查找字符串位置（不区分大小写）
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static int IndexIgnoreCase(this string source, string target)
        {
            return source.IndexOf(target, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 比较字符串（不区分大小写）
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool StartWithIgnoreCase(this string source, string target)
        {
            return source.StartsWith(target, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 文件不能以特定字符串打头的文件名判断
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool StartWithDosDeservedString(this string source)
        {
            var lstDeservedStartString = new List<string> { "CON.", "PRN.", "AUX.", "NUL.", "COM1.", "COM2.", "COM3.", "COM4.", "COM5.", "COM6.", "COM7.", "COM8.", "COM9.", "LPT1" };
            return lstDeservedStartString.Exists(item => source.StartWithIgnoreCase(item));
        }

        /// <summary>
        /// 去除文件名中不可用于文件名的11个字符
        /// </summary>
        /// <param name="filenameNoDir"></param>
        /// <param name="replaceWith">用什么字符串替换</param>
        /// <returns></returns>
        public static string ReplaceNonValidChars(this string filenameNoDir, string replaceWith)
        {
            if (string.IsNullOrEmpty(filenameNoDir))
                return string.Empty;
            //替换这9个字符<>/\|:"*? 以及 回车换行
            return Regex.Replace(filenameNoDir, @"[\<\>\/\\\|\:""\*\?\r\n]", replaceWith, RegexOptions.Compiled);
        }

        /// <summary>
        /// 去除非打印字符
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string RemoveNonPrintChars(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            Regex reg = new Regex("[\x00-\x08\x0B\x0C\x0E-\x1F]");
            return reg.Replace(source, "");
        }

        /// <summary>
        /// 获取汉字字符串的首字母
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string GetPinYin(this string source)
        {
            return GetChineseSpell(source);
        }

        /// <summary>
        /// 取得汉字字符串的拼音的首字母
        /// </summary>
        /// <param name="strText"></param>
        /// <returns></returns>
        private static string GetChineseSpell(string strText)
        {
            if (string.IsNullOrEmpty(strText))
                return string.Empty;
            int len = strText.Length;
            string myStr = "";
            for (int i = 0; i < len; i++)
            {
                myStr += getSpell(strText.Substring(i, 1));
            }
            return myStr;
        }

        /// <summary>
        /// 取得汉字字符的拼音的首字母
        /// </summary>
        /// <param name="cnChar"></param>
        /// <returns></returns>
        private static string getSpell(string cnChar)
        {
            if (string.IsNullOrEmpty(cnChar))
                return string.Empty;
            byte[] arrCN = Encoding.Default.GetBytes(cnChar);
            if (arrCN.Length > 1)
            {
                int area = arrCN[0];
                int pos = arrCN[1];
                int code = (area << 8) + pos;
                int[] areacode = { 45217, 45253, 45761, 46318, 46826, 47010, 47297, 47614, 48119, 48119, 49062, 49324, 49896, 50371, 50614, 50622, 50906, 51387, 51446, 52218, 52698, 52698, 52698, 52980, 53689, 54481 };
                for (int i = 0; i < 26; i++)
                {
                    int max = 55290;
                    if (i != 25) max = areacode[i + 1];
                    if (areacode[i] <= code && code < max)
                    {
                        return Encoding.Default.GetString(new byte[] { (byte)(65 + i) });
                    }
                }
                return "*";
            }
            else return cnChar;
        }

        #region 基于Sunday算法的字符串检索,已修订bug
        /// <summary>
        /// 更高效的算法，取代String.IndexOf(value, StringComparison)方法
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static int SundaySearch(this string text, string pattern)
        {
            /*
             说明：如果使用下面的代码去重载，效率变的跟IndexOf差不多了，估计是2次方法调用堆栈造成的
             return SundaySearch(text, pattern, false);
             */
            if (text == null || pattern == null)
                throw new ArgumentException("argsment can't be null");

            int plen = pattern.Length;
            int tlen = text.Length;

            // 子串为string.Empty时，返回0（IndexOf也是返回0）
            if (plen <= 0)
                return 0;
            if (tlen <= 0 || plen > tlen)
                return -1;

            int pe = plen - 1;
            int i = 0;
            int j = 0;
            int tb = 0;
            int matched = 0;            //增加标志位解决在临界点情况下匹配不到却认为是匹配到的bug
            int deadCheck = tlen * tlen;//避免陷入死循环问题

            while (i < tlen && j < plen && deadCheck >= 0)
            {
                deadCheck--;
                if (text[i] == pattern[j])
                {
                    i++;
                    j++;
                    matched++;
                }
                else
                {
                    int k = plen - 1;
                    if (pe >= tlen - 1)
                    {
                        break;
                    }

                    while (k > 0 && text[pe + 1] != pattern[k])
                    {
                        k--;
                    }
                    int gap = plen - k - matched; //需要减去已匹配的数量
                    i += gap;
                    pe = i + plen - 1;
                    tb = i;
                    j = 0;
                    matched = 0;
                }
            }
            if (matched == plen)
            {
                return tb;
            }
            return -1;
        }

        /// <summary>
        /// 更高效的算法，取代String.IndexOf(value, StringComparison)方法
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pattern"></param>
        /// <param name="ignoreCase">是否忽略字母的大小写（只支持字母）</param>
        /// <returns></returns>
        public static int SundaySearch(this string text, string pattern, bool ignoreCase)
        {
            if (text == null || pattern == null)
                throw new ArgumentException("argsment can't be null");

            int plen = pattern.Length;
            int tlen = text.Length;

            // 子串为string.Empty时，返回0（IndexOf也是返回0）
            if (plen <= 0)
                return 0;
            if (tlen <= 0 || plen > tlen)
                return -1;

            int pe = plen - 1;
            int i = 0;
            int j = 0;
            int tb = 0;
            int matched = 0;            //增加标志位解决在临界点情况下匹配不到却认为是匹配到的bug
            int deadCheck = tlen * tlen;//避免陷入死循环问题

            while (i < tlen && j < plen && deadCheck >= 0)
            {
                deadCheck--;
                if (text[i].CharEquals(pattern[j], ignoreCase))
                {
                    i++;
                    j++;
                    matched++;
                }
                else
                {
                    int k = plen - 1;
                    if (pe >= tlen - 1)
                    {
                        break;
                    }

                    while (k > 0 && !text[pe + 1].CharEquals(pattern[k], ignoreCase))
                    {
                        k--;
                    }
                    int gap = plen - k - matched; //需要减去已匹配的数量
                    i += gap;
                    pe = i + plen - 1;
                    tb = i;
                    j = 0;
                    matched = 0;
                }
            }
            if (matched == plen)
            {
                return tb;
            }
            return -1;
        }

        /// <summary>
        /// 比较2个字符是否相等
        /// </summary>
        /// <param name="ch"></param>
        /// <param name="pattern"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static bool CharEquals(this char ch, char pattern, bool ignoreCase)
        {
            if (!ignoreCase)
                return ch == pattern;

            // (int)'a' = 97    (int)'z' = 122  (int)'A' = 65   (int)'Z' = 90
            const int a = (int)'a';
            const int z = (int)'z';
            const int A = (int)'A';
            const int Z = (int)'Z';
            const int DIFF = a - A;

            if ((ch >= A && ch <= Z))
            {
                if (pattern >= A && pattern <= Z)
                {
                    return ch == pattern;
                }
                else if (pattern >= a && pattern <= z)
                {
                    return ch == pattern - DIFF;
                }
                else
                {
                    return false;
                }
            }
            if ((ch >= a && ch <= z))
            {
                if (pattern >= A && pattern <= Z)
                {
                    return ch == pattern + DIFF;
                }
                else if (pattern >= a && pattern <= z)
                {
                    return ch == pattern;
                }
                else
                {
                    return false;
                }
            }
            return ch == pattern;
        }
        #endregion


        ///// <summary>
        ///// 在32位系统和64位系统上生成的HashCode会不一致，请统一使用此方法获取哈希值
        ///// </summary>
        ///// <param name="s"></param>
        ///// <param name="Abs">是否只返回绝对值</param>
        ///// <returns></returns>
        //public static unsafe int GetHashCode32(this string s, bool Abs = true)
        //{
        //    fixed (char* chRef = s.ToCharArray())
        //    {
        //        char* chPtr = chRef;
        //        int num = 0x15051505;
        //        int num2 = num;
        //        int* numPtr = (int*)chPtr;
        //        for (int i = s.Length; i > 0; i -= 4)
        //        {
        //            num = (((num << 5) + num) + (num >> 0x1b)) ^ numPtr[0];
        //            if (i <= 2)
        //            {
        //                break;
        //            }
        //            num2 = (((num2 << 5) + num2) + (num2 >> 0x1b)) ^ numPtr[1];
        //            numPtr += 2;
        //        }
        //        int ret = num + (num2 * 0x5d588b65);
        //        if (Abs)
        //        {
        //            // 如果ret是int的最小值，取绝对值时，会报错：对 2 的补数的最小值求反的操作无效
        //            if (ret == int.MinValue)
        //                ret = int.MaxValue;
        //            else
        //                ret = Math.Abs(ret);
        //        }
        //        return ret;
        //    }
        //}

        /// <summary>
        /// 通过文件后缀简单判断获取文件的contenttype
        /// <remarks>当前仅判断软件包、铃声、压缩包、图片等资源</remarks>
        /// </summary>
        /// <param name="source">文件路径</param>
        /// <param name="defaultContentType"></param>
        /// <returns></returns>
        public static string GetContentType(this string source, string defaultContentType = "application/octet-stream")
        {
            var ext = Path.GetExtension(source) ?? "";
            switch (ext.ToLower())
            {
                case ".png":
                    return  "image/png";
                case ".jpe":
                case ".jpg":
                case ".jpeg":
                    return  "image/jpeg";
                case ".bmp":
                    return  "image/bmp";
                case ".gif":
                    return  "image/gif";
                case ".tiff":
                case ".tif":
                    return  "image/tiff";
                case ".mp3":
                    return "audio/mpeg";
                case ".wma":
                    return "audio/x-ms-wma";
                case ".ipa":
                    return "application/vnd.iphone";
                case ".apk":
                    return "application/vnd.android.package-archive";
                case ".jar":
                    return "application/java-archive";
                case ".rar":
                    return "application/x-rar-compressed";
                case ".zip":
                    return "application/zip";
                case ".m4r":
                case ".caf":
                case ".cab":
                case ".sis":
                case ".sisx":
                case ".wgz":
                case ".xdt":
                    return "application/octet-stream";
                case ".info":
                    return "text/plain";
                default:
                    return defaultContentType;
            }
        }



        /// <summary>
        /// 标准MD5加密
        /// </summary>
        /// <param name="source">待加密字符串</param>
        /// <param name="addKey">附加字符串</param>
        /// <param name="encoding">编码方式，为空时使用UTF-8</param>
        /// <returns></returns>
        public static string MD5(this string source, string addKey = "", Encoding encoding = null)
        {
            if (addKey.Length > 0)
            {
                source = source + addKey;
            }
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }

            byte[] datSource = encoding.GetBytes(source);
            byte[] newSource;
            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                newSource = md5.ComputeHash(datSource);
            }
            string byte2String = BitConverter.ToString(newSource).Replace("-", "").ToLower();
            return byte2String;
        }
    }
}