using Elite.WebServer.Base;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Elite.WebServer.Utility
{
    public class UdpHelper
    {
        public static JsonMsg<byte[]> SendCommand(byte[] sendData, IPEndPoint serverPoint, Socket clientSocket = null, int serialId = 0)
        {
            byte[] returns;
            JsonMsg<byte[]> msg;
            int i = 0;
            int recvSerialId = 0;

            do
            {

                LogHelper.GetInstance.Write("local webserver send to :" + serverPoint.ToString(), sendData);
                returns = SendCommandOnce(sendData, serverPoint, clientSocket);
                LogHelper.GetInstance.Write("local webserver received from :" + serverPoint.ToString(), returns);

                msg = BaseController.CheckResultWithData(returns);
                if (msg.code == 200)
                {
                    if (serialId != 0)
                    {
                        if (msg.data.Length > 6)
                        {
                            recvSerialId = BitConverter.ToUInt16(msg.data, 6);
                            if (recvSerialId == serialId) break;
                        }
                    }
                    else
                    {
                        break;
                    }

                }
                else if (msg.code != 501)
                {
                    break;
                }
            }
            while (++i < 3);
            return msg;
        }

        public static byte[] SendCommandOnce(byte[] sendData, IPEndPoint serverPoint, Socket clientSocket = null)
        {
            bool requestOnce = false;
            if (clientSocket is null)
            {
                requestOnce = true;
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                clientSocket.ReceiveTimeout = 15000;
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
                if (requestOnce) clientSocket.Close();
                return actualData;
            }
            catch(Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString("HH:MM:ss") + ex.Message);
                return new byte[] { 100 };
            }
        }
    }
}
