using EliteService.Audio;
using EliteService.DTO;
using EliteService.Utility;
using MySql.Data.MySqlClient;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Sockets;
using System.Security.AccessControl;

namespace EliteService
{
    public class GlobalData
    {
        public static Dictionary<int, Device> DeviceList;

        public static Dictionary<int, RtpFramer> RtpFramerList;

        public static int CloudServerPort = 30012; //服务器监听端口,接收客户端端口
        public static string CloudServerIp = "127.0.0.1";

        public static int ControlPort = 30002;
        public static int DeviceControlPort = 20004;
        public static int ClientAudiolPort = 20006;
        public static int MaxWorkThread = 16;
        public static int MaxIoThread = 16;
        public static int MinWorkThread = 2;
        public static int MinIoThread = 2;

        public static int AutoSaveInterval = 60;
        public static int SchoolId = 0;
        public static byte[] RegPassword = new byte[0];
        public static int SaveWaveInterval = 3600;

        public static string ServiceRoot = "";

        public static bool IsDebug = false;


        public static DateTime SyncTime = DateTime.Parse("23:00");


        private static readonly object lockObj = new object();

        public static Socket mServerSocket;

        static GlobalData()
        {
            DeviceList = new Dictionary<int, Device>();
            RtpFramerList = new Dictionary<int, RtpFramer>();
        }

        /// <summary>
        /// 移除机器
        /// </summary>
        /// <param name="id"></param>
        public static void RemoveDevice(int id)
        {
            try
            {
                DeviceList.Remove(id);
                RemoveRtpFrame(id);

                string key = Helper.md5("device_status_" + id.ToString()); //状态存在redis中

                RedisHelper.Remove(key);

                using (MySqlConnection conn = new MySqlConnection(Helper.GetConstr()))
                {
                    conn.Open();
                    string query = "update dev_device set status = 100 where id=" + id.ToString();
                    MySqlHelper.ExecuteNonQuery(conn, query);
                    conn.Close();
                }
            }
            catch { }
        }

        /// <summary>
        /// 移除RTP传输
        /// </summary>
        /// <param name="id"></param>
        public static void RemoveRtpFrame(int id)
        {
            if (RtpFramerList.ContainsKey(id))
            {
                RtpFramerList[id].Dispose();
                RtpFramerList.Remove(id);
            }
        }

        /// <summary>
        /// 维护机器列表，以及系统设置读取
        /// </summary>
        public static void Initial()
        {
            try
            {
                Console.WriteLine("更新设备列表以及系统参数设置");

                string cloudServUrl = System.Configuration.ConfigurationManager.AppSettings["cloudServUrl"].ToString();
                RedisHelper.Set("cloud_server_url", cloudServUrl);

                GlobalData.CloudServerIp = Helper.GetCloudServIp();
                GlobalData.CloudServerPort = Helper.GetCloudServPort();

                using (MySqlConnection conn = new MySqlConnection(Helper.GetConstr()))
                {
                    conn.Open();
                    try
                    {
                        string command;
                        DataSet ds;

                        lock (lockObj)
                        {
                            //把删除标志重置为1
                            foreach (KeyValuePair<int, Device> device in DeviceList.ToArray())
                            {
                                device.Value.DeleteTag = 1;
                            }

                            command = "select id,ip,name,is_auto_save,is_auto_record,arm_version,room_id,device_type from dev_device  where is_delete=0 order by id";
                            ds = MySqlHelper.ExecuteDataset(conn, command);
                            if (ds.Tables[0].Rows.Count > 0)
                            {
                                int id = 0;
                                foreach (DataRow row in ds.Tables[0].Rows)
                                {
                                    id = Convert.ToInt32(row["id"]);

                                    if (!DeviceList.ContainsKey(id))
                                    {
                                        Device device = new Device
                                        {
                                            Id = id,
                                            DeleteTag = 0,
                                            Ip = row["ip"].ToString(),
                                            Name = row["name"].ToString(),
                                            IsAutoSave = Convert.ToInt32(row["is_auto_save"].ToString()),
                                            IsAutoRecord = Convert.ToInt32(row["is_auto_record"].ToString())
                                        };
                                        DeviceList.Add(device.Id, device);
                                    }
                                    else
                                    {
                                        if (!DeviceList[id].Ip.Equals(row["ip"].ToString())) //ip不同需要修改
                                        {
                                            if (RtpFramerList.ContainsKey(id))
                                            {
                                                RtpFramerList[id].Dispose();
                                                RtpFramerList.Remove(id);
                                            }
                                            DeviceList[id].Ip = row["ip"].ToString();
                                        }
                                        DeviceList[id].Name = row["name"].ToString();
                                        DeviceList[id].DeleteTag = 0;
                                    }
                                    DeviceList[id].DeviceType = Convert.ToInt32(row["device_type"].ToString());
                                    DeviceList[id].RoomId = Convert.ToInt32(row["room_id"].ToString());
                                    DeviceList[id].ArmVersion = row["arm_version"].ToString();
                                    DeviceList[id].IsAutoSave = Convert.ToInt32(row["is_auto_save"].ToString());
                                    DeviceList[id].IsAutoRecord = Convert.ToInt32(row["is_auto_record"].ToString());
                                }
                            }
                        }
                        //删除标志仍为1的，进行删除
                        foreach (KeyValuePair<int, Device> device in DeviceList.ToArray())
                        {
                            if (device.Value.DeleteTag == 1)
                            {

                            }
                        }

                        command = "select item_key,value from sys_config_item  where is_delete=0 " +
                            " and group_id=1001 order by id";
                        ds = MySqlHelper.ExecuteDataset(conn, command);
                        if (ds.Tables[0].Rows.Count > 0)
                        {
                            foreach (DataRow row in ds.Tables[0].Rows)
                            {
                                switch (row["item_key"].ToString())
                                {
                                    case "school_id":
                                        SchoolId = Convert.ToInt32(row["value"]);
                                        RedisHelper.Set("school_id", SchoolId);
                                        break;
                                    case "auto_save_interval":
                                        AutoSaveInterval = Convert.ToInt32(row["value"]);
                                        break;
                                    case "reg_password":
                                        RegPassword = Helper.StrToHexByte(row["value"].ToString());
                                        RedisHelper.Set("reg_password", row["value"].ToString());
                                        break;
                                    case "token_expire_seconds":
                                        RedisHelper.Set("token_expire_seconds", row["value"].ToString());
                                        break;
                                    case "save_wave_interval":
                                        SaveWaveInterval = Convert.ToInt32(row["value"]);
                                        break;
                                    case "service_root":
                                        ServiceRoot = row["value"].ToString();
                                        //SetFileRole(ServiceRoot);
                                        break;
                                    case "is_debug":
                                        IsDebug = (Convert.ToInt32(row["value"]) == 1);
                                        RedisHelper.Set("is_debug", row["value"].ToString());
                                        break;
                                    case "sync_time":
                                        DateTime dateTime = Convert.ToDateTime(row["value"].ToString());
                                        GlobalData.SyncTime = dateTime;
                                        break;
                                }

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Initial Error:" + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
        ///// <summary>
        ///// 设置文件夹权限，处理为Everyone所有权限
        ///// </summary>
        ///// <param name="foldPath">文件夹路径</param>
        //public static void SetFileRole(string foldPath)
        //{
        //    DirectorySecurity fsec = new DirectorySecurity();
        //    fsec.AddAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.FullControl,
        //        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        //    System.IO.Directory.SetAccessControl(foldPath, fsec);
        //}
    }
}