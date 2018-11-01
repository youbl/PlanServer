using System;
using System.Text;

namespace PlanServerService
{
    public static class Utils
    {
        public static void Output(StringBuilder msg, string suffix = null)
        {
            Output(msg.ToString(), suffix);
        }
        public static void Output(string msg, string suffix = null)
        {
            suffix = suffix ?? "run";
            string day = DateTime.Now.ToString("yyyyMMdd");
            LogHelper.WriteCustom(msg, day + "\\" + suffix, false);
            //Console.WriteLine(msg);
        }
        public static void Output(string msg, Exception exp)
        {
            msg += Environment.NewLine + exp;
            LogHelper.WriteCustom(msg, "exception\\", "err", false);
            //Console.WriteLine(msg);
        }
    }
    
}
