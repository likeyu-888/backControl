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
using System.Text;
using System.Web.Http;

namespace Elite.WebServer.Controllers
{
    public class ConfigsController : BaseController
    {
        [HttpGet]
        [Power("912")]
        [Route("api/configs")]
        public IHttpActionResult Get(
            string dict = ""
            )
        {

            DataSet ds = null;
            Hashtable dictHashtable;

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                String columns = "itm.item_key,itm.value";

                StringBuilder commandText = new StringBuilder("select  " + columns +
                    "  from sys_config_item  itm" +
                    " where itm.is_delete=0 and itm.is_show=1 and itm.group_id=1001 " +
                    " order by itm.sort");
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                dictHashtable = GetDict(conn, dict);
                Dictionary<string, string> detail = new Dictionary<string, string>();

                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    detail.Add(row["item_key"].ToString(), row["value"].ToString());
                }

                var result = new { detail, dict = dictHashtable };

                return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(result));
            }

        }


        [HttpPut]
        [Power("911")]
        [Route("api/configs")]
        public IHttpActionResult Put(JObject obj)
        {
            GetRequest(obj);

            string cloud_server_ip = GetString("cloud_server_ip");
            int cloud_server_port = GetInt("cloud_server_port", 0);
            int token_expire_seconds = GetInt("token_expire_seconds", 0);
            string sync_time = GetString("sync_time", "23:00");

            int is_debug = GetInt("is_debug", 0);


            if (string.IsNullOrEmpty(cloud_server_ip))
            {
                throw new HttpResponseException(Error("请输入云平台服务Ip地址"));
            }
            if (cloud_server_port == 0)
            {
                throw new HttpResponseException(Error("请输入云平台服务端口"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();


                int action_id = 911;
                string remark2 = "";
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update sys_config_item set ");
                    commandText.Append(" value=@value ");
                    commandText.Append(" where item_key=@key");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@key", "cloud_server_ip"));
                    parameters.Add(new MySqlParameter("@value", cloud_server_ip));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@key", "cloud_server_port"));
                    parameters.Add(new MySqlParameter("@value", cloud_server_port.ToString()));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@key", "token_expire_seconds"));
                    parameters.Add(new MySqlParameter("@value", token_expire_seconds));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@key", "is_debug"));
                    parameters.Add(new MySqlParameter("@value", is_debug.ToString()));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@key", "sync_time"));
                    parameters.Add(new MySqlParameter("@value", sync_time.ToString()));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                    transaction.Commit();
                    ActionLog.Finished(conn, logId);
                }
                catch (Exception ex)
                {
                    try
                    {
                        transaction.Rollback();
                        return ErrorJson(ex.Message);
                    }
                    catch (Exception en)
                    {
                        return ErrorJson(en.Message);
                    }

                }

            }

            return SuccessJson();
        }
    }
}
