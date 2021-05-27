using Elite.WebServer.Base;
using Elite.WebServer.Services;
using Elite.WebServer.Utility;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Web.Http;

namespace Elite.WebServer.Controllers
{
    public class MonitorsController : BaseController
    {

        [HttpPut]
        [Power("122")]
        [Route("api/devices/{schoolId}/{id}/listening")]
        public IHttpActionResult Put(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();
            int status = GetInt("status", -1);
            int channel = GetInt("channel", -1);
            int school_id = GetSchoolId();

            if (school_id == 0)
            {
                throw new HttpResponseException(Error("请输入学校Id"));
            }
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入设备Id"));
            }

            if (status == -1)
            {
                throw new HttpResponseException(Error("请输入监听状态"));
            }
            if (channel == -1)
            {
                throw new HttpResponseException(Error("请输入监听通道" + status + channel));
            }
            if (!HasPower("209"))
            {
                if (!((IList)userInfo.schools.Split(',')).Contains(school_id.ToString()))
                {
                    throw new HttpResponseException(Error("您没有权限管理该学校"));
                }
            }

            string key = "device_monitor_" + id.ToString();
            if (status == 1)
            {
                if (RedisHelper.Exists(key))
                {
                    throw new HttpResponseException(Error("该设备当前已处于监听状态"));
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

                int action_id = 122;

                string remark2 = status == 1 ? "开始" : "停止";

                long logId = ActionLog.AddLog(conn, action_id, school_id, id, 0, userInfo.username, userInfo.id, remark2);

                try
                {
                    DeviceCommand devCommand = new DeviceCommand(this.tokenHex, school_id, id);
                    byte[] command = devCommand.CreateMonitorCmd(channel, status == 1 ? true : false);
                    IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(Helper.GetLocalServIp()), Helper.GetLocalServPort());
                    JsonMsg<byte[]> msg = UdpHelper.SendCommand(command, iPEndPoint);

                    if (msg.code != 200) return ErrorJson(msg.code, msg.message);

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
