using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace PlanServerRestart
{
    class Program
    {
        private const string RUN_ARG = "123";
        static void Main(string[] arg)
        {
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
                        WriteLog("停止计划任务服务完成，等待5分钟");
                        Thread.Sleep(TimeSpan.FromMinutes(5));
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
