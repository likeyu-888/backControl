using Elite.WebServer.Base;
using Elite.WebServer.Utility;
using MySql.Data.MySqlClient;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Elite.WebServer.Services
{
    public class Upgrade
    {
        private MySqlConnection conn;
        private int id;
        private string type;
        private long logId;
        private byte[] file;
        private byte[] tokenHex;

        public static void ReportUpgradeRate(int deviceId, string rate = "0")
        {
            string apiName = "api/devices/" + SyncActions.GetSchoolId().ToString() + "/" + deviceId.ToString() + "/finishrate";
            SyncActions.Request(apiName, Method.POST, new { finish_rate = rate });
        }

        public static string IsValidFile(string type, string fileName, byte[] header, byte[] data, int deviceType)
        {
            try
            {

                if (!fileName.ToLower().EndsWith(".elite"))
                {
                    return "1";
                }
                if (type.Equals("arm"))
                {
                    if (!fileName.ToLower().StartsWith("m"))
                    {
                        return "2";
                    }
                }
                else
                {
                    if (!fileName.ToLower().StartsWith("s"))
                    {
                        return "3";
                    }
                }

                if (header.Length < 1024) return "4";


                if (data.Length < 1) return "5";


                if ((header[0] != 0xf0) || (header[1] != 0xaa)) return "6";


                long length = BitConverter.ToInt64(header, 2);

                CRC16 crcObj = new CRC16();
                int value = crcObj.CreateCRC16(data, Convert.ToUInt32(length));

                if (((int)header[12]) != deviceType) return "7";


                byte[] bytes = BitConverter.GetBytes(value);
                return (bytes[0] == header[10] && bytes[1] == header[11]) ? "1000" : "8";
            }
            catch (Exception)
            {
                return "9";
            }
        }

        public void UpgradeAction(MySqlConnection conn, int id, string type, long logId, byte[] file, byte[] tokenHex)
        {
            this.conn = conn;
            this.id = id;
            this.type = type;
            this.logId = logId;
            this.file = file;
            this.tokenHex = tokenHex;

            Thread thread = new Thread(new ThreadStart(() =>
            {
                UpgradeThread();
            }))
            {
                IsBackground = true
            };

            thread.Start();
        }


        private void UpgradeThread()
        {
            try
            {
                Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                clientSocket.ReceiveTimeout = 1000;

                DeviceCommand devCommand = new DeviceCommand(this.tokenHex, id);
                byte[] command = null;
                IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(Helper.GetLocalServIp()), Helper.GetLocalServPort());
                JsonMsg<byte[]> msg = null;

                int size = 1024;
                int count = size;
                double pageCount = Math.Ceiling(Convert.ToDouble(file.Length) / size);
                byte[] buff;
                double finishRate = 0;

                string redisKey = Helper.md5("device_upgrade_rate_" + id.ToString());

                for (int serialId = 0; serialId < pageCount; serialId++)
                {

                    if (serialId == pageCount - 1)
                    {
                        count = (int)(file.Length - (pageCount - 1) * size);
                    }
                    buff = new byte[count];
                    Array.Copy(file, serialId * size, buff, 0, count);
                    command = devCommand.CreateSendUpgradeFileCmd(type == "arm", serialId + 1, buff);

                    msg = UdpHelper.SendCommand(command, iPEndPoint, clientSocket, serialId + 1);
                    if (msg.code != 200)
                    {
                        ActionLog.Failed(conn, logId, msg.message);
                        return;
                    }

                    finishRate = (serialId + 1) / pageCount * 100;

                    finishRate = Math.Round(finishRate, 0);

                    RedisHelper.Set(redisKey, finishRate.ToString(), 10);
                }

                command = devCommand.CreateFinishUpgradeCmd(type == "arm");
                msg = UdpHelper.SendCommand(command, iPEndPoint, clientSocket);

                if (msg.code != 200)
                {
                    ActionLog.Failed(conn, logId, msg.message);
                    return;
                }
                RedisHelper.Set(redisKey, "100", 1);
                List<MySqlParameter> parameters = new List<MySqlParameter>();
                MysqlHelper.ExecuteNonQuery(conn, System.Data.CommandType.Text, "update dev_device set status=100 where id=" + id.ToString(), parameters.ToArray());
                ActionLog.Finished(conn, logId);
                return;
            }
            catch (Exception ex)
            {
                LogHelper.GetInstance.Write("upgrade error", ex.Message);
            }
        }


    }
}
