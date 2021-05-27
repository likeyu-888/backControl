using Elite.WebServer.Base;
using Elite.WebServer.Services;
using Elite.WebServer.Utility;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Web.Http;

namespace Elite.WebServer.Controllers
{
    public class MonitorsController : BaseController
    {

        [HttpPut]
        [Power("admin,manage")]
        [Route("api/devices/{id}/listening")]
        public IHttpActionResult Put(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();
            int status = GetInt("status", -1);
            int channel = GetInt("channel", -1);

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
                throw new HttpResponseException(Error("请输入监听通道"));
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
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_device where id=@id and is_delete=0", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("设备不存在");
                DataRow row = ds.Tables[0].Rows[0];

                int action_id = 122;

                string remark2 = status == 1 ? "开始" : "停止";


                long logId = ActionLog.AddLog(conn, action_id, id, 0, userInfo.username, userInfo.id, remark2);

                try
                {
                    DeviceCommand devCommand = new DeviceCommand(this.tokenHex, id);
                    byte[] command = devCommand.CreateMonitorCmd(channel, status == 1 ? true : false, Convert.ToInt32(row["is_auto_record"]) == 1, ClientInfo.GetRealIp);
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
