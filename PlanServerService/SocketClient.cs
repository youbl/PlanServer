using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace PlanServerService
{
    public static class SocketClient
    {
        // 客户端方法，往指定ip发送消息,有接收到文件时，recievedFile就是文件路径
        public static string SendBySocket(string ip, int port, string msgs, ref string recievedFile)
        {
            try
            {
                using (Socket socket = ConnectSocket(ip, port))
                {
                    SocketCommon.SendData(socket, msgs);

                    if (!string.IsNullOrEmpty(recievedFile) && File.Exists(recievedFile))
                        SocketCommon.SendFile(socket, recievedFile);

                    // 接收 服务器返回的信息
                    socket.Blocking = true; // 在socket的Receive方法前必须明确指明其为阻塞模式
                    //ClientSocket.ReceiveTimeout = 5000;

                    string ret = SocketCommon.RecieveData(socket, out recievedFile);
                    return ret;
                }
            }
            catch (Exception exp)
            {
                return "err" + exp;
            }
        }

        private static Socket ConnectSocket(string ip, int port)
        {
            if (string.IsNullOrEmpty(ip))
            {
                ip = "127.0.0.1";
            }
            IPEndPoint serverInfo = new IPEndPoint(IPAddress.Parse(ip), port);
            Socket ClientSocket = new Socket(serverInfo.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            ClientSocket.Connect(serverInfo);
            if (ClientSocket.Connected)
                return ClientSocket;
            return null;
        }

    }
}
