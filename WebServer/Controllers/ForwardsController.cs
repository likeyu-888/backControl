using Elite.WebServer.Base;
using Elite.WebServer.Services;
using Elite.WebServer.Utility;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Text;
using System.Web.Http;

namespace Elite.WebServer.Controllers
{
    public class ForwardsController : BaseController
    {

        [HttpPost]
        [Route("api/devices/{id}/forward")]
        public IHttpActionResult Put(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();
            string info = GetString("info");

            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入设备Id"));
            }

            if (string.IsNullOrEmpty(info))
            {
                throw new HttpResponseException(Error("数据数据不得为空"));
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

                int action_id = 199;

                string remark2 = "info=" + (info.Length > 100 ? info.Substring(0, 100) : info);

                long logId = ActionLog.AddLog(conn, action_id, id, 0, userInfo.username, userInfo.id, remark2);

                string[] infoArr = info.Split(',');
                byte[] infoBytes = new byte[infoArr.Length];
                int i = 0;
                foreach (string str in infoArr)
                {
                    if (!DataValidate.IsInteger(str)) return ErrorJson("输入的数据每一位均应为数字");
                    infoBytes[i++] = Convert.ToByte(str);
                }

                try
                {
                    DeviceCommand devCommand = new DeviceCommand(this.tokenHex, id);
                    IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(Helper.GetLocalServIp()), Helper.GetLocalServPort());

                    byte[] command = devCommand.CreateCommonForwardCmd(infoBytes);

                    JsonMsg<byte[]> msg = UdpHelper.SendCommand(command, iPEndPoint);

                    if (msg.code != 200) return ErrorJson(msg.code, msg.message);

                    StringBuilder resultBuilder = new StringBuilder();
                    foreach (byte by in msg.data)
                    {
                        resultBuilder.Append(",").Append(Convert.ToInt16(by));
                    }
                    string result = resultBuilder.ToString().Substring(1);
                    ActionLog.Finished(conn, logId);
                    return SuccessJson(new { info = result });
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
