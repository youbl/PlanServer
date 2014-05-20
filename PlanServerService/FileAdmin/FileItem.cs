using System;
using System.Runtime.Serialization;

namespace PlanServerService.FileAdmin
{
    /// <summary>
    /// 文件或目录实体
    /// </summary>
    [DataContract(Name = "fi")]
    public class FileItem
    {
        /// <summary>
        /// 名称
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false, Name = "n")]
        public string Name { get; set; }
        /// <summary>
        /// 字节大小
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false, Name = "s")]
        public long Size { get; set; }
        /// <summary>
        /// 最后修改时间
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false, Name = "t")]
        public DateTime LastModifyTime { get; set; }
        /// <summary>
        /// 是文件还是目录
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false, Name = "f")]
        public bool IsFile { get; set; }
        /// <summary>
        /// 文件MD5值（目录此值无效）
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false, Name = "m")]
        public string FileMd5 { get; set; }

    }
}
