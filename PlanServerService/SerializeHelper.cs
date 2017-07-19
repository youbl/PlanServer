using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace PlanServerService
{
    /// <summary>
    /// 序列化辅助类
    /// </summary>
    public class SerializeHelper
    {
        /// <summary>
        /// 序列化对象为二进制文件
        /// </summary>
        /// <typeparam name="T">需要序列化的对象类型，必须声明[Serializable]特征，且必须是public类</typeparam>
        /// <param name="obj">需要序列化的对象</param>
        /// <param name="serializeFilePath">序列化后的物理文件路径</param>
        public static void BinarySerialize<T>(T obj, string serializeFilePath)
        {
            using (var fs = new FileStream(serializeFilePath, FileMode.Create))
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(fs, obj);
            }
        }
        /// <summary>
        /// 反序列化二进制文件为对象
        /// </summary>
        /// <typeparam name="T">需要反序列化的对象类型，必须声明[Serializable]特征</typeparam>
        /// <param name="serializeFilePath">反序列化对象的物理文件路径</param>
        public static T BinaryDeserialize<T>(string serializeFilePath) where T : class
        {
            using (var fs = new FileStream(serializeFilePath, FileMode.Open))
            {
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(fs) as T;
            }
        }

        /// <summary>
        /// 序列化对象为XML文件
        /// </summary>
        /// <typeparam name="T">
        /// 需要序列化的对象类型，必须声明[Serializable]特征，且属性类也必须声明[Serializable]特征。
        /// 如果属性是抽象类或接口，必须声明[System.Xml.Serialization.XmlInclude(typeof(子类))]特征
        /// </typeparam>
        /// <param name="obj">需要序列化的对象</param>
        /// <param name="serializeFilePath">序列化后的物理文件路径</param>
        [Obsolete("Obsoleted because this method not support HashTable, use XmlSerializeNew instead")]
        public static void XmlSerialize<T>(T obj, string serializeFilePath)
        {
            using (var sw = new StreamWriter(serializeFilePath, false))
            {
                var formatter = new XmlSerializer(typeof(T));
                formatter.Serialize(sw, obj);
            }
        }
        /// <summary>
        /// 反序列化XML文件为对象
        /// </summary>
        /// <typeparam name="T">
        /// 需要序列化的对象类型，必须声明[Serializable]特征，且属性类也必须声明[Serializable]特征。
        /// 如果属性是抽象类或接口，必须声明[System.Xml.Serialization.XmlInclude(typeof(子类))]特征
        /// </typeparam>
        /// <param name="serializeFilePath">反序列化对象的物理文件路径</param>
        [Obsolete("Obsoleted because this method not support HashTable, use XmlDeserializeNew instead")]
        public static T XmlDeserialize<T>(string serializeFilePath) where T : class
        {
            //using (var sr = new StreamReader(serializeFilePath))
            using(var xmlreader = new XmlTextReader(serializeFilePath))
            {
                // [\x0-\x8\x11\x12\x14-\x32]
                // 默认为true，如果序列化的对象含有比如0x1e之类的非打印字符，反序列化就会出错，因此设置为false http://msdn.microsoft.com/en-us/library/aa302290.aspx
                xmlreader.Normalization = false;
                xmlreader.WhitespaceHandling = WhitespaceHandling.Significant;
                xmlreader.XmlResolver = null;
                var formatter = new XmlSerializer(typeof(T));
                return formatter.Deserialize(xmlreader) as T;
            }
        }

        /// <summary>
        /// 把对象序列化为Xml字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        [Obsolete("Obsoleted because this method not support HashTable, use XmlSerializeToStrNew instead")]
        public static String XmlSerializeToStr<T>(T obj)
        {
            var formatter = new XmlSerializer(typeof(T));
            using (var memory = new MemoryStream())
            using (var xmlTextWriter = new XmlTextWriter(memory, Encoding.UTF8))
            {
                formatter.Serialize(xmlTextWriter, obj);
                using (var mem2 = (MemoryStream) xmlTextWriter.BaseStream)
                {
                    var ret = Encoding.UTF8.GetString(mem2.ToArray());
                    return ret;
                }
            }
        }

        /// <summary>
        /// 把Xml字符串反序列化成对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xml"></param>
        /// <returns></returns>
        [Obsolete("Obsoleted because this method not support HashTable, use XmlDeserializeFromStrNew instead")]
        public static T XmlDeserializeFromStr<T>(string xml) where T : class
        {
            var xs = new XmlSerializer(typeof(T));
            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            using (var xmlreader = new XmlTextReader(memoryStream))
            {
                // [\x0-\x8\x11\x12\x14-\x32]
                // 默认为true，如果序列化的对象含有比如0x1e之类的非打印字符，反序列化就会出错，因此设置为false http://msdn.microsoft.com/en-us/library/aa302290.aspx
                xmlreader.Normalization = false;
                xmlreader.WhitespaceHandling = WhitespaceHandling.Significant;
                xmlreader.XmlResolver = null;
                return xs.Deserialize(xmlreader) as T;
            }
        }

        /// <summary>
        /// 序列化对象为XML文件
        /// </summary>
        /// <typeparam name="T">
        /// 需要序列化的对象类型，必须声明[Serializable]特征，且属性类也必须声明[Serializable]特征。
        /// 如果属性是抽象类或接口，必须声明[System.Xml.Serialization.XmlInclude(typeof(子类))]特征
        /// </typeparam>
        /// <param name="obj">需要序列化的对象</param>
        /// <param name="serializeFilePath">序列化后的物理文件路径</param>
        public static void XmlSerializeNew<T>(T obj, string serializeFilePath)
        {
            //FileMode.Create:创建或覆盖     FileMode.CreateNew：创建，文件已经存在时异常
            using (FileStream fs = new FileStream(serializeFilePath, FileMode.Create))
            {
                var formatter = new DataContractSerializer(typeof(T));
                formatter.WriteObject(fs, obj);
            }
        }
        /// <summary>
        /// 反序列化XML文件为对象
        /// </summary>
        /// <typeparam name="T">
        /// 需要序列化的对象类型，必须声明[Serializable]特征，且属性类也必须声明[Serializable]特征。
        /// 如果属性是抽象类或接口，必须声明[System.Xml.Serialization.XmlInclude(typeof(子类))]特征
        /// </typeparam>
        /// <param name="serializeFilePath">反序列化对象的物理文件路径</param>
        public static T XmlDeserializeNew<T>(string serializeFilePath) where T : class
        {
            //using (var xmlreader = XmlDictionaryReader.CreateTextReader(serializeFilePath))
            //using (var sr = new StreamReader(serializeFilePath))
            using (var xmlreader = new XmlTextReader(serializeFilePath))
            {
                // [\x0-\x8\x11\x12\x14-\x32]
                // 默认为true，如果序列化的对象含有比如0x1e之类的非打印字符，反序列化就会出错，因此设置为false http://msdn.microsoft.com/en-us/library/aa302290.aspx
                xmlreader.Normalization = false;
                xmlreader.WhitespaceHandling = WhitespaceHandling.Significant;
                xmlreader.XmlResolver = null;
                var formatter = new DataContractSerializer(typeof(T));
                return formatter.ReadObject(xmlreader) as T;
            }
        }

        /// <summary>
        /// 把对象序列化为Xml字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static String XmlSerializeToStrNew<T>(T obj)
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
        public static T XmlDeserializeFromStrNew<T>(string xml) where T : class
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
    }
}
