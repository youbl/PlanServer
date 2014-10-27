using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace PlanServerRestart
{
    class Program
    {
        #region 常量与属性
        private const string RUN_ARG = "123";

        private static string SourcePath = ConfigurationManager.AppSettings["SourcePath"];
        private static string TargetPath = ConfigurationManager.AppSettings["TargetPath"];

        private static bool CopySQLite;
        #endregion

        static void Main(string[] arg)
        {
            string tmp = ConfigurationManager.AppSettings["CopySQLite"];
            if (!string.IsNullOrEmpty(tmp) && tmp.Equals("true", StringComparison.OrdinalIgnoreCase))
                CopySQLite = true;

            //string ss = CopyFile();
            //return;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            string msg = string.Format("启动目录:{0}\r\n启动文件:{1}\r\n程序启动……\r\n",
              AppDomain.CurrentDomain.BaseDirectory,
              Process.GetCurrentProcess().MainModule.FileName);

            if (arg != null && arg.Length > 0 && !string.IsNullOrEmpty(arg[0]))
            {
                switch (arg[0])
                {
                    case RUN_ARG:
                        WriteLog(msg + "开始停止计划任务服务");
                        OperationService(true);
                        WriteLog("停止计划任务服务完成，等待1分钟再复制文件");
                        Thread.Sleep(TimeSpan.FromMinutes(1));

                        WriteLog("开始复制文件");
                        string error = CopyFile();
                        if (!error.StartsWith("OK", StringComparison.Ordinal))
                        {
                            WriteLog("复制文件失败:" + error);
                        }
                        else
                        {
                            WriteLog("复制文件完成:" + error);
                        }
                        WriteLog("开始启动计划任务服务");
                        OperationService(false);
                        WriteLog("启动计划任务服务完成，程序退出");
                        break;
                    
                    default:
                        WriteLog(msg + "参数无效" + arg[0] + "，请添加参数" + RUN_ARG);
                        break;
                }
            }
            else
            {
                WriteLog(msg + "没有参数，请添加参数" + RUN_ARG);
            }
        }

        static void OperationService(bool isstop)
        {
            string command;
            if (isstop)
                command = "net stop planserver";
            else
                command = "net start planserver";

            using (var p = new Process())
            {
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = true; // 在当前进程中启动，不使用系统外壳程序启动
                //p.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;// 让dos窗体最大化
                p.StartInfo.Arguments = "/C " + command; //设定参数，其中的“/C”表示执行完命令后马上退出
                p.StartInfo.RedirectStandardInput = false; //设置为true，后面可以通过StandardInput输入dos命令
                p.StartInfo.RedirectStandardOutput = false;
                //p.StartInfo.CreateNoWindow = true;     //不创建窗口
                p.Start();
                //SetWindowPos(p.Handle, 3, Left, Top, Width, Height, 8);
                //p.StandardInput.WriteLine("ping " + url);
                p.WaitForExit(1000);
                //MessageBox.Show(p.StandardOutput.ReadToEnd());
                p.Close();
            }
        }

        static string CopyFile()
        {
            string target = TargetPath;
            if (string.IsNullOrEmpty(target))
            {
                return "未配置目标目录";
            }
            if (!Directory.Exists(target))
            {
                return "目标目录不存在:" + target;
            }

            string source = SourcePath;
            if (string.IsNullOrEmpty(source))
            {
                source = AppDomain.CurrentDomain.BaseDirectory;
            }
            if (!Directory.Exists(source))
            {
                return "源目录不存在:" + source;
            }
            int fileCnt = 0;
            StringBuilder sb = new StringBuilder();
            foreach (string file in Directory.GetFiles(source))
            {
                // 不复制Sqlite的dll，避免32位问题
                if (!CopySQLite && file.EndsWith("SQLite.dll", StringComparison.OrdinalIgnoreCase))
                    continue;

                string tfile = Path.Combine(target, Path.GetFileName(file) ?? "");
                try
                {
                    File.Copy(file, tfile, true);
                    fileCnt++;
                    sb.AppendFormat("{0}=>{1}\r\n", file, tfile);
                }
                catch (Exception exp)
                {
                    return file + "=>" + tfile + "\r\n" + exp;
                }
            }
            return "OK copy files:" + fileCnt.ToString() + "\r\n" + sb;
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            WriteLog("未知错误:\r\n" + ex);
        }

        static void WriteLog(string msg)
        {
            string dir = @"E:\weblogs\PlanServerMain\StopStartService";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            using (var sw = new StreamWriter(dir + @"\log.txt", true, Encoding.UTF8))
            {
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                sw.WriteLine(msg);
                sw.WriteLine();
            }
        }
    }
}
