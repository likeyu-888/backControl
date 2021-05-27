using System;
using System.Net;
using System.Net.Sockets;

namespace EliteService.Utility
{
    public class UdpHelper
    {
        public static byte[] SendCommand(byte[] sendData, IPEndPoint serverPoint, Socket clientSocket = null)
        {
            bool requestOnce = false;

            if (clientSocket is null)
            {
                requestOnce = true;
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                clientSocket.ReceiveTimeout = 1000;
            }

            if (GlobalData.IsDebug)
            {
                LogHelper.GetInstance.Write("send to device：" + serverPoint.ToString(), sendData);
            }

            clientSocket.SendTo(sendData, sendData.Length, SocketFlags.None, serverPoint);

            EndPoint returnPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] getData = new byte[1024];
            try
            {
                int recvLen = clientSocket.ReceiveFrom(getData, ref returnPoint);
                byte[] actualData = new byte[recvLen];
                Array.Copy(getData, 0, actualData, 0, recvLen);
                if (requestOnce) clientSocket.Close();
                if (GlobalData.IsDebug)
                {
                    LogHelper.GetInstance.Write("receive from device：" + serverPoint.ToString(), actualData);
                }
                return actualData;
            }
            catch(Exception ex)
            {
                return new byte[] { 100 };
            }
        }
    }
}
