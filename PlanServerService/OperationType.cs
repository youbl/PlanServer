namespace PlanServerService
{
    public enum OperationType
    {
        /// <summary>
        /// 获取所有任务
        /// </summary>
        GetAllTasks = 0,
        /// <summary>
        /// 保存多个任务
        /// </summary>
        SaveTasks = 1,
        /// <summary>
        /// 删除多个任务
        /// </summary>
        DelTasks = 2,
        /// <summary>
        /// 立即启动或停止任务
        /// </summary>
        Immediate = 3,


        /// <summary>
        /// 显示目录列表。多个参数以 | 分隔.
        /// 参数1：目录完整路径.
        /// 参数2：排序方法，int型，参考SortType枚举.
        /// 参数3：0不返回文件md5，1返回文件md5.
        /// </summary>
        DirShow = 100,
        /// <summary>
        /// 重命名目录。多个参数以 | 分隔
        /// 参数1：要重命名的目录完整路径
        /// 参数2：新目录名（含路径时，仅最后一个斜杠后的目录名有效，如C:\abc\def,只取def）
        /// </summary>
        DirRename = 101,
        /// <summary>
        /// 重命名文件。多个参数以 | 分隔
        /// 参数1：要重命名的文件完整路径
        /// 参数2：新文件名（含路径时，仅最后一个斜杠后的文件名有效，如C:\abc\def.txt,只取def.txt）
        /// </summary>
        FileRename = 102,
        /// <summary>
        /// 删除目录或文件。多个参数以 | 分隔.
        /// 参数1：父目录完整路径.
        /// 参数2：要删除的子文件列表,多个文件以 * 分隔.
        /// 参数3：要删除的子目录列表,多个目录以 * 分隔.
        /// 返回值：删除文件数|删除目录数
        /// </summary>
        DirDel = 103,
        /// <summary>
        /// 获取目录大小。
        /// 参数1：目录完整路径.
        /// 返回值：long字节大小|子目录数|文件数
        /// </summary>
        DirSizeGet = 104,
        /// <summary>
        /// zip下载多个目录或文件。多个参数以 | 分隔
        /// 参数1：父目录完整路径
        /// 参数2：要打包的子文件列表,多个文件以 * 分隔.
        /// 参数3：要打包的子目录列表,多个目录以 * 分隔.
        /// </summary>
        DirDownloadZip = 105,
        /// <summary>
        /// 下载单个文件。
        /// 参数1：文件完整路径
        /// </summary>
        FileDownload = 106,
        /// <summary>
        /// 创建新目录。
        /// 参数1：新目录完整路径
        /// </summary>
        DirCreate = 108,
        /// <summary>
        /// zip解压指定文件。多个参数以 | 分隔
        /// 参数1：zip文件完整路径。
        /// 参数2：要解压到的目录，空表示zip文件所在目录
        /// </summary>
        FileUnZip = 109,
        /// <summary>
        /// 移动目录或文件列表。多个参数以 | 分隔.
        /// 参数1：父目录完整路径.
        /// 参数2：移动到哪个目录下.
        /// 参数3：要移动的子文件列表,多个文件以 * 分隔.
        /// 参数4：要移动的子目录列表,多个目录以 * 分隔.
        /// 返回值：移动文件数|移动目录数|移动的目录下的子文件数
        /// </summary>
        DirMove = 110,
        /// <summary>
        /// 上传文件。多个参数以 | 分隔.
        /// 参数1：目录完整路径.
        /// 参数2：上传文件名.
        /// </summary>
        FileUpload = 111,

        /// <summary>
        /// 配合文件下载使用的枚举，下载本地文件
        /// </summary>
        LocalFileDown = 119,
        /// <summary>
        /// 配合文件下载使用的枚举，直接显示本地文件
        /// </summary>
        LocalFileOpen = 120,


        /// <summary>
        /// 加载并运行指定的dll里的方法
        /// </summary>
        RunMethod = 1024,

        /// <summary>
        /// 退出登录
        /// </summary>
        LogOut = 2048,

        AddAdminIp = 2049,
        DelAdminIp = 2050,
        AddAdminServer = 2051,
        DelAdminServer = 2052,
        GetAdminServers = 2053,

        /// <summary>
        /// 获取进程列表
        /// </summary>
        GetProcesses = 3000,
    }
}
