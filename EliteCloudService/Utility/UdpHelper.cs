using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EliteService.Utility
{
    public class UdpHelper
    {
        public static byte[] SendCommand(byte[] sendData, EndPoint serverPoint, Socket clientSocket = null)
        {
            if (clientSocket is null)
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                clientSocket.ReceiveTimeout = 1000;
            }

            if (GlobalData.IsDebug)
            {
                LogHelper.GetInstance.Write("云平台服务 send to：" + serverPoint.ToString(), sendData);
            }

            clientSocket.SendTo(sendData, sendData.Length, SocketFlags.None, serverPoint);
            Thread.Sleep(2);

            EndPoint returnPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] getData = new byte[1500];
            try
            {
                int recvLen = clientSocket.ReceiveFrom(getData, ref returnPoint);
                byte[] actualData = new byte[recvLen];
                Array.Copy(getData, 0, actualData, 0, recvLen);
                clientSocket.Close();
                if (GlobalData.IsDebug)
                {
                    LogHelper.GetInstance.Write("云平台服务 received from：" + serverPoint.ToString(), actualData);
                }
                return actualData;
            }
            catch
            {
                return new byte[] { 100 };
            }
        }
    }
}
