using Elite.WebServer.Base;
using Elite.WebServer.Services;
using Elite.WebServer.Utility;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Http;

namespace Elite.WebServer.Controllers
{
    public class CloudTokensController : BaseController
    {

        [HttpPost]
        [AllowAnonymous]
        [Route("api/cloud/tokens")]
        public IHttpActionResult Post(JObject obj)
        {

            GetRequest(obj);

            int school_id = GetInt("school_id", 0);
            string password = GetString("password");

            if (school_id == 0)
            {
                throw new HttpResponseException(Error("请输入学校Id"));
            }
            if (string.IsNullOrEmpty(password))
            {
                throw new HttpResponseException(Error("请输入注册密码"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();
                parameters.Add(new MySqlParameter("@school_id", school_id));
                parameters.Add(new MySqlParameter("@password", password));

                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from sch_school where id=@school_id and is_delete=0", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("学校Id或密码不正确");

                DataRow schoolRow = ds.Tables[0].Rows[0];

                string remark = "";
                long logId = SyncLog.AddLog(conn, school_id, 0, 0, "cloud/tokens", remark);

                if (password != schoolRow["password"].ToString())
                {
                    return ErrorJson("注册密码不正确");
                }


                string token = Helper.md5("school_login_" + schoolRow["id"] + "_" + DateTime.Now.ToString("yyyyMMddHHmmss"));
                UserInfo user = new UserInfo();
                user.username = schoolRow["name"].ToString();
                user.school_id = school_id;
                user.id = 0;
                user.company = schoolRow["name"].ToString();
                user.contact_name = schoolRow["contact_name"].ToString();
                user.email = schoolRow["email"].ToString();
                user.last_login_ip = "0.0.0.0";
                user.mobile = schoolRow["mobile"].ToString();
                user.wechat = schoolRow["wechat"].ToString();
                user.role_id = 2;
                user.status = 1;
                user.access = new string[] { "101", "102", "103", "111", "112", "113", "123", "125", "134", "142", "201", "202", "203", "210" };
                user.schools = school_id.ToString();

                try
                {
                    int tokenExpireMinutes = 20;
                    if (RedisHelper.Exists("token_expire_seconds"))
                    {
                        tokenExpireMinutes = Convert.ToInt32(RedisHelper.Get("token_expire_seconds")) / 60;
                    }

                    RedisHelper.Set(token, JsonConvert.SerializeObject(user), tokenExpireMinutes);
                    parameters = new List<MySqlParameter>();
                    parameters.Add(new MySqlParameter("@id", user.id));
                    parameters.Add(new MySqlParameter("@last_login_ip", ClientInfo.GetRealIp));
                    MySqlHelper.ExecuteNonQuery(conn, "update sch_school set last_login_time=CURRENT_TIMESTAMP,last_login_ip=@last_login_ip,login_count=login_count+1 where id=@id", parameters.ToArray());

                    LoginLog.Finished(conn, logId);

                    return SuccessJson(new { detail = user, token });
                }
                catch (Exception ex)
                {
                    return ErrorJson(ex.Message);
                }
            }
        }

        [HttpDelete]
        [Power("user")]
        [Route("api/cloud/tokens")]
        public IHttpActionResult Delete()
        {
            IEnumerable<string> tokens;
            Request.Headers.TryGetValues("x-auth-token", out tokens);
            //未带token,禁止访问
            if ((tokens == null) || (tokens.Count() <= 0))
            {
                return SuccessJson();
            }
            string token = tokens.FirstOrDefault();

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();


                UserInfo user = RedisHelper.Get<UserInfo>(token);

                if (user == null)
                {
                    return SuccessJson();
                }
                RedisHelper.Remove(token);

                string remark = "";
                long logId = SyncLog.AddLog(conn, user.id, 0, 0, "cloud/tokens", remark);

                LoginLog.Finished(conn, logId);
            }

            return SuccessJson();
        }
    }
}
