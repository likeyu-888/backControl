using EliteService.Api;
using EliteService.Audio;
using EliteService.DTO;
using EliteService.Service;
using EliteService.Utility;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace EliteService.Control
{
    public class CommandActions
    {
        private object lockObj = new object();

        private DeviceCommand devCommand;
        private byte[] command;
        private IPEndPoint ipEndPoint;
        private byte[] returns;
        private int deviceId;

        public CommandActions(IPEndPoint ipEndPoint, int deviceId)
        {
            devCommand = new DeviceCommand();
            this.ipEndPoint = ipEndPoint;
            this.deviceId = deviceId;
        }

        public CommandActions()
        {
        }

        /// <summary>
        /// 参数查询
        /// </summary>
        /// <returns></returns>
        public JsonMsg ExecuteQueryParamsCmd()
        {
            command = devCommand.CreateQueryParamsCmd();
            JsonMsg msg = ExecuteCommand(command);

            if (msg.code != 200) return msg;

            byte[] res = msg.data;
            if (res.Length > 308)
            {
                byte[] dsp = new byte[400];
                Array.Copy(res, 308, dsp, 0, res.Length - 308);
                GlobalData.DeviceList[deviceId].Dsp = dsp;
            }


            return new JsonMsg { code = 200, message = "操作成功", data = returns };
        }

        /// <summary>
        /// 参数设置
        /// </summary>
        /// <returns></returns>
        public JsonMsg ExecuteUpdateParamsCmd(byte[] buff)
        {
            byte[] data = new byte[614];
            Array.Copy(buff, 8, data, 0, 614);
            command = devCommand.CreateUpdateParamsCmd(data);

            JsonMsg msg = ExecuteCommand(command);

            if (msg.code != 200) return msg;

            byte[] res = data;
            if (res.Length > 308)
            {
                byte[] dsp = new byte[400];
                Array.Copy(res, 308, dsp, 0, res.Length - 308);
                GlobalData.DeviceList[deviceId].Dsp = dsp;
            }

            return new JsonMsg { code = 200, message = "操作成功", data = returns };
        }


        /// <summary>
        /// 查询下载
        /// </summary>
        /// <returns></returns>
        public JsonMsg ExecuteQueryRecordCmd(byte[] buff)
        {
            command = new byte[buff.Length - 43];

            Array.Copy(buff, 8, command, 0, buff.Length - 43);

            int id = BitConverter.ToUInt16(buff, 11); //第11，12位为id
            string create_time = Encoding.UTF8.GetString(buff, 13, Helper.GetStrLength(buff, 13, 25)); //13-37,25位为日期
            int page = BitConverter.ToUInt16(buff, 38); //38-39
            int page_size = BitConverter.ToUInt16(buff, 40); //40-41
            string sort_column = Encoding.UTF8.GetString(buff, 42, Helper.GetStrLength(buff, 42, 20)); //42-61
            string sort_direction = Encoding.UTF8.GetString(buff, 62, Helper.GetStrLength(buff, 62, 20)); //62-81

            JsonMsg msg = QueryRecord.Get(id, create_time, page, page_size, sort_column, sort_direction);

            return msg;
        }

        /// <summary>
        /// 执行下载
        /// </summary>
        /// <returns></returns>
        public JsonMsg ExecuteDownRecordCmd(byte[] buff)
        {
            int id = BitConverter.ToInt32(buff, 7); //第7，8,9,10位为id       

            JsonMsg msg = QueryRecord.DownRecord(id);

            return msg;
        }



        /// <summary>
        /// 通用转发
        /// </summary>
        /// <returns></returns>
        public JsonMsg ExecuteCommonForwardCmd(byte[] buff)
        {
            command = new byte[buff.Length - 43];

            Array.Copy(buff, 8, command, 0, buff.Length - 43);

            JsonMsg msg = ExecuteCommand(command);

            if (msg.code != 200) return msg;
            return new JsonMsg { code = 200, message = "操作成功", data = returns };
        }

        /// <summary>
        /// 状态查询
        /// </summary>
        /// <returns></returns>
        public JsonMsg ExecuteStatusCmd()
        {
            command = devCommand.CreateStatusCmd();

            JsonMsg msg = ExecuteCommand(command);
            if (msg.code != 200) return msg;
            return new JsonMsg { code = 200, message = "操作成功", data = returns };
        }


        /// <summary>
        /// 实际操作设备停止监听
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="clientPort"></param>
        /// <returns></returns>
        public JsonMsg StopDeviceMonitor(int channel, int clientPort)
        {
            RemoteCommand remote = new RemoteCommand();
            byte[] command = remote.CreateMonitorCmd(channel, clientPort, false);

            string ip = GlobalData.DeviceList.ContainsKey(deviceId) ? GlobalData.DeviceList[deviceId].Ip : "";
            if (DataValidate.IsIp(ip))
            {
                returns = UdpHelper.SendCommand(command, new IPEndPoint(IPAddress.Parse(ip), GlobalData.DeviceControlPort));
                JsonMsg msg = CommandActions.CheckResult(returns);
                if (msg.code == 200)
                {
                    GlobalData.DeviceList[deviceId].ListenStatus = 0;
                }
                return msg;
            }
            return new JsonMsg { code = 200, message = "操作成功", data = new byte[] { } };
        }

        /// <summary>
        /// 实际操作设备开始监听
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="clientPort"></param>
        /// <returns></returns>
        public JsonMsg StartDeviceMonitor(int channel, int clientPort)
        {
            //LogHelper.GetInstance.Write("开始监听，监听端口;", clientPort.ToString()); ;//2020-10-13  lky
            if (!GlobalData.RtpFramerList.ContainsKey(deviceId))
            {
                GlobalData.RtpFramerList.Add(deviceId, new RtpFramer(deviceId, clientPort));
            }
            RemoteCommand remote = new RemoteCommand();
            command = remote.CreateMonitorCmd(channel, clientPort, true);
            string ip = GlobalData.DeviceList.ContainsKey(deviceId) ? GlobalData.DeviceList[deviceId].Ip : "";
            if (DataValidate.IsIp(ip))
            {
                returns = UdpHelper.SendCommand(command, new IPEndPoint(IPAddress.Parse(ip), GlobalData.DeviceControlPort));

                JsonMsg msg = CommandActions.CheckResult(returns);
                if (msg.code == 200)
                {
                    GlobalData.DeviceList[deviceId].ListenChannel = channel;
                    GlobalData.DeviceList[deviceId].ListenPort = clientPort;
                    GlobalData.DeviceList[deviceId].ListenStatus = 1;
                }
                return msg;
            }
            return new JsonMsg { code = 500, message = "当前设备不在线", data = new byte[] { } };
        }

        public JsonMsg StartDeviceMonitor(int deviceId, int channel, int clientPort)
        {
            this.deviceId = deviceId;
            return StartDeviceMonitor(channel, clientPort);
        }

        public JsonMsg StopDeviceMonitor(int deviceId, int channel, int clientPort)
        {
            this.deviceId = deviceId;
            return StopDeviceMonitor(channel, clientPort);
        }

        /// <summary>
        /// 设备监听
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="isAutoRecord"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public JsonMsg ExecuteBeginMonitorCmd(byte[] request)
        {
            try
            {
                int channel = request[5];

                string clientIp = Convert.ToInt32(request[7]).ToString() + "." + Convert.ToInt32(request[8]).ToString() + "." + Convert.ToInt32(request[9]).ToString() + "." + Convert.ToInt32(request[10]).ToString(); ;

                string key = "device_monitor_" + deviceId.ToString();

                if (!GlobalData.DeviceList.ContainsKey(deviceId))
                {
                    return new JsonMsg { code = 500, message = "当前设备不在线", data = new byte[] { } };
                }
                int port = GlobalData.DeviceList[deviceId].ListenPort;
                int currentChannel = GlobalData.DeviceList[deviceId].ListenChannel;
                int listenStatus = GlobalData.DeviceList[deviceId].ListenStatus;

                if ((listenStatus == 1) && (currentChannel != channel))
                {  //在监听，通道不同，先关闭
                    this.StopDeviceMonitor(currentChannel, port);
                    using (MySqlConnection conn = new MySqlConnection(Helper.GetConstr()))
                    {
                        conn.Open();
                        string query = "update dev_device set is_auto_record=0 where id=" + deviceId.ToString();
                        MySqlHelper.ExecuteNonQuery(conn, query);
                        conn.Close();
                    }
                    listenStatus = 0;
                }
                if (port == 0)
                {
                    MonitorServer monitor = new MonitorServer();
                    port = monitor.GetPort();
                }

                if (listenStatus == 0)
                {
                    this.StartDeviceMonitor(channel, port);
                }


                if (GlobalData.RtpFramerList.ContainsKey(deviceId))
                {
                    bool result = GlobalData.RtpFramerList[deviceId].AddSender(clientIp);
                    if (result)
                    {
                        RedisHelper.Set(key, "");
                        return new JsonMsg { code = 200, message = "操作成功", data = returns };
                    }
                    return new JsonMsg { code = 500, message = "其他人员正在监听该设备", data = new byte[] { } };
                }
                else
                {
                    RedisHelper.Remove(key);
                    return new JsonMsg { code = 500, message = "当前设备不在线", data = new byte[] { } };
                }
            }
            catch
            {
                return new JsonMsg { code = 500, message = "异常错误", data = new byte[] { } };
            }
        }

        /// <summary>
        /// 设备录音
        /// </summary>
        /// <param name="isBegin"></param>
        /// <returns></returns>
        public JsonMsg ExecuteRecordCmd(bool isBegin)
        {
            try
            {
                lock (this.lockObj)
                {
                    if (GlobalData.DeviceList.ContainsKey(deviceId))
                    {
                        GlobalData.DeviceList[deviceId].IsAutoRecord = isBegin ? 1 : 0;
                    }
                }

                CommandActions actions = new CommandActions();
                int channel = 1; //录音通道

                DeviceCommand devCommand = new DeviceCommand();
                byte[] command = devCommand.CreateStatusCmd();

                byte[] statusBytes = UdpHelper.SendCommand(command, ipEndPoint);

                if (statusBytes[57] == 0) //未监听
                {
                    if (GlobalData.DeviceList.ContainsKey(deviceId))
                    {
                        if (isBegin)
                        {
                            int listen_port = GlobalData.DeviceList[deviceId].ListenPort;
                            if (listen_port == 0)
                            {
                                MonitorServer monitor = new MonitorServer();
                                listen_port = monitor.GetPort();
                            }
                            actions.StartDeviceMonitor(deviceId, channel, listen_port);
                        }
                    }
                }
                else
                {  //监听中

                    if (GlobalData.DeviceList.ContainsKey(deviceId))
                    {
                        if (isBegin)
                        {
                            if (GlobalData.DeviceList[deviceId].ListenChannel != 1) //当前监听通道不为1
                            {
                                string monitorKey = "device_monitor_" + deviceId.ToString();
                                if (RedisHelper.Exists(monitorKey)) RedisHelper.Remove(monitorKey);
                                actions.StopDeviceMonitor(deviceId, GlobalData.DeviceList[deviceId].ListenChannel, GlobalData.DeviceList[deviceId].ListenPort);

                                if (GlobalData.RtpFramerList.ContainsKey(deviceId))
                                {
                                    _ = GlobalData.RtpFramerList[deviceId].RemoveSender(GlobalData.RtpFramerList[deviceId].clientIp);
                                }

                                int port = GlobalData.DeviceList[deviceId].ListenPort;
                                actions.StartDeviceMonitor(deviceId, channel, port);
                            }
                        }
                        else
                        {
                            actions.StopDeviceMonitor(deviceId, GlobalData.DeviceList[deviceId].ListenChannel, GlobalData.DeviceList[deviceId].ListenPort);
                            string monitorKey = "device_monitor_" + deviceId.ToString();
                            if (RedisHelper.Exists(monitorKey)) RedisHelper.Remove(monitorKey);
                            if (GlobalData.RtpFramerList.ContainsKey(deviceId))
                            {
                                _ = GlobalData.RtpFramerList[deviceId].RemoveSender(GlobalData.RtpFramerList[deviceId].clientIp);
                            }
                        }
                    }
                }
                this.UpdateDeviceStatus();


            }
            catch (Exception) { }

            return new JsonMsg { code = 200, message = "操作成功" };
        }


        /// <summary>
        /// 删除服务器
        /// </summary>
        /// <returns></returns>
        public JsonMsg ExecuteDeleteCmd()
        {
            try
            {
                CommandActions actions = new CommandActions();

                DeviceCommand devCommand = new DeviceCommand();
                byte[] command = devCommand.CreateStatusCmd();

                byte[] statusBytes = UdpHelper.SendCommand(command, ipEndPoint);

                if (statusBytes[57] != 0) //监听中
                {
                    if (GlobalData.DeviceList.ContainsKey(deviceId))
                    {
                        actions.StopDeviceMonitor(deviceId, GlobalData.DeviceList[deviceId].ListenChannel, GlobalData.DeviceList[deviceId].ListenPort);
                        string monitorKey = "device_monitor_" + deviceId.ToString();
                        if (RedisHelper.Exists(monitorKey)) RedisHelper.Remove(monitorKey);
                        if (GlobalData.RtpFramerList.ContainsKey(deviceId))
                        {
                            _ = GlobalData.RtpFramerList[deviceId].RemoveSender(GlobalData.RtpFramerList[deviceId].clientIp);
                        }
                    }
                }

                lock (this.lockObj)
                {
                    if (GlobalData.DeviceList.ContainsKey(deviceId))
                    {
                        GlobalData.DeviceList.Remove(deviceId);
                    }
                }
            }
            catch (Exception) { }

            return new JsonMsg { code = 200, message = "操作成功" };
        }

        /// <summary>
        /// 同步数据
        /// </summary>        
        /// <returns></returns>
        public JsonMsg ExecuteSyncDataCmd()
        {
            SyncData syncData = new SyncData();
            syncData.SendData();

            return new JsonMsg { code = 200, message = "操作成功" };
        }

        private void UpdateDeviceStatus()
        {

            try
            {
                DeviceCommand devCommand = new DeviceCommand();
                byte[] command = devCommand.CreateStatusCmd();
                byte[] returns = UdpHelper.SendCommand(command, new IPEndPoint(IPAddress.Parse(GlobalData.DeviceList[deviceId].Ip), GlobalData.DeviceControlPort));

                if (returns.Length < 256) return;

                bool result = Helper.CheckCRC16(returns, returns.Length);

                if (result)
                {
                    string redisKey = Helper.md5("device_upgrade_rate_" + deviceId.ToString());
                    if (RedisHelper.Exists(redisKey))
                    {
                        if (RedisHelper.Get(redisKey).ToString().Equals("100")) return;
                    }

                    string key = Helper.md5("device_status_" + deviceId.ToString()); //状态存在redis中

                    RedisHelper.Set(key, JsonConvert.SerializeObject(returns));

                    float snr = BitConverter.ToSingle(returns, 112);
                    int listen_efficiency = Convert.ToInt16(returns[192]);
                    int attendence_difficulty = 0;
                    float anbient_noice = BitConverter.ToSingle(returns, 80);
                    int status = Convert.ToInt16(returns[5]);

                    using (MySqlConnection conn = new MySqlConnection(Helper.GetConstr()))
                    {
                        conn.Open();
                        string query = "update dev_device set status =" + status.ToString() + "," +
                             "snr=" + snr.ToString() + "," +
                            "listen_efficiency=" + listen_efficiency.ToString() + "," +
                            "attendence_difficulty=" + attendence_difficulty.ToString() + "," +
                            "anbient_noice=" + anbient_noice.ToString() + " " +
                        " where id=" + deviceId.ToString();
                        MySqlHelper.ExecuteNonQuery(conn, query);
                        conn.Close();
                    }
                }
            }
            catch { }
        }


        /// <summary>
        /// 停止设备监听
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="isAutoRecord"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public JsonMsg ExecuteStopMonitorCmd(byte[] request)
        {

            int channel = request[5];
            int isAutoRecord = request[6];

            string clientIp = Convert.ToInt32(request[7]).ToString() + "." + Convert.ToInt32(request[8]).ToString() + "." + Convert.ToInt32(request[9]).ToString() + "." + Convert.ToInt32(request[10]).ToString(); ;


            if (!GlobalData.DeviceList.ContainsKey(deviceId))
            {
                return new JsonMsg { code = 200, message = "当前设备不在线", data = new byte[] { } };
            }

            string key = "device_monitor_" + deviceId.ToString();
            if (GlobalData.RtpFramerList.ContainsKey(deviceId))
            {
                _ = GlobalData.RtpFramerList[deviceId].RemoveSender(clientIp);
            }
            try
            {
                RedisHelper.Remove(key);
            }
            catch { }

            int port = GlobalData.DeviceList[deviceId].ListenPort;
            int currentChannel = GlobalData.DeviceList[deviceId].ListenChannel;
            int listenStatus = GlobalData.DeviceList[deviceId].ListenStatus;

            JsonMsg msg = new JsonMsg { code = 200, message = "操作成功" };

            if (isAutoRecord == 0)
            {
                if (listenStatus == 1)
                {
                    this.StopDeviceMonitor(currentChannel, port);
                }
            }
            else
            {
                if ((listenStatus == 1) && (currentChannel != channel))
                {
                    //在监听，通道不同，先关闭
                    this.StopDeviceMonitor(currentChannel, port);
                    listenStatus = 0;
                    if (port == 0)
                    {
                        MonitorServer monitor = new MonitorServer();
                        port = monitor.GetPort();
                    }
                    if (listenStatus == 0)
                    {
                        msg = this.StartDeviceMonitor(channel, port);
                    }
                }
            }
            return msg;
        }

        /// <summary>
        /// 开始升级
        /// </summary>
        /// <param name="isArm"></param>
        /// <returns></returns>
        public JsonMsg ExecuteBeginUpgradeCmd(bool isArm)
        {
            try
            {
                string key = Helper.md5("device_upgrade_" + this.deviceId.ToString());

                if (RedisHelper.Exists(key))
                {
                    if (!RedisHelper.Get(key).ToString().Equals("100"))
                    {
                        return new JsonMsg { code = 500, message = "设备正在升级中，不可重复操作" };
                    }
                }
                command = devCommand.CreateBeginUpgradeCmd(isArm);

                JsonMsg msg = ExecuteCommand(command);

                if (msg.code == 200)
                {
                    RedisHelper.Set(key, 0, 10);
                }
                return msg;
            }
            catch
            {
                return new JsonMsg { code = 500, message = "异常错误" };
            }
        }


        /// <summary>
        /// 提交升级，新编，通过redisKey拉取文件进行升级
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public JsonMsg ExecuteUpgradeCmd(byte[] request)
        {
            try
            {
                bool isArm = (request[5] == 0x6D);

                string key = Helper.md5("device_upgrade_" + this.deviceId.ToString());

                if (RedisHelper.Exists(key))
                {
                    if (!RedisHelper.Get(key).ToString().Equals("100"))
                    {
                        return new JsonMsg { code = 500, message = "设备正在升级中，不可重复操作" };
                    }
                }

                bool fromCloud = (request[request.Length - 17] == 1); //是否来自云平台

                int logId = BitConverter.ToInt32(request, 6);
                string redisKey = Encoding.UTF8.GetString(request, 10, Helper.GetStrLength(request, 10, 32));

                //另起线程序处理
                Thread thread = new Thread(new ThreadStart(() =>
                {
                    Upgrade service = new Upgrade();
                    JsonMsg msg = service.StartUpgrade(fromCloud, ipEndPoint, deviceId, isArm, redisKey, logId);
                }))
                {
                    IsBackground = true
                };

                thread.Start();

                return new JsonMsg { code = 200, message = "操作成功" };
            }
            catch
            {
                return new JsonMsg { code = 500, message = "异常错误" };
            }
        }


        public JsonMsg CheckResult()
        {
            return CheckResult(this.returns);
        }

        public static JsonMsg CheckResult(byte[] data)
        {
            if ((data.Length == 1) && (data[0] == 100)) return new JsonMsg { code = 501, message = "设备未启动或数据传输有误" };

            if (!Helper.CheckCRC16(data, Convert.ToUInt16(data.Length)))
            {
                return new JsonMsg { code = 501, message = "数据传输有误" };
            }
            byte status = data[5];
            if (status == 0xee)
            {
                return new JsonMsg { code = 500, message = "操作失败", data = data };
            }
            else if (status == 0x92)
            {
                return new JsonMsg { code = 500, message = "测试中", data = data };
            }
            else if (status == 0xfd)
            {
                return new JsonMsg { code = 500, message = "故障检测中", data = data };
            }
            return new JsonMsg { code = 200, message = "操作成功", data = data };
        }

        /// <summary>
        /// 发送升级文件
        /// </summary>
        /// <param name="request"></param>
        /// <param name="isFromWeb"></param>
        /// <returns></returns>
        public JsonMsg ExecuteSendUpgradeFileCmd(byte[] request, bool isRequestFromCloud)
        {
            bool isArm = (request[5] == 0x6D);
            int serialId = BitConverter.ToUInt16(request, 6);
            int buffLen = request.Length - 8 - 19;
            if (!isRequestFromCloud) buffLen -= 16;
            byte[] buff = new byte[buffLen];
            Array.Copy(request, 8, buff, 0, buffLen);

            command = devCommand.CreateSendUpgradeFileCmd(isArm, serialId, buff);

            JsonMsg msg = ExecuteCommand(command);
            return msg;
        }

        /// <summary>
        /// 结束升级
        /// </summary>
        /// <param name="isArm"></param>
        /// <returns></returns>
        public JsonMsg ExecuteFinishUpgradeCmd(bool isArm)
        {
            string key = Helper.md5("device_upgrade_" + this.deviceId.ToString());

            command = devCommand.CreateFinishUpgradeCmd(isArm);
            JsonMsg msg = ExecuteCommand(command);

            if (msg.code == 200)
            {
                try
                {
                    RedisHelper.Set(key, "100", 1);
                }
                catch
                {

                }
            }
            return msg;
        }

        /// <summary>
        /// 强制结束升级
        /// </summary>
        /// <param name="isArm"></param>
        /// <returns></returns>
        public JsonMsg ExecuteAbortUpgradeCmd(bool isArm)
        {
            try
            {
                string redisKey = Helper.md5("device_upgrade_rate_" + this.deviceId.ToString());
                RedisHelper.Remove(redisKey);

                string key = Helper.md5("device_upgrade_" + this.deviceId.ToString());
                RedisHelper.Remove(key);

                string stopKey = Helper.md5("device_abort_upgrade_" + this.deviceId.ToString());
                RedisHelper.Set(stopKey, "", 1);
            }
            catch
            {
                return new JsonMsg { code = 500, message = "异常错误" };
            }

            command = devCommand.CreateAbortUpgradeCmd(isArm);
            return ExecuteCommand(command);

        }


        public JsonMsg ExecuteCommand(byte[] sendData, Socket clientSocket = null, int serialId = 0)
        {
            JsonMsg msg;
            int i = 0;
            do
            {
                returns = UdpHelper.SendCommand(sendData, ipEndPoint, clientSocket);
                msg = CheckResult();

                if (msg.code == 200)
                {
                    if (serialId != 0)
                    {
                        if (msg.data.Length > 6)
                        {
                            int recvSerialId = BitConverter.ToUInt16(msg.data, 6);
                            if (recvSerialId != serialId) continue;
                            else break;
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
                else
                {
                }
                i += 1;
                Thread.Sleep(10);
            }
            while (i < 2);
            return msg;
        }

    }

}
