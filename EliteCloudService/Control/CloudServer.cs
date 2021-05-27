using EliteService.Utility;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EliteService.Control
{


    public class CloudServer
    {
        private Thread mThread;
        private Socket mServerSocket;

        public void StartServer()
        {
            Console.WriteLine("云平台服务端启动");
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
            IPEndPoint serverIp = new IPEndPoint(IPAddress.Any, GlobalData.CloudServerPort);

            mServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mServerSocket.Bind(serverIp);

            while (true)
            {
                try
                {
                    EndPoint clientIp = new IPEndPoint(IPAddress.Any, 0);
                    byte[] revData = new byte[1064];
                    int len = mServerSocket.ReceiveFrom(revData, ref clientIp);

                    void method(object revObj) => this.DealClient(revObj);
                    ThreadPool.QueueUserWorkItem(method, new RevDataForm { RevData = revData, ClientIp = clientIp, RevLength = len });

                    Thread.Sleep(10);
                }
                catch (Exception ex)
                {
                    LogHelper.GetInstance.Write("云平台服务 error:", ex.Message);
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
                    LogHelper.GetInstance.Write("云平台服务接收数据 from:" + revObj.ClientIp.ToString(), revData);
                }
                DealRequest dealRequest = new DealRequest();

                dealRequest.Processing(mServerSocket, revData, revObj.ClientIp);
            }
            catch (Exception ex)
            {
                if (GlobalData.IsDebug)
                {
                    LogHelper.GetInstance.Write("云平台服务 error:", ex.Message);
                }
                ReturnMsg.GetReturn(new DTO.JsonMsg { code = 500, message = ex.Message, data = new byte[] { } });
                //mServerSocket.SendTo(result, result.Length, SocketFlags.None, revObj.clientIp); 启用该行将导致互相收发死循环。
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
