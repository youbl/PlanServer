using System;
using System.Configuration;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PlanServerService
{
    public static class SocketCommon
    {
        private static bool IsDebug;
        /// <summary>
        /// 传输文件时，临时存放的文件全路径
        /// </summary>
        public static string TmpDir;
        static SocketCommon()
        {
            string tmp = ConfigurationManager.AppSettings["IsDebug"];
            if (!string.IsNullOrEmpty(tmp) && (tmp == "1" || tmp.Equals("true", StringComparison.OrdinalIgnoreCase)))
                IsDebug = true;
            else
                IsDebug = false;

            tmp = ConfigurationManager.AppSettings["PlanWritePath"];
            if (!string.IsNullOrEmpty(tmp))
                TmpDir = tmp;
            else
                TmpDir = @"E:\upload\planserver\downtmp";
            if (!Directory.Exists(TmpDir))
                Directory.CreateDirectory(TmpDir);
        }

        public static void SendData(Socket socket, string msg)
        {
            // 转换为base64后发送，避免中文传输过程中的乱码问题
            msg = Convert.ToBase64String(Encoding.UTF8.GetBytes(msg));

            DateTime begin = DateTime.Now;
            // 发信息 给服务器
            byte[] datasend = Encoding.UTF8.GetBytes(msg);
            int len = datasend.Length;
            // 返回长度为 4 的字节数组
            byte[] datalen = BitConverter.GetBytes(len);

            // 前4个字节作为信息长度
            SendPart(socket, datalen);
            // 长度发完，再发数据
            SendPart(socket, datasend);

            if (IsDebug)
            {
                TaskService.Output(begin.ToString("HH:mm:ss.fff") + 
                    " 发送信息长度 " + len + "\r\n" + msg, "socketDetail");
            }
        }

        /// <summary>
        /// 从Socket接收消息，如果有接收到文件时，transFilePath是保存的文件路径
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="transFilePath"></param>
        /// <returns></returns>
        public static string RecieveData(Socket socket, out string transFilePath)
        {
            transFilePath = null;

            DateTime begin = DateTime.Now;
            int len = 1024;
            byte[] bytesReceived = new byte[len];

            StringBuilder sbRet = new StringBuilder();

            byte[] datalen = new byte[4];
            // 先接收4个字节,这4个字节标识数据的长度
            int recieved = socket.Receive(datalen, 0, datalen.Length, SocketFlags.None);
            if (recieved != datalen.Length)
            {
                return "";// 长度接收错误
            }

            int intDataLen = BitConverter.ToInt32(datalen, 0);

            int recievedLen = 0;
            int leftLen = intDataLen;
            while (recievedLen < intDataLen)
            {
                // 避免多接收数据
                int currentLen = leftLen < len ? leftLen : len;
                int tmp = socket.Receive(bytesReceived, currentLen, 0);
                if (tmp <= 0)
                {
                    TaskService.Output(begin.ToString("HH:mm:ss.fff") +
                    " 收到的base64信息长度" + intDataLen + " 实际字节长度" + recievedLen.ToString(), "socketDetail");
                    return "";//获取的数据与长度不一致
                }
                string data = Encoding.UTF8.GetString(bytesReceived, 0, tmp);
                sbRet.Append(data);
                recievedLen += tmp;
                leftLen -= tmp;
            }
            string ret = sbRet.ToString();

            // 发送过来的是base64后的字符串，避免中文传输过程中的乱码问题
            ret = Encoding.UTF8.GetString(Convert.FromBase64String(ret));
            
            if (IsDebug)
            {
                TaskService.Output(begin.ToString("HH:mm:ss.fff") +
                    " 收到的base64信息长度" + intDataLen + " 实际字节长度" + recievedLen.ToString() + 
                    " 实际字符串长度" + ret.Length + "\r\n" + ret, "socketDetail");
            }

            //bool havData = socket.Poll(1, SelectMode.SelectRead);
            // 休眠20毫秒，因为有时计算机速度太快，导致没及时接收到文件数据响应，Available为0了
            Thread.Sleep(TimeSpan.FromMilliseconds(20));
            int leftByteNum = socket.Available;
            if (leftByteNum > 0)
            {
                // 尝试接收文件
                string tmpFile = Path.Combine(TmpDir, DateTime.Now.ToString("yyyyMMddHHmmssfff"));
                if (ReceiveFile(socket, tmpFile))
                {
                    transFilePath = tmpFile;
                }
            }
            //else
            //{
            //    transFilePath = "qq" + avag.ToString();
            //}

            return ret;
        }


        /// <summary>
        /// 往Socket发送文件
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="filePath"></param>
        public static void SendFile(Socket socket, string filePath)
        {
            //if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            //    return;

            DateTime begin = DateTime.Now;
            long fileLen;
            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fileLen = file.Length;
                // 返回长度为 8 的字节数组
                byte[] datalen = BitConverter.GetBytes(fileLen);
                // 前8个字节作为信息长度
                SendPart(socket, datalen);

                int perLen;
                byte[] datasend = new byte[1024];
                while ((perLen = file.Read(datasend, 0, datasend.Length)) > 0)
                {
                    int partLen = SendPart(socket, datasend, perLen);
                    if (partLen != perLen)
                    {
                        // 发送长度不一致时，记录异常
                        TaskService.Output(begin.ToString("HH:mm:ss.fff") +
                                           " 实际发送" + partLen.ToString() + " 应发长度" + perLen.ToString() + "\r\n" +
                                           filePath, default(Exception));
                    }
                }
            }

            if (IsDebug)
            {
                TaskService.Output(begin.ToString("HH:mm:ss.fff") +
                    " 发送文件长度 " + fileLen.ToString() + "\r\n" + filePath, "socketDetail");
            }
        }

        /// <summary>
        /// 反复发送数据，直到全部发送完毕,返回发送完毕的字节数
        /// </summary>
        /// <param name="s"></param>
        /// <param name="data"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        static int SendPart(Socket s, byte[] data, int size = -1)
        {
            if (size == -1)
                size = data.Length;

            int total = 0;
            int dataleft = size;

            while (total < size)
            {
                // 在非阻止模式下，Send 可能会成功完成，即使它发送的字节数比您请求的字节数少
                int sent = s.Send(data, total, dataleft, SocketFlags.None);
                total += sent;
                dataleft -= sent;
            }

            return total;
        }

        /// <summary>
        /// 从Socket接收文件，并保存到指定路径
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="filePath"></param>
        public static bool ReceiveFile(Socket socket, string filePath)
        {
            byte[] datalen = new byte[8];
            // 先接收8个字节,这8个字节标识文件数据的长度
            int recieved = socket.Receive(datalen, 0, datalen.Length, SocketFlags.None);
            if (recieved != datalen.Length)
            {
                return false;// 长度接收错误
            }

            int intDataLen = BitConverter.ToInt32(datalen, 0);
            
            int total = 0;
            int recv;

            byte[] data = new byte[1024];
            using (FileStream file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                while (total < intDataLen)
                {
                    recv = socket.Receive(data, 0, data.Length, SocketFlags.None);
                    if (recv == 0)
                    {
                        return false;// 未接收完成
                    }
                    total += recv;
                    file.Write(data, 0, recv);
                }
            }
            return true;
        }  
    }
}
