using System;
using System.Threading;
using PlanServerService;

namespace PlanServerTaskManager.Web
{
    /// <summary>
    /// 垃圾回收通知
    /// </summary>
    public class GCNotification
    {
        // Variable for continual checking in the 
        // While loop in the WaitForFullGCProc method.
        static bool checkForNotify;

        // Variable for ending the example.
        static bool finalExit = false;

        /// <summary>
        /// 注册通知
        /// </summary>
        public static void Register()
        {
            try
            {
                // Register for a notification. 
                // 注: gcServer + Interactive模式下, GC回收通知会有部分丢失
                GC.RegisterForFullGCNotification(10, 10);
                WriteLog("注册垃圾回收通知"
                         + "\r\n gcServer enabled=" + System.Runtime.GCSettings.IsServerGC.ToString()
                         + "\r\n GCLatencyMode=" + System.Runtime.GCSettings.LatencyMode.ToString());

                checkForNotify = true;

                // Start a thread using WaitForFullGCProc.
                Thread thWaitForFullGC = new Thread(WaitForFullGCProc) { IsBackground = true };
                thWaitForFullGC.Start();
            }
            catch (InvalidOperationException invalidOp)
            {
                WriteLog("当前环境不支持垃圾回收通知:"
                         + invalidOp.Message);
            }
        }

        /// <summary>
        /// 取消通知
        /// </summary>
        public static void Cancel()
        {
            finalExit = true;
            GC.CancelFullGCNotification();
        }

        /// <summary>
        /// 等待完整的GC
        /// </summary>
        private static void WaitForFullGCProc()
        {
            while (true)
            {
                try
                {
                    // CheckForNotify is set to true and false in Main.
                    while (checkForNotify)
                    {
                        // Check for a notification of an approaching collection.
                        GCNotificationStatus s = GC.WaitForFullGCApproach();
                        if (s == GCNotificationStatus.Succeeded)
                        {
                            WriteLog("GC 即将开始");
                            OnFullGCApproachNotify();
                        }
                        else if (s == GCNotificationStatus.Canceled)
                        {
                            WriteLog("GC 即将开始 -> 取消");
                            break;
                        }
                        else
                        {
                            // This can occur if a timeout period
                            // is specified for WaitForFullGCApproach(Timeout) 
                            // or WaitForFullGCComplete(Timeout)  
                            // and the time out period has elapsed. 
                            WriteLog("GC 即将开始 -> 超时");
                            break;
                        }

                        // Check for a notification of a completed collection.
                        s = GC.WaitForFullGCComplete();
                        if (s == GCNotificationStatus.Succeeded)
                        {
                            WriteLog("GC 完成");
                            OnFullGCCompleteEndNotify();
                        }
                        else if (s == GCNotificationStatus.Canceled)
                        {
                            WriteLog("GC 完成 -> 取消");
                            break;
                        }
                        else
                        {
                            // Could be a time out.
                            WriteLog("GC 完成 -> 超时");
                            break;
                        }
                    }
                }
                catch (Exception exp)
                {
                    WriteLog("GC 检查异常 -> " + exp);
                }

                Thread.Sleep(500);
                // FinalExit is set to true right before  
                // the main thread cancelled notification.
                if (finalExit)
                {
                    break;
                }
            }

        }

        /// <summary>
        /// 即将发生GC前的处理
        /// </summary>
        private static void OnFullGCApproachNotify()
        {
            //WriteLog("Redirecting requests.");

            // Method that tells the request queuing  
            // server to not direct requests to this server. 
            //RedirectRequests();

            // Method that provides time to 
            // finish processing pending requests. 
            //FinishExistingRequests();

            // This is a good time to induce a GC collection
            // because the runtime will induce a full GC soon.
            // To be very careful, you can check precede with a
            // check of the GC.GCCollectionCount to make sure
            // a full GC did not already occur since last notified.
            //GC.Collect();
            //WriteLog("Induced a collection.");
        }

        /// <summary>
        /// 已完成GC后的处理
        /// </summary>
        private static void OnFullGCCompleteEndNotify()
        {
            // Method that informs the request queuing server
            // that this server is ready to accept requests again.
            //AcceptRequests();
            //WriteLog("Accepting requests again.");
        }

        private static void WriteLog(string msg)
        {
            LogHelper.WriteCustom(msg, "GCNotification\\", false);
        }
    }
}
