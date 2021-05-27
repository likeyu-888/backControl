using Elite.WebServer.Base;
using Elite.WebServer.Services;
using Elite.WebServer.Utility;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Web;
using System.Web.Http;

namespace Elite.WebServer.Controllers
{
    public class UpgradesController : BaseController
    {
        public delegate void ParameterizedThreadStart(object obj);

        [HttpPut]
        [HttpPost]
        [Power("admin,manage")]
        [Route("api/devices/{id}/update")]
        public IHttpActionResult Post()
        {
            GetRequest();

            int id = GetId();

            string type = GetString("type").ToLower();

            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入设备Id"));
            }
            if (string.IsNullOrEmpty(type))
            {
                throw new HttpResponseException(Error("请选择升级类型"));
            }

            HttpRequest request = HttpContext.Current.Request;
            HttpFileCollection fileCollection = request.Files;

            if (fileCollection.Count <= 0)
            {
                throw new HttpResponseException(Error("请上传文件"));
            }



            string key = Helper.md5("device_upgrade_" + id.ToString());

            if (RedisHelper.Exists(key))
            {
                throw new HttpResponseException(Error("该设备另一个升级操作正在进行中"));
            }

            HttpPostedFile httpPostedFile = fileCollection[0];

            if (httpPostedFile.ContentLength < 1024)
            {
                throw new HttpResponseException(Error("请上传文件"));
            }
            try
            {

                byte[] file = new byte[httpPostedFile.ContentLength];
                byte[] data = new byte[httpPostedFile.ContentLength - 1024];
                byte[] header = new byte[1024];

                Stream fileStream = httpPostedFile.InputStream;
                fileStream.Read(file, 0, file.Length);
                Array.Copy(file, 0, header, 0, header.Length);
                Array.Copy(file, 1024, data, 0, data.Length);

                using (conn = new MySqlConnection(Constr()))
                {
                    conn.Open();

                    List<MySqlParameter> parameters = new List<MySqlParameter>();

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@id", id));
                    DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_device where id=@id and is_delete=0", parameters.ToArray());
                    if (DsEmpty(ds)) return ErrorJson("设备不存在");
                    DataRow row = ds.Tables[0].Rows[0];

                    string isValid = Upgrade.IsValidFile(type, httpPostedFile.FileName, header, data, Convert.ToInt32(row["device_type"]));
                    if (!isValid.Equals("1000")) return ErrorJson("所选升级文件有误，请重新选择:" + isValid);

                    parameters.Clear();
                    ds = MySqlHelper.ExecuteDataset(conn, "select * from sys_config_item where item_key='service_root'", parameters.ToArray());
                    if (DsEmpty(ds)) return ErrorJson("服务器目录设置不存在");
                    string serviceRoot = ds.Tables[0].Rows[0]["value"].ToString();


                    int action_id = 123;

                    string remark2 = "升级类型：" + type;
                    long logId = ActionLog.AddLog(conn, action_id, id, 2, userInfo.username, userInfo.id, remark2);

                    string fileName = SaveUpgradeFile(id, httpPostedFile, serviceRoot);

                    DeviceCommand devCommand = new DeviceCommand(this.tokenHex, id);
                    byte[] command = devCommand.CreateUpgradeCmd(type == "arm", (int)logId, fileName);
                    IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(Helper.GetLocalServIp()), Helper.GetLocalServPort());
                    JsonMsg<byte[]> msg = UdpHelper.SendCommand(command, iPEndPoint);

                    if (msg.code != 200)
                    {
                        ActionLog.Failed(conn, logId, msg.message);
                        return ErrorJson(msg.message);
                    }

                    return SuccessJson();

                }
            }
            catch (Exception ex)
            {

                return ErrorJson();
            }
        }

        private string SaveUpgradeFile(int deviceId, HttpPostedFile httpPostedFile, string serviceRoot)
        {
            string path = serviceRoot + (@"/upgrades");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            string extension = ".elite";
            int index = httpPostedFile.FileName.IndexOf('.');

            string fileName;
            if (index > 0)
            {
                fileName = deviceId.ToString() + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;
            }
            else
            {
                fileName = httpPostedFile.FileName + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;
            }

            path = path + "/" + fileName;

            httpPostedFile.SaveAs(path);

            return fileName;
        }

        [HttpGet]
        [Power("admin,manage")]
        [Route("api/devices/{id}/update/finishrate")]
        public IHttpActionResult Get(int id)
        {
            try
            {
                string redisKey = Helper.md5("device_upgrade_rate_" + id.ToString());
                string key = Helper.md5("device_upgrade_" + id.ToString());

                if (!RedisHelper.Exists(redisKey)) return SuccessJson(new { finish_rate = -1 });

                string finishRate = RedisHelper.Get(redisKey).ToString();

                if (finishRate.Equals("100"))
                {
                    RedisHelper.Remove(key);
                }

                return SuccessJson(new { finish_rate = Convert.ToInt32(finishRate) });
            }
            catch (Exception ex)
            {
                return ErrorJson(ex.Message);
            }
        }



        [HttpDelete]
        [Power("admin,manage")]
        [Route("api/devices/{id}/update")]
        public IHttpActionResult Delete(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();

            string type = GetString("type").ToLower();

            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入设备Id"));
            }
            if (string.IsNullOrEmpty(type))
            {
                throw new HttpResponseException(Error("请选择升级类型"));
            }


            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_device where id=@id and is_delete=0", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("设备不存在");
                DataRow row = ds.Tables[0].Rows[0];

                int action_id = 124;

                string remark2 = "强制停止升级";

                long logId = ActionLog.AddLog(conn, action_id, id, 0, userInfo.username, userInfo.id, remark2);

                try
                {
                    Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                    {
                        ReceiveTimeout = 1000
                    };

                    DeviceCommand devCommand = new DeviceCommand(this.tokenHex, id);
                    byte[] command = devCommand.CreateAbortUpgradeCmd(type == "arm");
                    IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(Helper.GetLocalServIp()), Helper.GetLocalServPort());
                    JsonMsg<byte[]> msg = UdpHelper.SendCommand(command, iPEndPoint, clientSocket);

                    if (msg.code != 200) return ErrorJson(msg.code, msg.message);

                    string redisKey = Helper.md5("device_upgrade_rate_" + id.ToString());
                    RedisHelper.Remove(redisKey);

                    string key = Helper.md5("device_upgrade_" + id.ToString());
                    RedisHelper.Remove(key);

                    Upgrade.ReportUpgradeRate(id, "-1");

                    ActionLog.Finished(conn, logId);
                    return SuccessJson();
                }
                catch (Exception ex)
                {
                    try
                    {
                        return ErrorJson(ex.Message);
                    }
                    catch (Exception en)
                    {
                        return ErrorJson(en.Message);
                    }

                }
            }
        }

    }
}
