using EliteService.Control;
using EliteService.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;

namespace EliteService.Audio
{
    public class MonitorServer
    {
        private Thread mThread;
        private int udpPort = 32000;

        public void StartServer()
        {
            Console.WriteLine("监听服务启动");
            LogHelper.GetInstance.Write("监听服务已启动！", "");
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
            CommandActions actions = new CommandActions();

            int port;

            int channel = 1;


            while (true)
            {

                var keys = new List<int>(GlobalData.DeviceList.Keys);//将 Key 值全部取出并放入 List 中便于遍历
                for (int i = 0; i < keys.Count; i++)
                {
                    try
                    {
                        var key = keys[i];
                        if (!GlobalData.RtpFramerList.ContainsKey(key))
                        {

                            if (GlobalData.DeviceList.ContainsKey(key))
                            {

                                if (GlobalData.DeviceList[key].IsAutoRecord == 1)
                                {
                                    port = GetPort();
                                    actions.StartDeviceMonitor(key, channel, port);
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                string redisKey = Helper.md5("device_status_" + key.ToString()); //状态存在redis中
                                if (RedisHelper.Exists(redisKey))
                                {
                                    byte[] statusBytes = JsonConvert.DeserializeObject<byte[]>(RedisHelper.Get(redisKey).ToString());
                                    if (statusBytes[57] == 0) //未监听
                                    {
                                        if (GlobalData.DeviceList.ContainsKey(key))
                                        {
                                            if (GlobalData.DeviceList[key].IsAutoRecord == 1)
                                            {
                                                int listen_port = GlobalData.DeviceList[key].ListenPort;
                                                if (listen_port == 0) listen_port = GetPort();
                                                actions.StartDeviceMonitor(key, channel, listen_port);
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.GetInstance.Write("MonitorServer listening error：", ex.Message);
                    }
                }
                //设备列表里不存在的设备，进行删除
                try
                {
                    keys = new List<int>(GlobalData.RtpFramerList.Keys);
                    for (int i = 0; i < keys.Count; i++)
                    {
                        if (!GlobalData.DeviceList.ContainsKey(keys[i]))
                        {
                            GlobalData.RemoveDevice(keys[i]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.GetInstance.Write("MonitorServer listening error2：", ex.Message);
                }


                Thread.Sleep(5000);
            }
        }

        public int GetPort()
        {
            while (!Helper.udpPortIsFree(this.udpPort))
            {
                this.udpPort += 2;
            }
            return this.udpPort;
        }
    }
}
