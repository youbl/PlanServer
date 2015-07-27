using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PlanServerService.FileAdmin
{
    /// <summary>
    /// 返回的对象类
    /// </summary>
    [DataContract(Name = "res")]
    public class FileResult
    {
        /// <summary>
        /// 父目录名称
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false, Name = "d")]
        public string Dir { get; set; }
        /// <summary>
        /// 子目录列表
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false, Name = "r")]
        public FileItem[] SubDirs { get; set; }
        /// <summary>
        /// 子文件列表
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false, Name = "f")]
        public FileItem[] SubFiles { get; set; }
        /// <summary>
        /// 服务器当前时间
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false, Name = "s")]
        public DateTime ServerTime { get; set; }
        /// <summary>
        /// 服务器IP列表
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false, Name = "p")]
        public string ServerIp { get; set; }
        /// <summary>
        /// 服务器其它信息，如磁盘空间
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false, Name = "o")]
        public string Others { get; set; }

    }
}
