using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace PlanServerService
{
    public static class SocketServer
    {
        private static Socket ListenSocket;

        /// <summary>
        /// 处理消息的方法委托
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="filePath">用于接收或发送文件</param>
        /// <returns></returns>
        public delegate string OperationDelegate(string msg, ref string filePath);

        private static OperationDelegate Method;

        // 主调方法：监听端口，处理端口请求
        public static void ListeningBySocket(object args)
        {
            OperationDelegate method = (OperationDelegate) args;
            try
            {
                // 监听的代码
                ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint serverInfo = new IPEndPoint(IPAddress.Parse("0.0.0.0"), TaskService.ListenPort);
                ListenSocket.Bind(serverInfo);  //将SOCKET接口和IP端口绑定
                ListenSocket.Listen(10);        //开始监听，并且指定队列中最多可容纳的等待接受的传入连接数
                TaskService.Output("listening on port " + TaskService.ListenPort, "socket");

                Method = method;

                while (true)
                {
                    try
                    {
                        Socket socket = ListenSocket.Accept(); // 接受一个客户端

                        TaskService.Output(socket.RemoteEndPoint + " 成功连接服务器.", "socketDetail");
                        ThreadPool.UnsafeQueueUserWorkItem(RecieveAccept, socket);
                    }
                    catch (Exception ex)
                    {
                        TaskService.Output("err listening: ", ex);
                    }
                }
            }
            catch (System.Security.SecurityException exp)
            {
                TaskService.Output("err SecurityException: ", exp);
            }
            catch (Exception exp)
            {
                TaskService.Output("err: ", exp);
            }
        }

        private static void RecieveAccept(object args)
        {
            string endpoint = string.Empty;
            Socket socket = null;
            try
            {
                socket = args as Socket;
                if (socket == null)
                {
                    TaskService.Output(" socket为空.", "RecieveAccept");
                    return;
                }
                endpoint = socket.RemoteEndPoint.ToString();
                if (!socket.Connected)
                {
                    TaskService.Output(endpoint + " 未连接.", "RecieveAccept");
                    return;
                }

                // 接受数据等待时间，超过时，断开连接
                socket.ReceiveTimeout = (int)TimeSpan.FromSeconds(TaskService.WaitClientSecond).TotalMilliseconds;

                // 接收客户端发来的数据
                string recieveFilePath;
                string recieveMsg = SocketCommon.RecieveData(socket, out recieveFilePath);

                // 是否有接收到文件，有接收到时，不再发送（目前没有先接收再发送文件的操作，用这个进行标识）
                bool haveRecieveFile = !string.IsNullOrEmpty(recieveFilePath);

                // 给客户端返回消息
                string response;
                if (Method != null)
                {
                    try
                    {
                        response = Method(recieveMsg, ref recieveFilePath);
                    }
                    catch (Exception exp)
                    {
                        response = "err:" + exp;
                    }
                }
                else
                {
                    response = "err:未设定处理函数";
                }

                TaskService.Output(endpoint + "\r\n***接收***:"+ recieveFilePath + "\r\n" + recieveMsg + 
                    "\r\n***响应***:\r\n" + response, "socketResponse");
                if (socket.Connected)
                {
                    SocketCommon.SendData(socket, response);
                    // 没接收到文件，但是指定了文件路径时，要发送文件
                    if (!haveRecieveFile && !string.IsNullOrEmpty(recieveFilePath) && File.Exists(recieveFilePath))
                        SocketCommon.SendFile(socket, recieveFilePath);
                }
            }
            catch (SocketException exp)
            {
                if (exp.ErrorCode == 10060)
                {
                    // ReceiveTimeout到时了
                    //return;
                }
                LogHelper.WriteException("err监听出错:", exp);
                //return;
            }
            catch (Exception exp)
            {
                LogHelper.WriteException("err监听出错:", exp);
                //return;
            }
            finally
            {
                if(socket != null)
                    socket.Close();
                TaskService.Output(endpoint + " 成功断开服务器.", "socketDetail");
            }
        }
    }
}
