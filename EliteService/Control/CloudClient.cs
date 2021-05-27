using Elite.WebServer.Services;
using EliteService.Api;
using EliteService.DTO;
using EliteService.Service;
using EliteService.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EliteService.Control
{
    public class CloudClient
    {
        private object lockObj = new object();

        private int intervalCount = 0;

        private int heartBeatInterval = 3; //心跳检测时间间隔。

        public void BeginTask()
        {

            Thread thread = new Thread(new ThreadStart(() =>
            {
                DealHeartBeat();
            }))
            {
                IsBackground = true
            };

            thread.Start();
        }

        /// <summary>
        /// 每5秒向服务端发送一次心跳
        /// </summary>
        private void DealHeartBeat()
        {
            while (true)
            {
                try
                {
                    foreach (KeyValuePair<int, Device> item in GlobalData.DeviceList.ToArray())
                    {
                        void method(object revObj) => this.IntervalAction(revObj);
                        ThreadPool.QueueUserWorkItem(method, item.Value);
                    }
                    LoginToCloud();
                    SyncData();

                    Thread.Sleep(this.heartBeatInterval * 1000);
                    intervalCount++;
                }
                catch (Exception ex)
                {
                    LogHelper.GetInstance.Write("CloudClient DealHeartBeat Error:", ex.Message);
                }
            }
        }

        private void SyncData()
        {
            try
            {
                DateTime now = DateTime.Now;

                if ((now.Hour == GlobalData.SyncTime.Hour) && (now.Minute == GlobalData.SyncTime.Minute))
                {
                    try
                    {
                        if (RedisHelper.Exists("sync_data")) return;
                    }
                    catch
                    {
                        return;
                    }

                    Thread mThread = new Thread(new ThreadStart(() =>
                    {
                        SyncData syncData = new SyncData();
                        syncData.SendData();
                        try
                        {
                            RedisHelper.Set("sync_data", "", 10);
                        }
                        catch { }
                    }));
                    mThread.Start();
                }
            }
            catch { }
        }

        private void IntervalAction(object obj)
        {
            IPEndPoint ipPoint = null;

            try
            {
                Device dev = (Device)obj;
                CloudCommand cloudCommand = new CloudCommand(GlobalData.SchoolId, dev.Id);
                byte[] command = cloudCommand.CreateHeartBeatCmd();
                if (!DataValidate.IsIp(GlobalData.CloudServerIp))
                {
                    return;
                }
                if (string.IsNullOrEmpty(RedisHelper.Get<string>("reg_password")))
                {
                    return;
                }
                ipPoint = new IPEndPoint(IPAddress.Parse(GlobalData.CloudServerIp), GlobalData.CloudServerPort);
                GlobalData.mServerSocket.SendTo(command, command.Length, SocketFlags.None, ipPoint);
            }
            catch (Exception ex)
            {
                LogHelper.GetInstance.Write("heartbeat error：", ex.Message);
            }
        }

        public static void LoginToCloud()
        {
            try
            {
                if (RedisHelper.Exists("cloud_server_token")) return;

                string apiName = "api/cloud/tokens";
                IRestResponse response = SyncActions.Request(apiName, RestSharp.Method.POST, new
                {
                    school_id = SyncActions.GetSchoolId(),
                    password = RedisHelper.Get<string>("reg_password")
                });

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    LogHelper.GetInstance.Write("login to cloud server error：", response.Content);
                    return;
                }



                JObject obj = JsonConvert.DeserializeObject(response.Content) as JObject;
                RedisHelper.Set("cloud_server_token", obj["data"]["token"].ToString());
            }
            catch (Exception ex)
            {
                LogHelper.GetInstance.Write("login to cloud error：", ex.Message);
            }

        }
    }


}
