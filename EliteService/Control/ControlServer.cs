using EliteService.Utility;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EliteService.Control
{


    public class ControlServer
    {
        private Thread mThread;

        public void StartServer()
        {
            Console.WriteLine("客户端服务启动");
            LogHelper.GetInstance.Write("StartServer begin", "客户端服务启动");
            ThreadPool.SetMaxThreads(GlobalData.MaxWorkThread, GlobalData.MaxIoThread);
            ThreadPool.SetMinThreads(GlobalData.MinWorkThread, GlobalData.MinIoThread);

            mThread = new Thread(new ThreadStart(() =>
            {
                this.StartListening();
            }))
            {
                IsBackground = true
            };

            mThread.Start();
        }

        public void StartListening()
        {
            IPEndPoint serverIp = new IPEndPoint(IPAddress.Any, GlobalData.ControlPort);

            try
            {
                GlobalData.mServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                GlobalData.mServerSocket.Bind(serverIp);
            }
            catch (Exception ex)
            {
                LogHelper.GetInstance.Write("StartServer failed", ex.Message + GlobalData.ControlPort.ToString());
            }

            while (true)
            {
                try
                {
                    EndPoint clientIp = new IPEndPoint(IPAddress.Any, 0);
                    byte[] revData = new byte[1067];

                    int len = GlobalData.mServerSocket.ReceiveFrom(revData, ref clientIp);

                    void method(object revObj) => this.DealClient(revObj);
                    ThreadPool.QueueUserWorkItem(method, new RevDataForm { RevData = revData, ClientIp = clientIp, RevLength = len });

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public void DealClient(object obj)
        {

            RevDataForm revObj = (RevDataForm)obj;
            try
            {
                byte[] revData = new byte[revObj.RevLength];
                Array.Copy(revObj.RevData, 0, revData, 0, revObj.RevLength);
                if (GlobalData.IsDebug)
                {
                    LogHelper.GetInstance.Write("本地平台 receive from " + revObj.ClientIp.ToString(), revData);
                }
                DealRequest dealRequest = new DealRequest();

                byte[] result = dealRequest.Processing(revData);
                if (GlobalData.IsDebug)
                {
                    LogHelper.GetInstance.Write("本地平台 send to " + revObj.ClientIp.ToString(), result);
                }
                GlobalData.mServerSocket.SendTo(result, result.Length, SocketFlags.None, revObj.ClientIp);
            }
            catch (Exception ex)
            {
                byte[] result = ReturnMsg.GetReturn(new DTO.JsonMsg { code = 500, message = ex.Message, data = new byte[] { } });
                LogHelper.GetInstance.Write("本地平台 error dealClient:" + revObj.ClientIp.ToString(), ex.Message);
                LogHelper.GetInstance.Write("本地平台 error dealClient:" + revObj.ClientIp.ToString(), result);
                GlobalData.mServerSocket.SendTo(result, result.Length, SocketFlags.None, revObj.ClientIp);
            }
        }
    }

    public class RevDataForm
    {
        public byte[] RevData { get; set; }
        public int RevLength { get; set; }
        public EndPoint ClientIp { get; set; }

    }



}
