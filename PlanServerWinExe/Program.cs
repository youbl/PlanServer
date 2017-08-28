using System;
using System.Diagnostics;
using System.Threading;
using PlanServerService;

namespace PlanServer
{
    class Program
    {
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Process process = Process.GetCurrentProcess();
            string msg = string.Format("启动目录:{0}\r\n启动文件:{1}\r\n\r\n 程序启动……",
                AppDomain.CurrentDomain.BaseDirectory,
                process.MainModule.FileName);

            if (IsRunning(process))
            {
                msg += "\r\n应用程序已经在运行中。";
                LogHelper.WriteCustom(msg, @"start\", false);
                Thread.Sleep(1000);
                Environment.Exit(1);
            }

            Console.WriteLine(msg);
            LogHelper.WriteCustom(msg, @"start\", false);

            // 轮询数据库，处理任务的线程
            new Thread(TaskService.Run) { IsBackground = true }.Start();

            if (Common.GetBoolean("enableSocketListen"))
            {
                // 端口监听，处理管理程序的进程
                var method = new SocketServer.OperationDelegate(TaskService.ServerOperation);
                new Thread(SocketServer.ListeningBySocket).Start(method);

                msg = " 开始监听端口：" + TaskService.ListenPort;
                Console.WriteLine(msg);
                LogHelper.WriteCustom(msg, @"start\", false);
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            TaskService.Output("未知错误", ex);
        }

        /// <summary>
        /// 根据指定进程名和文件路径，判断程序是否已经在运行中。
        /// 通常用于程序单例运行，避免启动多个实例
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        public static bool IsRunning(Process current)
        {
            Process[] processes = Process.GetProcessesByName(current.ProcessName);
            foreach (Process process in processes)
            {
                if (process.Id != current.Id &&
                    process.MainModule.FileName.Equals(current.MainModule.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

    }
}
