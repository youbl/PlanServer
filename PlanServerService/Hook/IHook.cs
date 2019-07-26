using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using NLog;
using System.Text;
using System.Web;

namespace PlanServerService.Hook
{
    /// <summary>
    /// 需要发送通知的接口
    /// </summary>
    public interface IHook
    {
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        void Hook(string message);
    }
}
