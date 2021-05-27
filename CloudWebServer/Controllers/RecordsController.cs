using Elite.WebServer.Base;
using Elite.WebServer.Services;
using Elite.WebServer.Utility;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Http;

namespace Elite.WebServer.Controllers
{
    public class RecordsController : BaseController
    {
        [HttpGet]
        [Power("133")]
        [Route("api/devices/{schoolId}/{id}/records")]
        public IHttpActionResult Get(int id = 0,
            string create_time = "",
            int page = 1,
            int page_size = 10,
            string sort_column = "id",
            string sort_direction = "DESC",
            string dict = ""
            )
        {
            int school_id = GetSchoolId();
            if (!HasPower("209"))
            {
                if (!((IList)userInfo.schools.Split(',')).Contains(school_id.ToString()))
                {
                    throw new HttpResponseException(Error("您没有权限管理该学校"));
                }
            }

            Hashtable dictHashtable;
            try
            {



                if (string.IsNullOrEmpty(create_time)) create_time = "";

                JObject result = null;

                using (conn = new MySqlConnection(Constr()))
                {
                    conn.Open();

                    dictHashtable = GetDict(conn, dict, school_id);

                    Dictionary<int, dynamic> deviceDict = new Dictionary<int, dynamic>();

                    DataSet devices = MysqlHelper.ExecuteDataSet(conn, CommandType.Text, "select id,name,ip from dev_device where school_id=" + school_id.ToString());
                    if (!DsEmpty(devices))
                    {
                        foreach (DataRow row in devices.Tables[0].Rows)
                        {
                            dynamic device = new
                            {
                                name = row["name"].ToString(),
                                ip = row["ip"].ToString()
                            };
                            deviceDict.Add((int)row["id"], device);
                        }
                    }


                    DeviceCommand devCommand = new DeviceCommand(this.tokenHex, school_id, id);
                    byte[] command = devCommand.CreateQueryRecordCmd(id, create_time, page, page_size, sort_column, sort_direction);

                    IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(Helper.GetLocalServIp()), Helper.GetLocalServPort());

                    JsonMsg<byte[]> msg = UdpHelper.SendCommand(command, iPEndPoint);

                    if (msg.code != 200)
                    {
                        PagedData<JArray> pagedData2 = new PagedData<JArray>();
                        pagedData2.total = 0;
                        pagedData2.page = page;
                        pagedData2.page_size = page_size;
                        pagedData2.page_count = 0;
                        pagedData2.list = new JArray();
                        if (dictHashtable != null)
                        {
                            pagedData2.dict = dictHashtable;
                        }

                        JsonMsg<PagedData<JArray>> jsonMsg2 = new JsonMsg<PagedData<JArray>>();

                        jsonMsg2.code = 200;
                        jsonMsg2.message = "操作失败:" + msg.message;
                        jsonMsg2.data = pagedData2;
                        return Content(HttpStatusCode.OK, jsonMsg2);
                    }

                    string temp = Encoding.UTF8.GetString(msg.data);

                    result = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(msg.data)) as dynamic;

                    if (!result.ContainsKey("code")) return ErrorJson("异常错误");

                    JArray array = result["data"]["list"] as JArray;

                    int deviceId = 0;
                    for (int i = 0; i < array.Count; i++)
                    {
                        deviceId = (int)array[i]["device_id"];
                        if (deviceDict.ContainsKey(deviceId))
                        {
                            array[i]["ip"] = deviceDict[deviceId].ip;
                            array[i]["name"] = deviceDict[deviceId].name;
                        }
                    }


                    PagedData<JArray> pagedData = new PagedData<JArray>();
                    pagedData.total = Convert.ToInt32(result["data"]["total"]);
                    pagedData.page = Convert.ToInt32(result["data"]["page"]);
                    pagedData.page_size = Convert.ToInt32(result["data"]["page_size"]);
                    pagedData.page_count = Convert.ToInt32(result["data"]["page_count"]);
                    pagedData.list = array;
                    if (dictHashtable != null)
                    {
                        pagedData.dict = dictHashtable;
                    }

                    JsonMsg<PagedData<JArray>> jsonMsg = new JsonMsg<PagedData<JArray>>();

                    jsonMsg.code = Convert.ToInt32(result["code"]);
                    jsonMsg.message = result["message"].ToString();
                    jsonMsg.data = pagedData;

                    return Content(HttpStatusCode.OK, jsonMsg);
                }
            }
            catch (Exception ex)
            {
                return ErrorJson(ex.Message);
            }
        }

        [HttpGet]
        [Power("132")]
        [Route("api/devices/records/{schoolId}/{id}/{recordId}")]
        public IHttpActionResult DownRecord()
        {

            int id = GetId();
            int schoolId = GetSchoolId();
            int recordId = GetUriParamsInt("recordId");

            if (schoolId == 0)
            {
                throw new HttpResponseException(Error("请输入学校Id"));
            }
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入设备Id"));
            }
            if (recordId == 0)
            {
                throw new HttpResponseException(Error("请输入文件Id"));
            }

            if (!HasPower("209"))
            {
                if (!((IList)userInfo.schools.Split(',')).Contains(schoolId.ToString()))
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
                parameters.Add(new MySqlParameter("@school_id", schoolId));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_device where school_id=@school_id and id=@id and is_delete=0", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("设备不存在");
                DataRow row = ds.Tables[0].Rows[0];

                int action_id = 132;

                string remark2 = "设备Id:" + id.ToString() + ",文件Id:" + recordId.ToString();

                long logId = ActionLog.AddLog(conn, action_id, schoolId, id, 0, userInfo.username, userInfo.id, remark2);

                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("select * from dev_record where ");
                    commandText.Append("school_id=@school_id and ");
                    commandText.Append("device_id=@device_id and ");
                    commandText.Append("id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@school_id", schoolId));
                    parameters.Add(new MySqlParameter("@device_id", id));
                    parameters.Add(new MySqlParameter("@id", recordId));

                    ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                    if (!DsEmpty(ds))
                    {

                        string filePath = ds.Tables[0].Rows[0]["file_path"].ToString();
                        string physicalPath = System.Web.Hosting.HostingEnvironment.MapPath(filePath);

                        if (File.Exists(physicalPath))
                        {
                            ActionLog.Finished(conn, logId);
                            return SuccessJson(new { finish = 1 });
                        }
                    }

                    string key = Helper.md5("download_" + schoolId.ToString() + "_" + recordId.ToString());
                    if (RedisHelper.Exists(key))
                    {
                        ActionLog.Finished(conn, logId);
                        return SuccessJson(new { finish = 0 });
                    }

                    DeviceCommand devCommand = new DeviceCommand(this.tokenHex, schoolId, id);

                    byte[] command = devCommand.CreateDownRecordCmd(recordId);

                    IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(Helper.GetLocalServIp()), Helper.GetLocalServPort());

                    JsonMsg<byte[]> msg = UdpHelper.SendCommand(command, iPEndPoint);

                    if (msg.code != 200)
                    {

                        return ErrorJson(msg.code, msg.message);
                    }

                    RedisHelper.Set(key, 1, 20); //预计20分钟传输完成，200K/s

                    ActionLog.Finished(conn, logId);
                    return SuccessJson(new { finish = 0 });
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



        [HttpGet]
        [Power("132")]
        [Route("api/devices/records/{schoolId}/{id}/{recordId}/finish")]
        public IHttpActionResult GetFinish()
        {
            int id = GetId();
            int schoolId = GetSchoolId();
            int recordId = GetUriParamsInt("recordId");

            if (schoolId == 0)
            {
                throw new HttpResponseException(Error("请输入学校Id"));
            }
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入设备Id"));
            }
            if (recordId == 0)
            {
                throw new HttpResponseException(Error("请输入文件Id"));
            }
            if (!HasPower("209"))
            {
                if (!((IList)userInfo.schools.Split(',')).Contains(schoolId.ToString()))
                {
                    throw new HttpResponseException(Error("您没有权限管理该学校"));
                }
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();


                StringBuilder commandText = new StringBuilder();
                commandText.Append("select * from dev_record where ");
                commandText.Append("school_id=@school_id and ");
                commandText.Append("device_id=@device_id and ");
                commandText.Append("id=@id");

                parameters.Clear();
                parameters.Add(new MySqlParameter("@school_id", schoolId));
                parameters.Add(new MySqlParameter("@device_id", id));
                parameters.Add(new MySqlParameter("@id", recordId));

                DataSet ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                if (!DsEmpty(ds))
                {
                    string key = Helper.md5("download_" + schoolId.ToString() + "_" + recordId.ToString());
                    RedisHelper.Remove(key);
                    return SuccessJson(new { finish = 1 });
                }
                else
                {
                    return SuccessJson(new { finish = 0 });
                }
            }
        }

        [HttpPost]
        [Power("134")]
        [Route("api/devices/records/{schoolId}/{id}/{recordId}")]
        public IHttpActionResult UploadRecord()
        {
            try
            {

                GetRequest();

                int schoolId = GetUriParamsInt("schoolId");
                int id = GetUriParamsInt("id");
                int recordId = GetUriParamsInt("recordId");

                if (schoolId == 0)
                {
                    return ErrorJson("请输入学校Id");
                }
                if (id == 0)
                {
                    return ErrorJson("请输入设备Id");
                }
                if (recordId == 0)
                {
                    return ErrorJson("请输入文件Id");
                }

                if (!HasPower("209"))
                {
                    if (!((IList)userInfo.schools.Split(',')).Contains(schoolId.ToString()))
                    {
                        return ErrorJson("您没有权限管理该学校");
                    }
                }

                HttpRequest request = HttpContext.Current.Request;
                HttpFileCollection fileCollection = request.Files;

                if (fileCollection.Count <= 0)
                {
                    return ErrorJson("请上传文件");
                }

                HttpPostedFile httpPostedFile = fileCollection[0];


                string path = System.Web.Hosting.HostingEnvironment.MapPath(@"/records");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                path = path + "/" + schoolId.ToString();
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                path = path + "/" + id.ToString();
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                path = path + "/" + DateTime.Now.ToString("yyyyMM");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                string file_path = "/records/" + schoolId.ToString() + "/" + id.ToString() + "/" + DateTime.Now.ToString("yyyyMM") + "/" + httpPostedFile.FileName;

                string physicalPath = path + "/" + httpPostedFile.FileName;

                httpPostedFile.SaveAs(physicalPath);

                using (MySqlConnection conn = new MySqlConnection(Constr()))
                {
                    conn.Open();

                    if (!MysqlHelper.Exists(conn, "select count(*) from dev_record where school_id=" + schoolId.ToString() + " and id=" + recordId.ToString()))
                    {
                        string query = "insert into dev_record " +
                       " set device_id=" + id.ToString() + "," +
                       " school_id=" + schoolId.ToString() + "," +
                       " id=" + recordId.ToString() + "," +
                       "size=" + Math.Ceiling((decimal)httpPostedFile.ContentLength / 1024).ToString() + "," +
                       "file_path='" + file_path + "'";

                        MySqlHelper.ExecuteNonQuery(conn, query);
                    }

                    conn.Close();
                }

                return SuccessJson();
            }
            catch (Exception ex)
            {
                return ErrorJson(ex.Message);
            }

        }



    }
}
