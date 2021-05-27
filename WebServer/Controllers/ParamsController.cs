using Elite.WebServer.Base;
using Elite.WebServer.Services;
using Elite.WebServer.Utility;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Http;

namespace Elite.WebServer.Controllers
{
    public class ParamsController : BaseController
    {

        [HttpPut]
        [Power("admin,manage")]
        [Route("api/devices/{id}/params")]
        public IHttpActionResult Put(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();

            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入设备Id"));
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

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_device where id=@id and is_delete=0", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("设备不存在");
                DataRow row = ds.Tables[0].Rows[0];

                var dObject = JsonConvert.DeserializeObject<dynamic>(requestData);

                if (!DataValidate.IsIp(dObject.arm.ip.ToString()))
                {
                    return ErrorJson("请输入正确的Ip地址");
                }
                if (!DataValidate.IsIp(dObject.arm.mark.ToString()))
                {
                    return ErrorJson("请输入正确的子网掩码");
                }
                if (!DataValidate.IsIp(dObject.arm.gateway.ToString()))
                {
                    return ErrorJson("请输入正确的默认网关");
                }
                if (!DataValidate.IsIp(dObject.arm.server_ip.ToString()))
                {
                    return ErrorJson("请输入正确的服务器Ip");
                }

                int action_id = 121;


                string remark2 = "修改设备参数";

                long logId = ActionLog.AddLog(conn, action_id, id, 0, userInfo.username, userInfo.id, remark2);

                try
                {
                    float reverbTimeNum = dObject.reverb_time;


                    byte[] reverbTime = BitConverter.GetBytes(reverbTimeNum);

                    byte[] buff = null;

                    switch (Convert.ToInt32(row["device_type"]))
                    {
                        case 4:
                            Services.Version4.DeviceInfoHex infoJson4 = new Services.Version4.DeviceInfoHex();
                            buff = infoJson4.GetHexFromJson(dObject);
                            Array.Copy(reverbTime, 0, buff, 496, reverbTime.Length);
                            break;
                        case 5:
                            Services.Version5.DeviceInfoHex infoJson5 = new Services.Version5.DeviceInfoHex();
                            buff = infoJson5.GetHexFromJson(dObject);
                            Array.Copy(reverbTime, 0, buff, 608, reverbTime.Length);
                            break;
                    }


                    DeviceCommand devCommand = new DeviceCommand(this.tokenHex, id);
                    byte[] command = devCommand.CreateUpdateParamsCmd(buff);
                    IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(Helper.GetLocalServIp()), Helper.GetLocalServPort());
                    JsonMsg<byte[]> msg = UdpHelper.SendCommand(command, iPEndPoint);

                    if (msg.code != 200) return ErrorJson(msg.code, msg.message);

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@reverb_time", reverbTimeNum));
                    parameters.Add(new MySqlParameter("@id", id));

                    MySqlHelper.ExecuteNonQuery(conn, "update sch_room set reverb_time=@reverb_time where device_id=@id", parameters.ToArray());

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
