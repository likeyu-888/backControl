using Elite.WebServer.Services;
using EliteService.Api;
using EliteService.Audio;
using EliteService.Control;
using EliteService.DTO;
using EliteService.Utility;
using MySql.Data.MySqlClient;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EliteService.Service
{
    class Upgrade
    {
        private JsonMsg msg = new JsonMsg { code = 200 };
        private byte[] fileData;
        private int deviceId;
        private bool isArm;
        private byte[] command;
        private DeviceCommand devCommand = new DeviceCommand();
        private CommandActions comActions;
        private bool report = true;
        private int reportTimes = 0;

        private bool isAbort = false;

        UdpClient udpClient;
        Thread receivedThread;
        private int upgradePort;
        IPEndPoint deviceEndPoint;
        private int receivedSerialId = 0;
        private bool isProcessing = false;
        private bool fromCloud = false;

        private int logId = 0;

        private int lastSerialId = 0;
        private bool needSendAgain = false;

        public JsonMsg StartUpgrade(bool fromCloud, IPEndPoint deviceEndPoint, int deviceId, bool isArm, string redisKey, int logId = 0)
        {
            try
            {
                this.needSendAgain = false;
                this.fromCloud = fromCloud;
                this.isArm = isArm;
                this.deviceId = deviceId;
                this.deviceEndPoint = deviceEndPoint;
                this.logId = logId;

                comActions = new CommandActions(deviceEndPoint, deviceId);

                if (fromCloud)
                {
                    DownloadFile(redisKey);
                }
                else
                {
                    GetUploadFile(redisKey);
                }

                if (msg.code != 200) return this.msg;

                ExecuteUpgrade();
                if (msg.code != 200) return this.msg;

                return new JsonMsg { code = 200, message = "操作成功" };
            }
            catch (Exception)
            {
                return new JsonMsg { code = 500, message = "升级错误" };
            }
        }

        private void GetUploadFile(string fileName)
        {

            string filePath = GlobalData.ServiceRoot + (@"/upgrades/" + fileName);
            if (!File.Exists(filePath))
            {
                this.msg = new JsonMsg { code = 500, message = "升级文件不存在" };
            }

            FileStream fs = new FileStream(filePath, FileMode.Open);

            //获取文件大小
            long size = fs.Length;

            this.fileData = new byte[size];

            //将文件读到内byte数组中
            fs.Read(this.fileData, 0, this.fileData.Length);

            fs.Close();
        }

        private void ReportUpgradeThread()
        {
            string stopKey = Helper.md5("device_abort_upgrade_" + this.deviceId.ToString());

            while (report && (!isAbort))
            {
                ReportUpgradeRate();
                Thread.Sleep(1000);
                reportTimes++;
                if (reportTimes >= 60 * 20)
                {
                    break;
                }

                if (lastSerialId == receivedSerialId)
                {
                    needSendAgain = true;
                }

                try
                {
                    if (RedisHelper.Exists(stopKey))
                    { //控制是否停止
                        RedisHelper.Remove(stopKey);
                        isProcessing = false;
                        isAbort = true;
                        string redisKey = Helper.md5("device_upgrade_rate_" + this.deviceId.ToString());
                        RedisHelper.Remove(redisKey);

                        string key = Helper.md5("device_upgrade_" + this.deviceId.ToString());
                        RedisHelper.Remove(key);
                    }
                }
                catch { }
            }
        }

        private void DeleteFinishThread()
        {
            Thread.Sleep(40000); //40秒后删除
            string redisKey = Helper.md5("device_upgrade_rate_" + deviceId.ToString());
            try
            {
                if (RedisHelper.Exists(redisKey)) RedisHelper.Remove(redisKey);
            }
            catch { }
        }

        private void ReportUpgradeRate(string rate = "0")
        {
            try
            {
                string apiName = "api/devices/" + SyncActions.GetSchoolId().ToString() + "/" + deviceId.ToString() + "/finishrate";

                string redisKey = Helper.md5("device_upgrade_rate_" + deviceId.ToString());
                if (rate.Equals("0"))
                {
                    if (RedisHelper.Exists(redisKey)) rate = RedisHelper.Get(redisKey).ToString();
                    else rate = "-1";
                }
                if (rate.Equals("-1"))
                {
                    report = false;
                    RedisHelper.Remove(redisKey);
                }
                LogHelper.GetInstance.Write("ReportUpgradeRate", rate);
                SyncActions.Request(apiName, Method.POST, new { finish_rate = rate });
            }
            catch { }
        }

        /// <summary>
        /// 下载升级文件
        /// </summary>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        private void DownloadFile(string redisKey)
        {
            try
            {
                string apiName = "api/devices/" + SyncActions.GetSchoolId().ToString() + "/" + redisKey + "/update";

                IRestResponse response = SyncActions.Download(apiName);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    this.fileData = response.RawBytes;
                }

                if (fileData.Length <= 0) this.msg = new JsonMsg { code = 500, message = "升级文件获取失败" };
            }
            catch (Exception ex)
            {
                LogHelper.GetInstance.Write("download file error", ex.Message);
            }
        }

        private void UpgradeRevThread()
        {
            IPEndPoint iPEndPoint = null;
            while (isProcessing)
            {
                byte[] data;
                try
                {
                    data = this.udpClient.Receive(ref iPEndPoint);
                    if (GlobalData.IsDebug)
                    {
                        LogHelper.GetInstance.Write("upgrade received from device：", data);
                    }
                }
                catch
                {
                    continue;
                }

                if (!Helper.CheckCRC16(data, Convert.ToUInt16(data.Length)))
                {
                    continue;
                }

                if ((data[0] != 0xf0) || (data[1] != 0x55)) continue;

                if (data[4] != 0x11) continue;

                this.receivedSerialId = (int)BitConverter.ToUInt16(data, 6);

            }
        }


        private JsonMsg ExecuteUpgrade()
        {
            try
            {
                string stopKey = Helper.md5("device_abort_upgrade_" + this.deviceId.ToString());
                if (RedisHelper.Exists(stopKey)) RedisHelper.Remove(stopKey);

                MonitorServer monitor = new MonitorServer();
                upgradePort = monitor.GetPort();

                IPEndPoint localEP = new IPEndPoint(IPAddress.Any, upgradePort);
                isProcessing = true;

                this.udpClient = new UdpClient(localEP);
                this.receivedThread = new Thread(new ThreadStart(this.UpgradeRevThread));
                this.receivedThread.Start();

                reportTimes = 0;
                this.msg = comActions.ExecuteBeginUpgradeCmd(isArm);
                if (msg.code != 200) return msg;
                Thread.Sleep(10);

                string deviceKey = Helper.md5("device_upgrade_" + deviceId.ToString());
                string redisKey = Helper.md5("device_upgrade_rate_" + deviceId.ToString());
                RedisHelper.Set(redisKey, 0, 10);

                Thread thread = new Thread(new ThreadStart(() =>
                {
                    ReportUpgradeThread();
                }))
                {
                    IsBackground = true
                };

                thread.Start();

                int size = 1024;
                int count = size;
                double pageCount = Math.Ceiling(Convert.ToDouble(fileData.Length - 1024) / size);
                byte[] buff;
                double finishRate = 0;

                byte[] header = new byte[1024];
                byte[] body = new byte[this.fileData.Length - 1024];

                Array.Copy(this.fileData, 1024, body, 0, body.Length);

                int serialId = 0;
                for (serialId = 0; (serialId < pageCount) && isProcessing; serialId++)
                {
                    if (serialId == pageCount - 1)
                    {
                        count = (int)(body.Length - (pageCount - 1) * size);
                    }
                    buff = new byte[count];
                    Array.Copy(body, serialId * size, buff, 0, count);

                    command = devCommand.CreateSendUpgradeFileCmd(isArm, serialId + 1, buff);
                    if (GlobalData.IsDebug)
                    {
                        LogHelper.GetInstance.Write("upgrade send to device：", command);
                    }
                    int sendTimes = 0;
                    do
                    {
                        sendTimes++;
                        needSendAgain = false;
                        try
                        {
                            this.udpClient.Send(command, command.Length, deviceEndPoint);
                            while ((serialId + 1) != receivedSerialId)
                            {
                                if (needSendAgain)
                                {
                                    break;
                                }
                            }
                            if (!needSendAgain)
                            {
                                break;
                            }
                        }
                        catch
                        {
                        }
                    } while (sendTimes <= 3);



                    finishRate = (serialId + 1) / pageCount * 100;
                    finishRate = Math.Round(finishRate, 0);

                    RedisHelper.Set(redisKey, finishRate.ToString(), 10);
                }

                this.isProcessing = false;
                if (this.receivedThread.IsAlive)
                {
                    this.receivedThread.Abort();
                    this.receivedThread = null;
                }

                if (isAbort)
                {
                    return new JsonMsg { code = 500, message = "升级已被强制停止" };
                }

                Thread.Sleep(10);
                this.msg = comActions.ExecuteFinishUpgradeCmd(isArm);
                if (msg.code != 200) return msg;
                RedisHelper.Set(redisKey, 100, 1);

                RedisHelper.Set("device_update_params_" + deviceId.ToString(), "", 5);

                Thread deleteThread = new Thread(new ThreadStart(this.DeleteFinishThread));
                deleteThread.Start();


                using (MySqlConnection conn = new MySqlConnection(Helper.GetConstr()))
                {
                    conn.Open();
                    List<MySqlParameter> parameters = new List<MySqlParameter>();
                    MySqlHelper.ExecuteNonQuery(conn, "update dev_device set status=101 where id=" + deviceId.ToString(), parameters.ToArray());

                    MySqlHelper.ExecuteNonQuery(conn, "update log_action set status=1 where id=" + this.logId.ToString(), parameters.ToArray());
                }

                ReportUpgradeRate();

                RedisHelper.Remove(deviceKey);
                return msg;
            }
            catch (Exception ex)
            {
                LogHelper.GetInstance.Write("upgrade error:", ex.Message);
                return new JsonMsg { code = 500, message = "升级失败" };
            }
        }
    }
}