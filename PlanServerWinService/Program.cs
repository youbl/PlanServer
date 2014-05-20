using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using PlanServerService;

namespace PlanServerWinService
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Process process = Process.GetCurrentProcess();
            string msg = string.Format("启动目录:{0}\r\n启动文件:{1}\r\n\r\n 程序启动……",
                AppDomain.CurrentDomain.BaseDirectory,
                process.MainModule.FileName);
            //Console.WriteLine(msg);
            LogHelper.WriteCustom(msg, @"start\", false);

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
			{ 
				new PlanServer() 
			};
            ServiceBase.Run(ServicesToRun);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            TaskService.Output("未知错误，程序退出", ex);
        }
    }
}
