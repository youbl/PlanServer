using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace PlanServerService
{
    public static class ProcessHelper
    {

        /// <summary>
        /// 杀死指定的进程列表
        /// </summary>
        /// <param name="processes"></param>
        /// <returns></returns>
        public static int KillProcesses(List<ProcessItem> processes)
        {
            var ret = 0;
            foreach (ProcessItem process in processes)
            {
                var procName = process.name;
                try
                {
                    KillProcessByPid(process.pid);
                    ret++;
                }
                catch (Exception exp)
                {
                    Utils.Output("KillProcessByPid出错 " + procName, exp);
                }
            }
            return ret;
        }
        // 不能根据进程名杀，可能杀错，比如一个程序放在2个目录下
        // 要根据exe程序物理路径杀进程，并返回杀死进程个数(如果同一exe启动多次，会被全部杀死) 通过Win32_Process查询到id后杀死



        // 根据进程id杀进程
        // ReSharper disable once UnusedMethodReturnValue.Local
        private static bool KillProcessByPid(int pid)
        {
            using (var process = FindProcessByPid(pid))
            {
                if (process != null && !process.CloseMainWindow())
                {
                    process.Kill();
                    return true;
                }
            }
            return false;
        }
        public static Process FindProcessByPid(int pid)
        {
            return Process.GetProcessById(pid);
        }


        /// <summary>
        /// 启动进程的锁，避免同时启动多个进程
        /// </summary>
        private static object _objStartLock = new object();

        /// <summary>
        /// 进程不存在时，启动指定的任务
        /// </summary>
        /// <param name="exepath"></param>
        /// <param name="exepara"></param>
        /// <returns></returns>
        public static int CheckAndStartProcess(string exepath, string exepara)
        {
            lock (_objStartLock)
            {
                if (ProcessItem.Exists(exepath))
                {
                    return 0;
                }
                return StartProcess(exepath, exepara);
            }
        }

        // 启动指定的任务
        public static int StartProcess(string exepath, string exepara)
        {
            var p = new Process();
            p.StartInfo.FileName = exepath;
            p.StartInfo.UseShellExecute = true; // 在当前进程中启动，不使用系统外壳程序启动
            //p.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;// 让dos窗体最大化
            p.StartInfo.Arguments = exepara; //设定参数，"/C"表示执行完命令后马上退出
            p.StartInfo.RedirectStandardInput = false; //设置为true，后面可以通过StandardInput输入dos命令
            p.StartInfo.RedirectStandardOutput = false;
            p.StartInfo.CreateNoWindow = true;     //不创建窗口
            p.Start();
            //SetWindowPos(p.Handle, 3, Left, Top, Width, Height, 8);
            //p.StandardInput.WriteLine("ping " + url);
            //p.WaitForExit(1000);
            //MessageBox.Show(p.StandardUtils.Output.ReadToEnd());
            //p.Close();
            return p.Id;
        }

        /// <summary>
        /// 根据路径，过滤进程，并按pid排序返回
        /// </summary>
        /// <param name="processes"></param>
        /// <param name="exePath"></param>
        /// <returns></returns>
        public static List<ProcessItem> FilterByPath(List<ProcessItem> processes, string exePath)
        {
            var ret = processes.Where(item => item.exePath.Equals(exePath, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.pid);
            return ret.ToList();
        }


        /*
        // 根据exe程序物理路径杀进程，并返回杀死进程个数(如果同一exe启动多次，会被全部杀死)
        // noCompareProcess不比较的进程名列表
        // win32ExpProcess本次比较过程中出错的进程名列表
        static int KillProcessByPath(string exePath, Dictionary<string, bool> noCompareProcess,
            Dictionary<string, bool> win32ExpProcess)
        {
            int ret = 0;
    
            Process[] allProcess = Process.GetProcesses();
            foreach (Process proc in allProcess)
            {
                string procName = proc.ProcessName.ToLower();
                if (noCompareProcess != null && noCompareProcess.ContainsKey(procName))
                    continue;
                try
                {
                    if (exePath.Equals(proc.MainModule.FileName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!proc.CloseMainWindow())
                        {
                            proc.Kill();
                        }
                        ret++;
                    }
                    //if (proc.Id != 0 && proc.MainWindowHandle.ToInt32() != 0)
                    //{
                    //    // 如果有主窗口
                    //    ProcessModule pm = proc.MainModule;
                    //    String modname = pm.ModuleName;
                    //}
                }
                catch (Win32Exception)//System.ComponentModel.Win32Exception: 拒绝访问。
                {
                    if (win32ExpProcess != null)
                    {
                        win32ExpProcess[procName] = false;
                    }
                }
                catch (Exception exp)
                {
                    // 有些进程无法枚举进程模块
                    Utils.Output("KillProcessByPath出错 " + procName, exp);
                }
            }
            return ret;
            //GC.Collect();
        }
        */



        /*
        // noCompareProcess不比较的进程名列表
        // win32ExpProcess本次比较过程中出错的进程名列表
        [Obsolete("Cann't get process exe path, Please use FindProcessByPath(string exePath) instead.")]
        static int FindProcessByPath(string exePath, Dictionary<string, bool> noCompareProcess,
            Dictionary<string, bool> win32ExpProcess)
        {
            int ret = 0;
            Process[] allProcess = Process.GetProcesses();
            foreach (Process proc in allProcess)
            {
                string procName = proc.ProcessName.ToLower();
                if (noCompareProcess != null && noCompareProcess.ContainsKey(procName))
                    continue;
                try
                {
                    if (exePath.Equals(proc.MainModule.FileName, StringComparison.OrdinalIgnoreCase))
                    {
                        ret++;
                    }
                }
                catch (Win32Exception)//System.ComponentModel.Win32Exception: 拒绝访问。
                {
                    if(win32ExpProcess != null)
                    {
                        win32ExpProcess[procName] = false;
                    }
                }
                catch (Exception exp)
                {
                    // 有些进程无法枚举进程模块
                    Utils.Output("FindProcessByPath出错 " + procName, exp);
                }
            }
            return ret;
        }
        */

    }

    public class ProcessItem
    {
        private static DateTime _cacheTime = DateTime.MinValue;
        private static List<ProcessItem> _cacheProcess = null;
        private const int CACHE_MS = 2000;
        static object _lock = new object();

        /// <summary>
        /// 返回所有进程列表，并缓存2秒，避免频繁读取服务器进程影响性能.
        /// WmiPrvSE进程可能占用高CPU
        /// </summary>
        /// <returns></returns>
        public static List<ProcessItem> GetProcessesAndCache(bool cache = true)
        {
            var now = DateTime.Now;
            lock (_lock)
            {
                if (!cache || _cacheProcess == null || (now - _cacheTime).TotalMilliseconds > CACHE_MS)
                {
                    _cacheProcess = GetAllProcesses();
                    _cacheTime = now;
                }
            }

            return _cacheProcess;
        }

        static List<ProcessItem> GetAllProcesses()
        {
            var ret = new List<ProcessItem>();

            //http://msdn.microsoft.com/en-us/library/windows/desktop/aa394372(v=vs.85).aspx
            var scope = new ManagementScope(@"\\.\root\cimv2");
            var query = new SelectQuery("SELECT * FROM Win32_Process"); // where Name = 'w3wp.exe'
            using (var searcher = new ManagementObjectSearcher(scope, query))
            {
                foreach (var o in searcher.Get())
                {
                    var process = (ManagementObject)o;
                    //StringBuilder sbb = new StringBuilder(10000);
                    //foreach (PropertyData property in process.Properties)
                    //{
                    //    sbb.Append(property.Name + ":" + property.Value + "\r\n");
                    //}
                    var name = Convert.ToString(process["name"]);
                    var exePath = Convert.ToString(process["ExecutablePath"]);
                    var commandLine = Convert.ToString(process["CommandLine"]);
                    var pid = Convert.ToInt32(process["ProcessId"]);
                    var startDate = Convert.ToString(process["CreationDate"]);
                    var mem = Convert.ToInt64(process["WorkingSetSize"]);
                    var memVirtual = Convert.ToInt64(process["VirtualSize"]);
                    var memPage = Convert.ToInt64(process["PagefileUsage"]);

                    ret.Add(new ProcessItem()
                    {
                        commandLine = commandLine,
                        createDate = startDate,
                        exePath = exePath,
                        memory = mem,
                        memoryPage = memPage,
                        memoryVirtual = memVirtual,
                        name = name,
                        pid = pid
                    });
                    //ret.AppendFormat("{0},{1},{2},{4},{3},{5},{6}\r\n",
                    //    pid,
                    //    name,
                    //    mem.ToString(),
                    //    memVirtual.ToString(),
                    //    memPage.ToString(),
                    //    startDate,
                    //    commandLine);
                }
            }
            return ret;
        }

        /// <summary>
        /// 判断指定进程是否运行中。
        /// WmiPrvSE进程可能占用高CPU
        /// </summary>
        /// <param name="exepath">exe全路径</param>
        /// <returns></returns>
        public static bool Exists(string exepath)
        {
            // 经测试，name不区分大小写，斜杠要做转义
            exepath = exepath.Replace("'", "").Replace(@"\", @"\\");
            //http://msdn.microsoft.com/en-us/library/windows/desktop/aa394372(v=vs.85).aspx
            var scope = new ManagementScope(@"\\.\root\cimv2");
            var query = new SelectQuery("SELECT * FROM Win32_Process where ExecutablePath = '" + exepath + "'");
            using (var searcher = new ManagementObjectSearcher(scope, query))
            {
                return searcher.Get().Count > 0;
            }
        }


        /// <summary>
        /// 返回指定路径的进程。
        /// WmiPrvSE进程可能占用高CPU
        /// </summary>
        /// <param name="exepath">exe全路径</param>
        /// <returns></returns>
        public static List<ProcessItem> GetProcessByPath(string exepath)
        {
            var ret = new List<ProcessItem>();
            // 经测试，name不区分大小写，斜杠要做转义
            exepath = exepath.Replace("'", "").Replace(@"\", @"\\");
            //http://msdn.microsoft.com/en-us/library/windows/desktop/aa394372(v=vs.85).aspx
            var scope = new ManagementScope(@"\\.\root\cimv2");
            var query = new SelectQuery("SELECT * FROM Win32_Process where ExecutablePath = '" + exepath + "'");
            using (var searcher = new ManagementObjectSearcher(scope, query))
            {
                foreach (var o in searcher.Get())
                {
                    var process = (ManagementObject)o;
                    var name = Convert.ToString(process["name"]);
                    var exePath = Convert.ToString(process["ExecutablePath"]);
                    var commandLine = Convert.ToString(process["CommandLine"]);
                    var pid = Convert.ToInt32(process["ProcessId"]);
                    var startDate = Convert.ToString(process["CreationDate"]);
                    var mem = Convert.ToInt64(process["WorkingSetSize"]);
                    var memVirtual = Convert.ToInt64(process["VirtualSize"]);
                    var memPage = Convert.ToInt64(process["PagefileUsage"]);
                    ret.Add(new ProcessItem()
                    {
                        commandLine = commandLine,
                        createDate = startDate,
                        exePath = exePath,
                        memory = mem,
                        memoryPage = memPage,
                        memoryVirtual = memVirtual,
                        name = name,
                        pid = pid
                    });
                }
            }
            return ret;
        }

        public int pid;
        public string name;
        public string exePath;
        public string commandLine;
        public long memory;
        public long memoryVirtual;
        public long memoryPage;
        public string createDate;
        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3} {4} {5} {6}",
                pid.ToString(),
                name,
                commandLine,
                memory.ToString("N0"),
                memoryVirtual.ToString("N0"),
                memoryPage.ToString("N0"),
                createDate);
        }
    }
}
