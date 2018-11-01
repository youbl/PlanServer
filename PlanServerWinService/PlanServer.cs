using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using PlanServerService;

namespace PlanServerWinService
{
    public partial class PlanServer : ServiceBase
    {
        public PlanServer()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            string msg = string.Format("启动目录:{0}\r\n启动文件:{1}\r\n程序启动……",
                AppDomain.CurrentDomain.BaseDirectory,
                Process.GetCurrentProcess().MainModule.FileName);
            Utils.Output(msg);


            // OnStart方法不允许有死循环，否则启动服务时会报错，改用Thread进行死循环
            new Thread(TaskAutoRunService.Run) { IsBackground = true }.Start();

            if (Common.GetBoolean("enableSocketListen"))
            {
                // 端口监听，处理管理程序的进程
                var method = new SocketServer.OperationDelegate(TaskService.ServerOperation);
                new Thread(SocketServer.ListeningBySocket) { IsBackground = true }.Start(method);
                msg = " 开始监听端口：" + TaskService.ListenPort;
                Utils.Output(msg);
            }
        }

        protected override void OnStop()
        {
        }
    }
}
