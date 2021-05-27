using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EliteService.Control
{


    public class RtpMonitor
    {
        private Thread mThread;
        private Socket mServerSocket;

        public void StartServer()
        {
            Console.WriteLine("Rtp监听端启动");

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
            IPEndPoint serverIp = new IPEndPoint(IPAddress.Any, GlobalData.ClientAudiolPort);

            mServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mServerSocket.Bind(serverIp);

            while (true)
            {
                try
                {
                    EndPoint clientIp = new IPEndPoint(IPAddress.Any, 0);
                    byte[] revData = new byte[1560];
                    int len = mServerSocket.ReceiveFrom(revData, ref clientIp);

                    void method(object revObj) => this.DealClient(revObj);
                    ThreadPool.QueueUserWorkItem(method, new RevDataForm { RevData = revData, ClientIp = clientIp, RevLength = len });

                    Thread.Sleep(10);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public void DealClient(object obj)
        {
            try
            {
                RevDataForm revDataForm = (RevDataForm)obj;
                Console.WriteLine("监听包来了...");
            }
            catch (Exception)
            {

            }
        }
    }


}
