using Elite.WebServer.Base;
using Elite.WebServer.Services;
using Elite.WebServer.Utility;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Web;
using System.Web.Http;

namespace Elite.WebServer.Controllers
{
    public class UpgradesController : BaseController
    {
        [HttpPut]
        [HttpPost]
        [Power("123")]
        [Route("api/devices/{schoolId}/{id}/update")]
        public IHttpActionResult Post()
        {
            GetRequest();

            int id = GetId();
            int school_id = GetSchoolId();
            string type = GetString("type").ToLower();

            if (school_id == 0)
            {
                throw new HttpResponseException(Error("请输入学校Id"));
            }
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入设备Id"));
            }
            if (string.IsNullOrEmpty(type))
            {
                throw new HttpResponseException(Error("请选择升级类型"));
            }
            if (!HasPower("209"))
            {
                if (!((IList)userInfo.schools.Split(',')).Contains(school_id.ToString()))
                {
                    throw new HttpResponseException(Error("您没有权限管理该学校"));
                }
            }

            HttpRequest request = HttpContext.Current.Request;
            HttpFileCollection fileCollection = request.Files;

            if (fileCollection.Count <= 0)
            {
                throw new HttpResponseException(Error("请上传文件"));
            }

            HttpPostedFile httpPostedFile = fileCollection[0];


            string key = Helper.md5("device_upgrade_" + school_id.ToString() + "_" + id.ToString());

            string rateKey = Helper.md5("device_upgrade_rate_" + school_id.ToString() + "_" + id.ToString());

            if (RedisHelper.Exists(key))
            {
                throw new HttpResponseException(Error("该设备另一个升级操作正在进行中."));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                parameters.Add(new MySqlParameter("@school_id", school_id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_device where school_id=@school_id and id=@id and is_delete=0", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("设备不存在");
                DataRow row = ds.Tables[0].Rows[0];

                byte[] file = new byte[httpPostedFile.ContentLength];
                byte[] data = new byte[httpPostedFile.ContentLength - 1024];
                byte[] header = new byte[1024];

                Stream fileStream = httpPostedFile.InputStream;
                fileStream.Read(file, 0, file.Length);
                Array.Copy(file, 0, header, 0, header.Length);
                Array.Copy(file, 1024, data, 0, data.Length);

                bool isValid = Upgrade.IsValidFile(type, httpPostedFile.FileName, header, data, Convert.ToInt32(row["device_type"]));
                if (!isValid) return ErrorJson("所选升级文件有误，请重新选择");

                int action_id = 123;

                string remark2 = "升级类型：" + type;

                long logId = ActionLog.AddLog(conn, action_id, school_id, id, 2, userInfo.username, userInfo.id, remark2);

                try
                {
                    string path = System.Web.Hosting.HostingEnvironment.MapPath(@"/upgrades");
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    path = path + "/" + school_id.ToString();
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    string extension = Path.GetExtension(httpPostedFile.FileName);

                    string fileName = "";
                    int index = httpPostedFile.FileName.IndexOf('.');
                    if (index > 0)
                    {
                        fileName = httpPostedFile.FileName.Substring(0, index) + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;
                    }
                    else
                    {
                        fileName = httpPostedFile.FileName + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;
                    }

                    path = path + "/" + fileName;

                    httpPostedFile.SaveAs(path);

                    RedisHelper.Set(key, path);

                    string logKey = Helper.md5("device_upgrade_log_" + school_id.ToString() + "_" + id.ToString());
                    RedisHelper.Set(logKey, (int)logId, 10);

                    RedisHelper.Set(rateKey, 0, 10);

                    DeviceCommand devCommand = new DeviceCommand(this.tokenHex, school_id, id);
                    byte[] command = devCommand.CreateUpgradeCmd(type == "arm", (int)logId, key);
                    IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(Helper.GetLocalServIp()), Helper.GetLocalServPort());
                    JsonMsg<byte[]> msg = UdpHelper.SendCommand(command, iPEndPoint);

                    if (msg.code != 200)
                    {
                        ActionLog.Failed(conn, logId);
                        RedisHelper.Remove(key);
                        RedisHelper.Remove(rateKey);
                        return ErrorJson(msg.code, msg.message);
                    }
                    //成功后将通过接口0xb4修改状态
                    return SuccessJson();
                }
                catch (Exception ex)
                {
                    try
                    {
                        RedisHelper.Remove(rateKey);
                        RedisHelper.Remove(key);
                        return ErrorJson(ex.Message);
                    }
                    catch (Exception en)
                    {
                        return ErrorJson(en.Message);
                    }
                }
            }
        }

        /// <summary>
        /// 供学校端调整用，升级成功后修改日志状态
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Power("125")]
        [Route("api/devices/{schoolId}/{deviceId}/finishrate")]
        public IHttpActionResult UpdateFinishRate(JObject obj)
        {

            GetRequest(obj);
            int deviceId = GetUriParamsInt("deviceId");
            decimal rate = GetDecimal("finish_rate", 0);

            int school_id = GetUriParamsInt("schoolId");

            if (deviceId == 0)
            {
                throw new HttpResponseException(Error("请输入设备Id"));
            }

            if (!HasPower("209"))
            {
                if (!((IList)userInfo.schools.Split(',')).Contains(school_id.ToString()))
                {
                    throw new HttpResponseException(Error("您没有权限管理该学校"));
                }
            }

            int logId = 0;
            string logKey = Helper.md5("device_upgrade_log_" + school_id.ToString() + "_" + deviceId.ToString());
            if (RedisHelper.Exists(logKey))
            {
                logId = RedisHelper.Get<int>(logKey);
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                if (logId > 0)
                {

                    List<MySqlParameter> parameters = new List<MySqlParameter>();
                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@id", logId));
                    DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from log_action where id=@id", parameters.ToArray());
                    if (DsEmpty(ds))
                    {
                        return ErrorJson("升级记录不存在");
                    }

                    string upgradeKey = Helper.md5("device_upgrade_" + school_id.ToString() + "_" + deviceId.ToString());
                    string key = Helper.md5("device_upgrade_rate_" + school_id.ToString() + "_" + deviceId.ToString());

                    if (rate == 100)
                    {
                        RedisHelper.Remove(upgradeKey);
                        RedisHelper.Set(key, 100, 1);

                        parameters.Clear();
                        parameters.Add(new MySqlParameter("@id", logId));
                        parameters.Add(new MySqlParameter("@status", 1));
                        MySqlHelper.ExecuteNonQuery(conn, "update log_action set status=@status where id=@id and action_id=123", parameters.ToArray());
                        MySqlHelper.ExecuteNonQuery(conn, "update dev_device set status=101 where school_id=" + school_id.ToString() + " and id=" + deviceId.ToString(), parameters.ToArray());
                    }
                    else if (rate == -1)
                    {
                        RedisHelper.Remove(upgradeKey);
                        RedisHelper.Remove(key);

                        parameters.Clear();
                        parameters.Add(new MySqlParameter("@id", logId));
                        int status = 1;
                        parameters.Add(new MySqlParameter("@status", status));
                        MySqlHelper.ExecuteNonQuery(conn, "update log_action set status=@status where id=@id and action_id=123", parameters.ToArray());
                    }
                    else
                    {
                        RedisHelper.Set(key, rate);
                    }
                }
                else
                {
                    List<MySqlParameter> parameters = new List<MySqlParameter>();
                    parameters.Clear();

                    string upgradeKey = Helper.md5("device_upgrade_" + school_id.ToString() + "_" + deviceId.ToString());
                    string key = Helper.md5("device_upgrade_rate_" + school_id.ToString() + "_" + deviceId.ToString());

                    if (!RedisHelper.Exists(upgradeKey))
                    {
                        RedisHelper.Set(upgradeKey, "", 5);
                    }

                    if (rate == 100)
                    {
                        RedisHelper.Remove(upgradeKey);
                        RedisHelper.Set(key, 100, 1);

                        parameters.Clear();
                        parameters.Add(new MySqlParameter("@id", logId));
                        parameters.Add(new MySqlParameter("@status", 1));
                        MySqlHelper.ExecuteNonQuery(conn, "update dev_device set status=101 where school_id=" + school_id.ToString() + " and id=" + deviceId.ToString(), parameters.ToArray());
                    }
                    else if (rate == -1)
                    {
                        RedisHelper.Remove(upgradeKey);
                        RedisHelper.Remove(key);
                    }
                    else
                    {
                        RedisHelper.Set(key, rate);
                    }
                }

            }
            return SuccessJson();
        }

        [HttpGet]
        [Route("api/devices/{schoolId}/{id}/update/finishrate")]
        public IHttpActionResult Get()
        {
            int school_id = GetUriParamsInt("schoolId");
            int id = GetUriParamsInt("id");

            if (!HasPower("209"))
            {
                if (!((IList)userInfo.schools.Split(',')).Contains(school_id.ToString()))
                {
                    throw new HttpResponseException(Error("您没有权限管理该学校"));
                }
            }

            string key = Helper.md5("device_upgrade_" + school_id.ToString() + "_" + id.ToString());
            string redisKey = Helper.md5("device_upgrade_rate_" + school_id.ToString() + "_" + id.ToString());

            if (!RedisHelper.Exists(redisKey)) return SuccessJson(new { finish_rate = -1 });

            string finishRate = RedisHelper.Get(redisKey).ToString();

            if (finishRate.Equals("100"))
            {
                RedisHelper.Remove(key);
                RedisHelper.Remove(redisKey);
            }

            return SuccessJson(new { finish_rate = Convert.ToInt32(finishRate) });
        }



        [HttpDelete]
        [Power("124")]
        [Route("api/devices/{schoolId}/{id}/update")]
        public IHttpActionResult Delete(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();
            int school_id = GetSchoolId();

            string type = GetString("type").ToLower();


            if (school_id == 0)
            {
                throw new HttpResponseException(Error("请输入学校Id"));
            }
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入设备Id"));
            }
            if (string.IsNullOrEmpty(type))
            {
                throw new HttpResponseException(Error("请选择升级类型"));
            }
            if (!HasPower("209"))
            {
                if (!((IList)userInfo.schools.Split(',')).Contains(school_id.ToString()))
                {
                    throw new HttpResponseException(Error("您没有权限管理该学校"));
                }
            }


            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                parameters.Add(new MySqlParameter("@school_id", school_id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_device where school_id=@school_id and id=@id and is_delete=0", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("设备不存在");
                DataRow row = ds.Tables[0].Rows[0];

                int action_id = 124;

                string remark2 = "强制停止升级";

                long logId = ActionLog.AddLog(conn, action_id, school_id, id, 0, userInfo.username, userInfo.id, remark2);

                try
                {
                    Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                    {
                        ReceiveTimeout = 1000
                    };

                    DeviceCommand devCommand = new DeviceCommand(this.tokenHex, school_id, id);
                    byte[] command = devCommand.CreateAbortUpgradeCmd(type == "arm");
                    IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(Helper.GetLocalServIp()), Helper.GetLocalServPort());
                    JsonMsg<byte[]> msg = UdpHelper.SendCommand(command, iPEndPoint, clientSocket);

                    if (msg.code != 200) return ErrorJson(msg.code, msg.message);

                    string redisKey = Helper.md5("device_upgrade_rate_" + school_id.ToString() + "_" + id.ToString());
                    RedisHelper.Remove(redisKey);

                    string key = Helper.md5("device_upgrade_" + school_id.ToString() + "_" + id.ToString());

                    RedisHelper.Remove(key);

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

        /// <summary>
        /// 客户端下载升级文件
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Power("123")]
        [Route("api/devices/{schoolId}/{redisKey}/update")]
        public HttpResponseMessage GetUpgradeFile()
        {
            string redisKey = GetUriParamsStr("redisKey");
            int school_id = GetSchoolId();

            if (school_id == 0)
            {
                throw new HttpResponseException(Error("请输入学校Id"));
            }
            if (string.IsNullOrEmpty(redisKey))
            {
                throw new HttpResponseException(Error("请输入升级文件唯一编号:" + redisKey));
            }

            if (!RedisHelper.Exists(redisKey))
            {
                throw new HttpResponseException(Error("升级文件唯一编号不存在"));
            }
            if (!HasPower("209"))
            {
                if (!((IList)userInfo.schools.Split(',')).Contains(school_id.ToString()))
                {
                    throw new HttpResponseException(Error("您没有权限管理该学校"));
                }
            }

            string physicalPath = RedisHelper.Get<string>(redisKey);

            string file = Path.GetFileName(physicalPath);

            if (!File.Exists(physicalPath)) throw new HttpResponseException(Error("升级文件不存在"));

            var stream = new FileStream(physicalPath, FileMode.Open);
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = file
            };

            return response;
        }

    }
}
