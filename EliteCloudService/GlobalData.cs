using EliteService.DTO;
using EliteService.Utility;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace EliteService
{
    public class GlobalData
    {
        public static Dictionary<int, School> SchoolList;

        public static int CloudServerPort = 30012; //服务器监听端口,接收客户端端口

        public static int MaxWorkThread = 16;
        public static int MaxIoThread = 16;
        public static int MinWorkThread = 2;
        public static int MinIoThread = 2;
        public static int ClientAudiolPort = 20006;

        public static string CloudServerIp = "127.0.0.1";
        public static bool IsDebug = false;


        private static readonly object lockObj = new object();

        static GlobalData()
        {
            SchoolList = new Dictionary<int, School>();
        }


        /// <summary>
        /// 维护机器列表，以及系统设置读取
        /// </summary>
        public static void Initial()
        {
            try
            {
                Console.WriteLine("更新学校列表以及系统参数设置");
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
                            foreach (KeyValuePair<int, School> device in SchoolList)
                            {
                                device.Value.deleteTag = 1;
                            }

                            command = "select id,name,password from sch_school where is_delete=0 order by id";
                            ds = MySqlHelper.ExecuteDataset(conn, command);
                            if (ds.Tables[0].Rows.Count > 0)
                            {
                                int id = 0;
                                foreach (DataRow row in ds.Tables[0].Rows)
                                {
                                    id = Convert.ToInt32(row["id"]);

                                    if (!SchoolList.ContainsKey(id))
                                    {
                                        School school = new School
                                        {
                                            id = id,
                                            deleteTag = 0,
                                            password = Helper.StrToHexByte(row["password"].ToString())
                                        };
                                        SchoolList.Add(school.id, school);
                                    }
                                    else
                                    {
                                        SchoolList[id].password = Helper.StrToHexByte(row["password"].ToString());
                                        SchoolList[id].deleteTag = 0;
                                    }
                                }
                            }
                        }
                        //删除标志仍为1的，进行删除
                        foreach (KeyValuePair<int, School> school in SchoolList)
                        {
                            if (school.Value.deleteTag == 1)
                            {
                                SchoolList.Remove(school.Value.id);
                            }
                        }

                        command = "select item_key,value from sys_config_item  where is_delete=0 " +
                            " and item_key in ('cloud_server_ip','cloud_server_port','is_debug','token_expire_seconds') order by id";
                        ds = MySqlHelper.ExecuteDataset(conn, command);
                        if (ds.Tables[0].Rows.Count > 0)
                        {
                            foreach (DataRow row in ds.Tables[0].Rows)
                            {
                                switch (row["item_key"].ToString())
                                {
                                    case "cloud_server_ip":
                                        CloudServerIp = row["value"].ToString();
                                        break;
                                    case "cloud_server_port":
                                        CloudServerPort = Convert.ToInt32(row["value"]);
                                        break;
                                    case "token_expire_seconds":
                                        RedisHelper.Set("token_expire_seconds", Convert.ToInt32(row["value"]));
                                        break;
                                    case "is_debug":
                                        IsDebug = (Convert.ToInt32(row["value"]) == 1);
                                        break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
    }
}