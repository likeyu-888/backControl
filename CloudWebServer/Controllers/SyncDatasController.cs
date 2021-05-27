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
    public class SyncDatasController : BaseController
    {

        [HttpPut]
        [Power("210")]
        [Route("api/schools/{schoolId}/syncdata")]
        public IHttpActionResult Put(JObject obj)
        {
            GetRequest(obj);

            int school_id = GetSchoolId();

            if (school_id == 0)
            {
                throw new HttpResponseException(Error("请输入学校Id"));
            }
            if (!HasPower("209"))
            {
                if (!((IList)userInfo.schools.Split(',')).Contains(school_id.ToString()))
                {
                    throw new HttpResponseException(Error("您没有权限管理该学校"));
                }
            }


            HttpContextBase context = (HttpContextBase)Request.Properties["MS_HttpContext"];//获取传统context   
            Stream stream = context.Request.InputStream;
            stream.Position = 0;
            string requestData = "";
            using (var streamReader = new StreamReader(stream, Encoding.UTF8))
            {
                requestData = streamReader.ReadToEndAsync().Result;
                stream.Position = 0;
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();


                int action_id = 210;


                string remark2 = "学校数据同步";

                long logId = ActionLog.AddLog(conn, action_id, school_id, 0, 0, userInfo.username, userInfo.id, remark2);

                try
                {
                    DeviceCommand devCommand = new DeviceCommand(this.tokenHex, school_id, 0);
                    byte[] command = devCommand.CreateSyncDataCmd();
                    IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(Helper.GetLocalServIp()), Helper.GetLocalServPort());
                    JsonMsg<byte[]> msg = UdpHelper.SendCommand(command, iPEndPoint);

                    if (msg.code != 200) return ErrorJson(msg.code, msg.message);

                    ActionLog.Finished(conn, logId);
                    return SuccessJson();
                }
                catch (Exception ex)
                {
                    return ErrorJson(ex.Message);
                }
            }
        }

        [HttpPost]
        [Power("210")]
        [Route("api/syncdatas/{schoolId}/groups")]
        public IHttpActionResult PostGroups(JObject obj)
        {
            GetRequest(obj);

            int school_id = GetSchoolId();

            if (school_id == 0)
            {
                throw new HttpResponseException(Error("请输入学校Id"));
            }
            if (!HasPower("209"))
            {
                if (!((IList)userInfo.schools.Split(',')).Contains(school_id.ToString()))
                {
                    throw new HttpResponseException(Error("您没有权限管理该学校"));
                }
            }


            HttpContextBase context = (HttpContextBase)Request.Properties["MS_HttpContext"];//获取传统context   
            Stream stream = context.Request.InputStream;
            stream.Position = 0;
            string requestData = "";
            using (var streamReader = new StreamReader(stream, Encoding.UTF8))
            {
                requestData = streamReader.ReadToEndAsync().Result;
                stream.Position = 0;
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();


                int action_id = 210;


                string remark2 = "数据同步(分组)";

                long logId = ActionLog.AddLog(conn, action_id, school_id, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    DataSet ds = MySqlHelper.ExecuteDataset(conn, "select id from dev_group where school_id=" + school_id.ToString());
                    List<int> idList = new List<int>();
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        idList.Add(Convert.ToInt32(row["id"]));
                    }

                    StringBuilder sqlCommand = new StringBuilder("");

                    JObject jObject = JsonConvert.DeserializeObject<JObject>(requestData);
                    foreach (JObject row in jObject["list"])
                    {
                        int id = Convert.ToInt32(row["id"]);
                        string name = row["name"].ToString();
                        string remark = row["remark"].ToString();
                        string sort = row["sort"].ToString();

                        if (idList.Exists(t => t == id))
                        {
                            sqlCommand.Append(";update dev_group set " +
                                " name='" + name + "',remark='" + remark + "',sort=" + sort +
                                " where school_id=" + school_id.ToString() + " and id=" + id.ToString());
                        }
                        else
                        {
                            sqlCommand.Append(";insert into dev_group set " +
                                " name='" + name + "',remark='" + remark + "',sort=" + sort +
                                ",school_id=" + school_id.ToString() + ",id=" + id.ToString());
                        }
                        idList.Remove(id);
                    }
                    foreach (int tempId in idList)
                    {
                        sqlCommand.Append(";delete from dev_group where school_id=" + school_id.ToString() + " and id=" + tempId.ToString());
                    }

                    if (sqlCommand.Length > 0)
                    {
                        MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, sqlCommand.ToString());
                    }


                    transaction.Commit();

                    ActionLog.Finished(conn, logId);
                    return SuccessJson();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return ErrorJson(ex.Message);
                }
            }
        }


        [HttpPost]
        [Power("210")]
        [Route("api/syncdatas/{schoolId}/rooms")]
        public IHttpActionResult PostRooms(JObject obj)
        {
            GetRequest(obj);

            int school_id = GetSchoolId();

            if (school_id == 0)
            {
                throw new HttpResponseException(Error("请输入学校Id"));
            }
            if (!HasPower("209"))
            {
                if (!((IList)userInfo.schools.Split(',')).Contains(school_id.ToString()))
                {
                    throw new HttpResponseException(Error("您没有权限管理该学校"));
                }
            }


            HttpContextBase context = (HttpContextBase)Request.Properties["MS_HttpContext"];//获取传统context   
            Stream stream = context.Request.InputStream;
            stream.Position = 0;
            string requestData = "";
            using (var streamReader = new StreamReader(stream, Encoding.UTF8))
            {
                requestData = streamReader.ReadToEndAsync().Result;
                stream.Position = 0;
            }


            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();


                int action_id = 210;


                string remark2 = "数据同步(教室)";

                long logId = ActionLog.AddLog(conn, action_id, school_id, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    DataSet ds = MySqlHelper.ExecuteDataset(conn, "select id from sch_room where school_id=" + school_id.ToString());
                    List<int> idList = new List<int>();
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        idList.Add(Convert.ToInt32(row["id"]));
                    }

                    StringBuilder sqlCommand = new StringBuilder("update sch_room set is_delete=1 where school_id=" + school_id.ToString());

                    JObject jObject = JsonConvert.DeserializeObject<JObject>(requestData);
                    foreach (JObject row in jObject["list"])
                    {
                        int id = Convert.ToInt32(row["id"]);
                        string name = row["name"].ToString();
                        string remark = row["remark"].ToString();
                        string reverb_time = row["reverb_time"].ToString();
                        string sort = row["sort"].ToString();
                        string device_id = row["device_id"].ToString();

                        if (idList.Exists(t => t == id))
                        {
                            sqlCommand.Append(";update sch_room set " +
                                " name='" + name + "',remark='" + remark + "',sort=" + sort +
                                " ,reverb_time=" + reverb_time + ",device_id=" + device_id + ",is_delete=0 " +
                                " where school_id=" + school_id.ToString() + " and id=" + id.ToString());
                        }
                        else
                        {
                            sqlCommand.Append(";insert into sch_room set " +
                                " name='" + name + "',remark='" + remark + "',sort=" + sort +
                                " ,reverb_time=" + reverb_time + ",device_id=" + device_id + ",is_delete=0 " +
                                ",school_id=" + school_id.ToString() + ",id=" + id.ToString());
                        }
                    }

                    if (sqlCommand.Length > 0)
                    {
                        MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, sqlCommand.ToString());
                    }
                    transaction.Commit();

                    ActionLog.Finished(conn, logId);
                    return SuccessJson();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return ErrorJson(ex.Message);
                }
            }
        }


        [HttpPost]
        [Power("210")]
        [Route("api/syncdatas/{schoolId}/devices")]
        public IHttpActionResult PostDevices(JObject obj)
        {
            GetRequest(obj);

            int school_id = GetSchoolId();

            if (school_id == 0)
            {
                throw new HttpResponseException(Error("请输入学校Id"));
            }
            if (!HasPower("209"))
            {
                if (!((IList)userInfo.schools.Split(',')).Contains(school_id.ToString()))
                {
                    throw new HttpResponseException(Error("您没有权限管理该学校"));
                }
            }


            HttpContextBase context = (HttpContextBase)Request.Properties["MS_HttpContext"];//获取传统context   
            Stream stream = context.Request.InputStream;
            stream.Position = 0;
            string requestData = "";
            using (var streamReader = new StreamReader(stream, Encoding.UTF8))
            {
                requestData = streamReader.ReadToEndAsync().Result;
                stream.Position = 0;
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();


                int action_id = 210;


                string remark2 = "数据同步(设备)";

                long logId = ActionLog.AddLog(conn, action_id, school_id, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    DataSet ds = MySqlHelper.ExecuteDataset(conn, "select id from dev_device where school_id=" + school_id.ToString());
                    List<int> idList = new List<int>();
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        idList.Add(Convert.ToInt32(row["id"]));
                    }

                    StringBuilder sqlCommand = new StringBuilder("update dev_device set is_delete=1 where school_id=" + school_id.ToString());

                    JObject jObject = JsonConvert.DeserializeObject<JObject>(requestData);
                    foreach (JObject row in jObject["list"])
                    {

                        int id = Convert.ToInt32(row["id"]);
                        string name = row["name"].ToString();
                        string group_id = row["group_id"].ToString();
                        string status = row["status"].ToString();
                        string room_id = row["room_id"].ToString();
                        string is_auto_save = row["is_auto_save"].ToString();
                        string is_auto_record = row["is_auto_record"].ToString();
                        string sampling_rate = row["sampling_rate"].ToString();
                        string device_type = row["device_type"].ToString();
                        string snr = row["snr"].ToString();
                        string listen_efficiency = row["listen_efficiency"].ToString();
                        string attendence_difficulty = row["attendence_difficulty"].ToString();
                        string anbient_noice = row["anbient_noice"].ToString();
                        string ip = row["ip"].ToString();
                        string gateway = row["gateway"].ToString();
                        string mark = row["mark"].ToString();
                        string mac = row["mac"].ToString();
                        string dsp_version = row["dsp_version"].ToString();
                        string arm_version = row["arm_version"].ToString();

                        if (idList.Exists(t => t == id))
                        {
                            sqlCommand.Append(";update dev_device set " +
                                "name='" + name + "'," +
                                "group_id=" + group_id + "," +
                                "status=" + status + "," +
                                "room_id=" + room_id + "," +
                                "is_auto_save=" + is_auto_save + "," +
                                "is_auto_record=" + is_auto_record + "," +
                                "sampling_rate=" + sampling_rate + "," +
                                "device_type=" + device_type + "," +
                                "snr=" + snr + "," +
                                "listen_efficiency=" + listen_efficiency + "," +
                                "attendence_difficulty=" + attendence_difficulty + "," +
                                "anbient_noice=" + anbient_noice + "," +
                                "ip='" + ip + "'," +
                                 "gateway='" + gateway + "'," +
                                  "mark='" + mark + "'," +
                                   "mac='" + mac + "'," +
                                     "dsp_version='" + dsp_version + "'," +
                                       "arm_version='" + arm_version + "'," +
                                "is_delete=0 " +
                                " where school_id=" + school_id.ToString() + " and id=" + id.ToString());
                        }
                        else
                        {
                            sqlCommand.Append(";insert into dev_device set " +
                                "name='" + name + "'," +
                                "group_id=" + group_id + "," +
                                "status=" + status + "," +
                                "room_id=" + room_id + "," +
                                "is_auto_save=" + is_auto_save + "," +
                                "is_auto_record=" + is_auto_record + "," +
                                "sampling_rate=" + sampling_rate + "," +
                                "device_type=" + device_type + "," +
                                "snr=" + snr + "," +
                                "listen_efficiency=" + listen_efficiency + "," +
                                "attendence_difficulty=" + attendence_difficulty + "," +
                                "anbient_noice=" + anbient_noice + "," +
                                "ip='" + ip + "'," +
                                 "gateway='" + gateway + "'," +
                                  "mark='" + mark + "'," +
                                   "mac='" + mac + "'," +
                                     "dsp_version='" + dsp_version + "'," +
                                       "arm_version='" + arm_version + "'," +
                                "is_delete=0, " +
                                "school_id=" + school_id.ToString() + ",id=" + id.ToString());
                        }
                    }

                    if (sqlCommand.Length > 0)
                    {
                        MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, sqlCommand.ToString());
                    }

                    transaction.Commit();

                    ActionLog.Finished(conn, logId);
                    return SuccessJson();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return ErrorJson(ex.Message);
                }
            }
        }
    }
}
