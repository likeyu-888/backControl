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
    public class TokensController : BaseController
    {

        [HttpPost]
        [AllowAnonymous]
        [Route("api/tokens")]
        public IHttpActionResult Post(JObject obj)
        {

            GetRequest(obj);

            string username = GetString("username");
            string password = GetString("password");

            if (string.IsNullOrEmpty(username))
            {
                throw new HttpResponseException(Error("请输入用户名=" + username));
            }
            if (string.IsNullOrEmpty(password))
            {
                throw new HttpResponseException(Error("请输入用户密码"));
            }

            password = Helper.md5(password);

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@username", username),
                    new MySqlParameter("@password", password)
                };

                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from ucb_user where username=@username and is_delete=0", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("用户名或密码不正确");

                DataRow userRow = ds.Tables[0].Rows[0];

                string remark = "";
                long logId = LoginLog.AddLog(conn, 0, username, Convert.ToInt32(userRow["id"]), remark);

                if (password != userRow["password"].ToString())
                {
                    return ErrorJson("用户名或密码不正确");
                }

                if (Convert.ToInt16(userRow["status"]) == 0)
                {
                    return ErrorJson("用户已被禁止登录");
                }

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", userRow["role_id"].ToString()));
                ds = MySqlHelper.ExecuteDataset(conn, "select * from ucb_role where id=@id", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("用户组不存在");
                string rules = ds.Tables[0].Rows[0]["rules"].ToString();

                string token = Helper.md5("login_" + userRow["id"] + "_" + Helper.RadomStr(6));

                string schools = userRow["schools"].ToString();
                schools = schools.Replace(" ", "");
                schools = schools.Replace(",,", ",");
                schools = schools.Replace(",,", ",");
                if (schools.StartsWith(",")) schools = schools.Substring(1);
                if (schools.EndsWith(",")) schools = schools.Substring(0, schools.Length - 1);
                if (string.IsNullOrEmpty(schools)) schools = "0";

                UserInfo user = new UserInfo
                {
                    username = userRow["username"].ToString(),
                    id = Convert.ToInt32(userRow["id"]),
                    school_id = 0,
                    company = userRow["company"].ToString(),
                    contact_name = userRow["contact_name"].ToString(),
                    email = userRow["email"].ToString(),
                    last_login_ip = userRow["last_login_ip"].ToString(),
                    mobile = userRow["mobile"].ToString(),
                    wechat = userRow["wechat"].ToString(),
                    role_id = Convert.ToInt32(userRow["role_id"]),
                    status = Convert.ToInt32(userRow["status"]),
                    schools = schools,

                    access = rules.Split(',')
                };




                try
                {
                    int tokenExpireMinutes = 20;
                    if (RedisHelper.Exists("token_expire_seconds"))
                    {
                        tokenExpireMinutes = Convert.ToInt32(RedisHelper.Get("token_expire_seconds")) / 60;
                    }

                    RedisHelper.Set(token, JsonConvert.SerializeObject(user), tokenExpireMinutes);

                    RedisHelper.Set(token, JsonConvert.SerializeObject(user));
                    parameters = new List<MySqlParameter>
                    {
                        new MySqlParameter("@id", user.id),
                        new MySqlParameter("@last_login_ip", ClientInfo.GetRealIp)
                    };
                    MySqlHelper.ExecuteNonQuery(conn, "update ucb_user set last_login_time=CURRENT_TIMESTAMP,last_login_ip=@last_login_ip,login_count=login_count+1 where id=@id", parameters.ToArray());

                    LoginLog.Finished(conn, logId);

                    string key = Helper.md5("user_login_again_" + user.id.ToString());
                    if (RedisHelper.Exists(key))
                    {
                        RedisHelper.Remove(key);
                    }

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
        [Route("api/tokens")]
        public IHttpActionResult Delete()
        {
            Request.Headers.TryGetValues("x-auth-token", out IEnumerable<string> tokens);
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
                long logId = LoginLog.AddLog(conn, 0, user.username, user.id, remark, 1);

                LoginLog.Finished(conn, logId);
            }

            return SuccessJson();
        }
    }
}
