using Elite.WebServer.Services;
using EliteService.Api;
using EliteService.DTO;
using EliteService.Service;
using EliteService.Utility;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;

namespace EliteService.Control
{
    public class AutoSave
    {
        private readonly object lockObj = new object();

        private int intervalCount = 0;

        private int heartBeatInterval = 10; //心跳检测时间间隔。

        private bool sendingFlag = true;//正在发送邮件

        public void BeginTask()
        {

            Thread thread = new Thread(new ThreadStart(() =>
            {
                DealAutoSave();
            }))
            {
                IsBackground = true
            };

            thread.Start();
        }

        /// <summary>
        /// 声学数据自动保存
        /// </summary>
        private void DealAutoSave()
        {
            while (true)
            {
                GlobalData.Initial(); //2020-06-27
                try
                {
                    foreach (KeyValuePair<int, Device> item in GlobalData.DeviceList.ToArray())
                    {
                        void method(object revObj) => this.IntervalAction(revObj);
                        ThreadPool.QueueUserWorkItem(method, item.Value);
                    }
                }
                catch
                {
                }

                Thread.Sleep(this.heartBeatInterval * 1000);
                intervalCount++;
                if (intervalCount >= 10000) intervalCount = 0;
            }
        }

        private void IntervalAction(object obj)
        {
            Device dev = (Device)obj;
            DeviceCommand devCommand = new DeviceCommand();
            byte[] command = devCommand.CreateStatusCmd();

            if (!DataValidate.IsIp(dev.Ip))
            {
                using (MySqlConnection conn = new MySqlConnection(Helper.GetConstr()))
                {
                    try
                    {
                        conn.Open();
                        string query = "update dev_device set status =100 where id=" + dev.Id.ToString();
                        MySqlHelper.ExecuteNonQuery(conn, query);
                        conn.Close();
                    }
                    catch { }

                    if (SyncActions.SendToCloudEnable())
                    {
                        SyncActions.Request("api/devices/" + SyncActions.GetSchoolId().ToString() + "/" + dev.Id.ToString() + "/histories", RestSharp.Method.POST, new
                        {
                            school_id = SyncActions.GetSchoolId(),
                            status = 100,
                            snr = 0,
                            listen_efficiency = 0,
                            attendence_difficulty = 0,
                            anbient_noice = 0
                        });
                    }
                }
                return;
            }

            byte[] returns = UdpHelper.SendCommand(command, new IPEndPoint(IPAddress.Parse(dev.Ip), GlobalData.DeviceControlPort));

            lock (lockObj)
            {
                try
                {
                    if (returns.Length <= 1) GlobalData.DeviceList[dev.Id].Errors++;
                    else GlobalData.DeviceList[dev.Id].Errors = 0;

                    if (GlobalData.DeviceList[dev.Id].Errors > 3)
                    {
                        GlobalData.RemoveDevice(dev.Id);
                    }
                }
                catch
                {
                }
            }

            if (dev.IsAutoSave == 1)
            {
                UpdateDatabase(returns, dev);
            }
            else
            {
                UpdateDatabaseWithoutSaveHistory(returns, dev);
            }

            //邮件预警
            if (returns.Length >1)
            {
                if (returns[5] == 0x00 || returns[5] == 0xbb)//0x00--正常&空闲 0xbb--正常&繁忙
                {
                    dev.EmailedSentFlag = false;
                }
                else if (returns[5] == 0x01 || returns[5] == 0xbc)//0x01--故障&空闲 0xbc--故障&繁忙
                {
                    if (!dev.EmailedSentFlag && dev.DeviceType == 5)//上线后还未发送邮件预警（且设备型号为OS-704FC-A）
                    {
                        Action<byte[], Device> action = SendWarningEmail;
                        action.BeginInvoke(returns, dev, null, null);
                    }
                }
            }
        }

        private void SendWarningEmail(byte[] returns, Device dev)
        {
          

            string arm_version;
            string dsp_version;
            string room_name;
            string group_name;
            using (MySqlConnection conn = new MySqlConnection(Helper.GetConstr()))
            {
                conn.Open();
                //查询的列
                string columns = "dev.device_type,dev.id, dev.name, dev.ip,arm_version,dsp_version, dev.mark, dev.gateway,dev.mac, dev.group_id, dev.status, grp.name as group_name, dev.room_id,room.reverb_time,room.name as room_name,dev.create_time ";
                //sql语句
                StringBuilder commandText = new StringBuilder("SELECT " + columns +
                    "FROM dev_device dev " +
                    "left join sch_room room on dev.room_id = room.id and room.is_delete=0 " +
                    "left join dev_group grp on dev.group_id = grp.id " +
                    "where dev.id=" + dev.Id.ToString());
                //查询结果
                DataSet ds_device = MySqlHelper.ExecuteDataset(conn, commandText.ToString());
                if (ds_device.Tables[0].Rows.Count > 0)
                {
                    DataRow device = ds_device.Tables[0].Rows[0];
                    //string arm_version = Convert.ToString(device["arm_version"]);
                    //string dsp_version = Convert.ToString(device["dsp_version"]);
                    room_name = Convert.ToString(device["room_name"]);
                    group_name = Convert.ToString(device["group_name"]);
                }
                else
                {
                    return;
                }
                //给所有用户发预警邮件
                DataSet ds_user = MySqlHelper.ExecuteDataset(conn, "select user.contact_name,user.email,user.status,user.is_delete from ucb_user user");
                if (ds_user.Tables[0].Rows.Count > 0)
                {
                    for (int i = 0; i < ds_user.Tables[0].Rows.Count; i++)
                    {
                        DataRow user = ds_user.Tables[0].Rows[i];

                        //接收人
                        string toEmailAddress = user["email"].ToString();
                        //接收人称呼
                        string contactName = user["contact_name"].ToString();
                        //标题
                        string subject = "主机故障预警";
                        //用户账号是否启用
                        int status = Convert.ToInt32(user["status"]);
                        //用户账号是否被删除
                        int is_delete = Convert.ToInt32(user["is_delete"]);

                        if (!string.IsNullOrEmpty(contactName) && !string.IsNullOrEmpty(toEmailAddress) && status == 1 && is_delete == 0)
                        {
                            //正文
                            string body = ReplaceText(contactName, dev.Name, dev.Ip, room_name, group_name);
                            EmailHelper emailHelper = new EmailHelper(toEmailAddress, subject, true, body);
                            emailHelper.Send();
                            dev.EmailedSentFlag = true;
                            LogHelper.GetInstance.Write($"发送预警邮件", $"接收人：{contactName}--{toEmailAddress}，故障设备：{dev.Name}--{dev.Ip} 所属教室：{room_name} 所属分组：{group_name}");
                        }
                    }

                }
                conn.Close();
            }
        }

        /// <summary>
        /// 替换模板中的字段值
        /// </summary>
        /// <param name="toName"></param>
        /// <param name="deviceName"></param>
        /// <param name="deviceIP"></param>
        /// <returns></returns>
        public string ReplaceText(string contactName, string deviceName, string deviceIP, string roomName, string groupName)
        {
            string Namespace = MethodBase.GetCurrentMethod().DeclaringType.Namespace;
            string path = $"{Namespace}.EmailTemplate.WarningTemplate.html";
            if (path == string.Empty)
            {
                return string.Empty;
            }
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            StreamReader sr = new StreamReader(stream);
            string str;
            str = sr.ReadToEnd();
            str = str.Replace("$Contact_Name$", contactName);
            str = str.Replace("$Device_Name$", deviceName);
            str = str.Replace("$Device_IP$", deviceIP);
            str = str.Replace("$Room_Name$", roomName);
            str = str.Replace("$Group_Name$", groupName);

            return str;
        }

        private void UpdateStatus(int deviceId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Helper.GetConstr()))
                {
                    conn.Open();
                    string query = "update dev_device set status =100 where id=" + deviceId.ToString();
                    MySqlHelper.ExecuteNonQuery(conn, query);
                    conn.Close();
                }
            }
            catch { }
        }

        private void UpdateDatabase(byte[] returns, Device dev)
        {
            try
            {
                string redisKey = Helper.md5("device_upgrade_rate_" + dev.Id.ToString());
                if (RedisHelper.Exists(redisKey))
                {
                    if (RedisHelper.Get(redisKey).ToString().Equals("100")) return;
                }

                if (returns.Length < 60)
                {
                    UpdateStatus(dev.Id);
                    return;
                }


                if (Helper.CheckCRC16(returns, returns.Length))
                {

                    string key = Helper.md5("device_status_" + dev.Id.ToString()); //状态存在redis中

                    RedisHelper.Set(key, JsonConvert.SerializeObject(returns));

                    float snr = 0;
                    float listen_efficiency = 0;
                    float attendence_difficulty = 0;
                    float anbient_noice = 0;

                    int status = Convert.ToInt16(returns[5]);


                    lock (lockObj)
                    {
                        if (GlobalData.DeviceList.ContainsKey(dev.Id))
                        {
                            GlobalData.DeviceList[dev.Id].Status = status;
                        }
                    }

                    bool isSave = false;
                    if (((intervalCount * heartBeatInterval) % GlobalData.AutoSaveInterval) == 0)
                    {
                        isSave = true;
                    }

                    using (MySqlConnection conn = new MySqlConnection(Helper.GetConstr()))
                    {
                        conn.Open();

                        if (returns.Length <= 80)
                        {
                            DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_device where id=" + dev.Id.ToString());
                            if (ds.Tables[0].Rows.Count > 0)
                            {
                                DataRow device = ds.Tables[0].Rows[0];
                                snr = Convert.ToSingle(device["snr"]);
                                listen_efficiency = Convert.ToInt16(device["listen_efficiency"]);
                                attendence_difficulty = 0;
                                anbient_noice = Convert.ToSingle(device["anbient_noice"]);
                            }
                        }
                        else
                        {
                            //获取平均值
                            byte[] dsp = GetDsp(dev);

                            try
                            {
                                if (dev.DeviceType == 5)
                                {
                                    NoiseInfo5 noise = new NoiseInfo5();
                                    noise.QueryStatus(returns, dsp);

                                    snr = noise.snr;
                                    listen_efficiency = noise.efficiency;
                                    attendence_difficulty = noise.difficulty;
                                    anbient_noice = noise.noise;
                                }
                                else if (dev.DeviceType == 4)
                                {
                                    NoiseInfo4 noise = new NoiseInfo4();
                                    noise.QueryStatus(returns, dsp);
                                    snr = noise.snr;
                                    listen_efficiency = noise.efficiency;
                                    attendence_difficulty = noise.difficulty;
                                    anbient_noice = noise.noise;
                                }
                            }
                            catch (Exception ex)
                            {
                                LogHelper.GetInstance.Write("噪音取值", "失败:" + ex.Message);
                            }
                        }

                        string query = "";
                        if (isSave)
                        {
                            query = "insert into dev_history_" + dev.Id.ToString() + "" +
                                " set device_id=" + dev.Id.ToString() + "," +
                                "snr=" + snr.ToString() + "," +
                                "listen_efficiency=" + listen_efficiency.ToString() + "," +
                                "attendence_difficulty=" + attendence_difficulty.ToString() + "," +
                                "anbient_noice=" + anbient_noice.ToString();
                            MySqlHelper.ExecuteNonQuery(conn, query);
                        }

                        query = "update dev_device set status =" + status.ToString() + "," +
                             "snr=" + snr.ToString() + "," +
                            "listen_efficiency=" + listen_efficiency.ToString() + "," +
                            "attendence_difficulty=" + attendence_difficulty.ToString() + "," +
                            "anbient_noice=" + anbient_noice.ToString() + " " +
                        " where id=" + dev.Id.ToString();
                        MySqlHelper.ExecuteNonQuery(conn, query);


                        if (dev.ArmVersion.Trim().Equals("") || RedisHelper.Exists("device_update_params_" + dev.Id.ToString()))
                        {
                            float reverb_time = 0;
                            if (dev.RoomId > 0)
                            {
                                object obj = MySqlHelper.ExecuteScalar(conn, "select reverb_time from sch_room where id=" + dev.RoomId.ToString());
                                if (obj != null) reverb_time = Convert.ToSingle(obj);
                            }

                            UpdateDeviceInfo(conn, dev.Id, dev.DeviceType, dev.Name, reverb_time, dev.Ip);
                        }
                        conn.Close();

                        if (isSave)
                        {
                            if (SyncActions.SendToCloudEnable())
                            {
                                SyncActions.Request("api/devices/" + SyncActions.GetSchoolId().ToString() + "/" + dev.Id.ToString() + "/histories", RestSharp.Method.POST, new
                                {
                                    school_id = SyncActions.GetSchoolId(),
                                    status,
                                    snr,
                                    listen_efficiency,
                                    attendence_difficulty,
                                    anbient_noice
                                });
                            }
                        }
                    }
                }
                else
                {
                    UpdateStatus(dev.Id);
                }
            }
            catch
            {
            }
        }

        private byte[] GetDsp(Device dev)
        {

            byte[] dsp = new byte[400];

            if (dev.Dsp == null)
            {
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(dev.Ip), GlobalData.DeviceControlPort);
                CommandActions actions = new CommandActions(ipEndPoint, dev.Id);
                JsonMsg msg = actions.ExecuteQueryParamsCmd();
                byte[] res = msg.data;
                if (res.Length > 308)
                {
                    Array.Copy(res, 308, dsp, 0, res.Length - 308);
                    dev.Dsp = dsp;
                }
            }
            else
            {
                Array.Copy(dev.Dsp, 0, dsp, 0, dev.Dsp.Length);
            }

            return dsp;
        }

        private int ArmInt(byte[] msgData, int index)
        {
            if (msgData.Length < index + 8 + 1) return 0;
            return Convert.ToInt32(msgData[index + 8]);
        }

        private void UpdateDeviceInfo(MySqlConnection transaction, int deviceId, int deviceType, string name, float reverb_time, string deviceIp)
        {
            try
            {
                string updateKey = "device_update_params_" + deviceId.ToString();
                if (RedisHelper.Exists(updateKey)) RedisHelper.Remove(updateKey);

                DeviceCommand devCommand = new DeviceCommand();
                byte[] command = devCommand.CreateQueryParamsCmd();
                byte[] msgData = UdpHelper.SendCommand(command, new IPEndPoint(IPAddress.Parse(deviceIp), GlobalData.DeviceControlPort));

                if (!Helper.CheckCRC16(msgData, msgData.Length))
                {
                    return;
                }

                int armVersion = (int)msgData[297 + 8];
                int dspVersion = (int)msgData[300 + 8 + 5];
                string gateway = ArmInt(msgData, 36).ToString() + "." + ArmInt(msgData, 37).ToString() + "." + ArmInt(msgData, 38).ToString() + "." + ArmInt(msgData, 39).ToString();
                string mark = ArmInt(msgData, 40).ToString() + "." + ArmInt(msgData, 41).ToString() + "." + ArmInt(msgData, 42).ToString() + "." + ArmInt(msgData, 43).ToString();
                string mac = ArmInt(msgData, 44).ToString("X2") + "." + ArmInt(msgData, 45).ToString("X2") + "." + ArmInt(msgData, 46).ToString("X2") + "." + ArmInt(msgData, 47).ToString("X2") + "." + ArmInt(msgData, 48).ToString("X2") + "." + ArmInt(msgData, 49).ToString("X2");
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                MySqlHelper.ExecuteNonQuery(transaction, "update dev_device set " +
                              "arm_version=" + armVersion.ToString() + "," +
                              "dsp_version=" + dspVersion.ToString() + "," +
                              "gateway='" + gateway + "'," +
                              "mark='" + mark + "'," +
                              "mac='" + mac + "' where id=" + deviceId.ToString(), parameters.ToArray());


                byte[] buff = new byte[620];
                Array.Copy(msgData, 8, buff, 0, msgData.Length - 12);

                if (reverb_time != 0)
                {
                    byte[] reverbTime = BitConverter.GetBytes(reverb_time);
                    if (deviceType == 4)
                    {
                        Array.Copy(reverbTime, 0, buff, 496, reverbTime.Length);
                    }
                    else if (deviceType == 5)
                    {
                        Array.Copy(reverbTime, 0, buff, 608, reverbTime.Length);
                    }
                }


                byte[] nameBytes = Encoding.Default.GetBytes(name);
                Array.Copy(new byte[32], 0, buff, 0, 32);
                Array.Copy(nameBytes, 0, buff, 0, nameBytes.Length);

                command = devCommand.CreateUpdateParamsCmd(buff);

                UdpHelper.SendCommand(command, new IPEndPoint(IPAddress.Parse(deviceIp), GlobalData.DeviceControlPort));


                if (SyncActions.SendToCloudEnable())
                {
                    SyncActions.Request("api/devices/" + SyncActions.GetSchoolId().ToString() + "/" + deviceId.ToString() + "/version", RestSharp.Method.PUT, new
                    {
                        arm_version = armVersion,
                        dsp_version = dspVersion,
                        gateway,
                        mark,
                        mac
                    });
                }
            }
            catch { }
        }



        private void UpdateDatabaseWithoutSaveHistory(byte[] returns, Device dev)
        {
            try
            {
                string redisKey = Helper.md5("device_upgrade_rate_" + dev.Id.ToString());
                if (RedisHelper.Exists(redisKey))
                {
                    if (RedisHelper.Get(redisKey).ToString().Equals("100")) return;
                }

                if (returns.Length < 60)
                {
                    UpdateStatus(dev.Id);
                    return;
                }

                bool result = Helper.CheckCRC16(returns, returns.Length);

                if (result)
                {
                    string key = Helper.md5("device_status_" + dev.Id.ToString()); //状态存在redis中

                    RedisHelper.Set(key, JsonConvert.SerializeObject(returns));

                    float snr = 0;
                    float listen_efficiency = 0;
                    float attendence_difficulty = 0;
                    float anbient_noice = 0;

                    int status = Convert.ToInt16(returns[5]);

                    lock (lockObj)
                    {
                        if (GlobalData.DeviceList.ContainsKey(dev.Id))
                        {
                            GlobalData.DeviceList[dev.Id].Status = status;
                        }
                    }

                    bool isSave = false;
                    if (((intervalCount * heartBeatInterval) % GlobalData.AutoSaveInterval) == 0)
                    {
                        isSave = true;
                    }


                    using (MySqlConnection conn = new MySqlConnection(Helper.GetConstr()))
                    {
                        conn.Open();
                        string query = "";

                        if (returns.Length <= 80)
                        {
                            DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_device where id=" + dev.Id.ToString());
                            if (ds.Tables[0].Rows.Count > 0)
                            {
                                DataRow device = ds.Tables[0].Rows[0];
                                snr = Convert.ToSingle(device["snr"]);
                                listen_efficiency = Convert.ToInt16(device["listen_efficiency"]);
                                attendence_difficulty = 0;
                                anbient_noice = Convert.ToSingle(device["anbient_noice"]);
                            }
                        }
                        else
                        {
                            //获取平均值
                            byte[] dsp = GetDsp(dev);

                            try
                            {
                                if (dev.DeviceType == 5)
                                {
                                    NoiseInfo5 noise = new NoiseInfo5();
                                    noise.QueryStatus(returns, dsp);

                                    snr = noise.snr;
                                    listen_efficiency = noise.efficiency;
                                    attendence_difficulty = noise.difficulty;
                                    anbient_noice = noise.noise;
                                }
                                else if (dev.DeviceType == 4)
                                {
                                    NoiseInfo4 noise = new NoiseInfo4();
                                    noise.QueryStatus(returns, dsp);
                                    snr = noise.snr;
                                    listen_efficiency = noise.efficiency;
                                    attendence_difficulty = noise.difficulty;
                                    anbient_noice = noise.noise;
                                }
                            }
                            catch (Exception ex)
                            {
                                LogHelper.GetInstance.Write("噪音取值", "失败:" + ex.Message);
                            }
                        }

                        query = "update dev_device set status =" + status.ToString() + "," +
                                "snr=" + snr.ToString() + "," +
                            "listen_efficiency=" + listen_efficiency.ToString() + "," +
                            "attendence_difficulty=" + attendence_difficulty.ToString() + "," +
                            "anbient_noice=" + anbient_noice.ToString() + " " +
                        " where id=" + dev.Id.ToString();
                        MySqlHelper.ExecuteNonQuery(conn, query);


                        if (dev.ArmVersion.Trim().Equals("") || RedisHelper.Exists("device_update_params_" + dev.Id.ToString()))
                        {
                            float reverb_time = 0;
                            if (dev.RoomId > 0)
                            {
                                object obj = MySqlHelper.ExecuteScalar(conn, "select reverb_time from sch_room where id=" + dev.RoomId.ToString());
                                if (obj != null) reverb_time = Convert.ToSingle(obj);
                            }

                            UpdateDeviceInfo(conn, dev.Id, dev.DeviceType, dev.Name, reverb_time, dev.Ip);
                        }


                        conn.Close();
                    }

                    if (isSave)
                    {
                        if (SyncActions.SendToCloudEnable())
                        {
                            SyncActions.Request("api/devices/" + SyncActions.GetSchoolId().ToString() + "/" + dev.Id.ToString() + "/histories", RestSharp.Method.POST, new
                            {
                                school_id = SyncActions.GetSchoolId(),
                                status,
                                snr,
                                listen_efficiency,
                                attendence_difficulty,
                                anbient_noice
                            });
                        }
                    }
                }
                else
                {
                    UpdateStatus(dev.Id);
                }
            }
            catch (Exception)
            {

            }
        }
    }


}
