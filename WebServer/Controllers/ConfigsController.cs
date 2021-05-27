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
        [Power("admin")]
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
                detail["reg_password"] = "";
                var result = new { detail, dict = dictHashtable };

                return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(result));
            }

        }


        [HttpPut]
        [Power("admin")]
        [Route("api/configs")]
        public IHttpActionResult Put(JObject obj)
        {
            GetRequest(obj);

            string school_name = GetString("school_name");
            int school_id = GetInt("school_id", 0);
            int auto_save_interval = GetInt("auto_save_interval", 0);
            int save_wave_interval = GetInt("save_wave_interval", 0);
            int token_expire_seconds = GetInt("token_expire_seconds", 20 * 60);

            string reg_password = GetString("reg_password");
            string service_root = GetString("service_root");
            string sync_time = GetString("sync_time", "23:00");

            int is_debug = GetInt("is_debug", 0);




            if (string.IsNullOrEmpty(school_name))
            {
                throw new HttpResponseException(Error("请输入学校名称"));
            }
            if (auto_save_interval % 60 != 0)
            {
                throw new HttpResponseException(Error("保存声学数据频率应是60秒的倍数"));
            }
            if (save_wave_interval % (60) != 0)
            {
                throw new HttpResponseException(Error("保存录音频率应是60秒的倍数"));
            }
            if (string.IsNullOrEmpty(service_root))
            {
                throw new HttpResponseException(Error("请输入服务器目录"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();


                int action_id = 911;
                string remark2 = "";
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update sys_config_item set ");
                    commandText.Append(" value=@value ");
                    commandText.Append(" where item_key=@key");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@key", "school_id"));
                    parameters.Add(new MySqlParameter("@value", school_id));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@key", "school_name"));
                    parameters.Add(new MySqlParameter("@value", school_name));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@key", "auto_save_interval"));
                    parameters.Add(new MySqlParameter("@value", auto_save_interval));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@key", "save_wave_interval"));
                    parameters.Add(new MySqlParameter("@value", save_wave_interval));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@key", "is_debug"));
                    parameters.Add(new MySqlParameter("@value", is_debug));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@key", "service_root"));
                    parameters.Add(new MySqlParameter("@value", service_root));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());


                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@key", "token_expire_seconds"));
                    parameters.Add(new MySqlParameter("@value", token_expire_seconds));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@key", "sync_time"));
                    parameters.Add(new MySqlParameter("@value", sync_time.ToString()));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                    if (!string.IsNullOrEmpty(reg_password))
                    {
                        parameters.Clear();
                        parameters.Add(new MySqlParameter("@key", "reg_password"));
                        parameters.Add(new MySqlParameter("@value", Helper.md5(reg_password)));

                        MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());
                    }


                    if (RedisHelper.Exists("cloud_server_token"))
                    {
                        RedisHelper.Remove("cloud_server_token");
                    }
                    if (RedisHelper.Exists("sync_data"))
                    {
                        RedisHelper.Remove("sync_data");
                    }

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
