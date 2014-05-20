namespace PlanServerService
{
    /// <summary>
    /// 目录或文件排序方式
    /// </summary>
    public enum SortType
    {
        /// <summary>
        /// 按文件或目录名称顺序
        /// </summary>
        Name = 0,
        /// <summary>
        /// 按文件或目录名称倒序
        /// </summary>
        NameDesc = 1,
        /// <summary>
        /// 按文件扩展名顺序
        /// </summary>
        Extention = 2,
        /// <summary>
        /// 按文件扩展名倒序
        /// </summary>
        ExtentionDesc = 3,
        /// <summary>
        /// 按文件大小顺序
        /// </summary>
        Size = 4,
        /// <summary>
        /// 按文件大小倒序
        /// </summary>
        SizeDesc = 5,
        /// <summary>
        /// 按文件或目录修改时间顺序
        /// </summary>
        ModifyTime = 6,
        /// <summary>
        /// 按文件或目录修改时间倒序
        /// </summary>
        ModifyTimeDesc = 7
    }
}
