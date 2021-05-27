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
using System.Web.Routing;

namespace Elite.WebServer.Controllers
{
    public class UsersController : BaseController
    {
        [HttpGet]
        [Power("909")]
        [Route("api/users")]
        public IHttpActionResult Get(
            [FromUri] int role_id = 0,
            [FromUri] string create_time = "",
            [FromUri] string keyword = "",
            [FromUri] int page = 1,
            [FromUri] int page_size = 10,
            [FromUri] string sort_column = "id",
            [FromUri] string sort_direction = "DESC",
            [FromUri] string dict = ""
            )
        {

            DataSet ds = null;
            Hashtable dictHashtable;
            int total = 0;

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                String columns = "user.id, user.username, user.role_id, user.mobile, user.email, user.wechat, user.create_time, user.last_login_time, user.login_count, user.status*1 as status, val.name as role_text,user.schools";

                StringBuilder commandText = new StringBuilder("select SQL_CALC_FOUND_ROWS " + columns +
                    "  from ucb_user user left join ucb_role val on user.role_id=val.id  where user.is_delete=0 ");
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                if (role_id != 0)
                {
                    commandText.Append(" and user.role_id = @role_id");
                    parameters.Add(new MySqlParameter("@role_id", role_id));
                }

                if (!string.IsNullOrEmpty(create_time))
                {
                    string[] times = create_time.Split(',');
                    if (DataValidate.IsDate(times[0]))
                    {
                        commandText.Append(" and (user.create_time >=@create_time_begin) ");
                        parameters.Add(new MySqlParameter("@create_time_begin", times[0]));
                    }
                    if ((times.Length == 2) && (DataValidate.IsDate(times[1])))
                    {
                        commandText.Append(" and user.create_time<@create_time_end");
                        parameters.Add(new MySqlParameter("@create_time_end", times[1]));
                    }
                }

                if (!string.IsNullOrEmpty(keyword))
                {
                    commandText.Append(" and ((user.username like CONCAT('%',@keyword,'%')) or (user.mobile like CONCAT('%',@keyword,'%')) or (user.email like CONCAT('%',@keyword,'%')))");
                    parameters.Add(new MySqlParameter("@keyword", keyword));
                }

                commandText.Append(QueryOrder("user." + sort_column, sort_direction));
                commandText.Append(QueryLimit(page_size, page));

                ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                total = TotalCount(conn);

                dictHashtable = GetDict(conn, dict);

            }

            int pageCount = PageCount(total, page_size);


            return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(total, page, page_size, pageCount, ds.Tables[0], dictHashtable));
        }

        [HttpGet]
        [Power("909")]
        [Route("api/users/{id}")]
        public IHttpActionResult Get(int id,
            string dict = ""
            )
        {
            DataSet ds = null;
            Hashtable dictHashtable;

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                String columns = "user.id, user.schools, user.username, user.role_id, user.company, user.contact_name, user.mobile, user.email, user.wechat, user.create_time, user.last_login_time,user.last_login_ip, user.login_count, user.status*1 as status, val.name as role_text";

                StringBuilder commandText = new StringBuilder("select  " + columns +
                    "  from ucb_user  user left join ucb_role val on user.role_id=val.id  where user.id=@id and user.is_delete=0");
                List<MySqlParameter> parameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@id", id)
                };

                ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                if (DsEmpty(ds))
                {
                    throw new HttpResponseException(Error("用户不存在"));
                }

                dictHashtable = GetDict(conn, dict);
            }

            var result = new { detail = DataRowToDict(ds.Tables[0].Rows[0]), dict = dictHashtable };

            return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(result));
        }


        [HttpPost]
        [Power("901")]
        [Route("api/users")]
        public IHttpActionResult Post(JObject obj)
        {
            GetRequest(obj);


            string username = GetString("username");
            string password = GetString("password");
            int role_id = GetInt("role_id", 0);
            string contact_name = GetString("contact_name");
            string company = GetString("company");
            string mobile = GetString("mobile");
            string email = GetString("email");
            string wechat = GetString("wechat");

            if (string.IsNullOrEmpty(username))
            {
                throw new HttpResponseException(Error("请输入用户名"));
            }
            if (string.IsNullOrEmpty(password))
            {
                throw new HttpResponseException(Error("请输入用户密码"));
            }
            if (password.Length < 6)
            {
                throw new HttpResponseException(Error("新的用户密码至少为6位"));
            }
            if (role_id == 0)
            {
                throw new HttpResponseException(Error("请选择用户角色"));
            }
            if (string.IsNullOrEmpty(contact_name))
            {
                throw new HttpResponseException(Error("请输入用户姓名"));
            }
            password = Helper.md5(password);

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@username", username)
                };

                bool exists = MysqlHelper.Exists(conn, "select count(*) from ucb_user where username=@username and is_delete=0", parameters.ToArray());
                if (exists) return ErrorJson("用户名已存在");

                if (!string.IsNullOrEmpty(email))
                {
                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@email", email));
                    exists = MysqlHelper.Exists(conn, "select count(*) from ucb_user where email=@email and is_delete=0", parameters.ToArray());
                    if (exists) return ErrorJson("电子邮箱已存在");
                }
                if (!string.IsNullOrEmpty(mobile))
                {
                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@mobile", mobile));
                    exists = MysqlHelper.Exists(conn, "select count(*) from ucb_user where mobile=@mobile and is_delete=0", parameters.ToArray());
                    if (exists) return ErrorJson("电话已存在");
                }

                int action_id = 901;
                int device_id = 0;
                string roleName = "普通用户";
                if (role_id == 9) roleName = "超级管理员";
                else if (role_id == 2) roleName = "设备管理员";

                string remark = "用户名:" + username + ",角色：" + roleName;
                long logId = ActionLog.AddLog(conn, action_id, 0, device_id, 0, userInfo.username, userInfo.id, remark);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("insert into ucb_user set ");
                    commandText.Append("username=@username,");
                    commandText.Append("password=@password,");
                    commandText.Append("role_id=@role_id,");
                    commandText.Append("contact_name=@contact_name,");
                    commandText.Append("company=@company,");
                    commandText.Append("email=@email,");
                    commandText.Append("mobile=@mobile,");
                    commandText.Append("wechat=@wechat");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@username", username));
                    parameters.Add(new MySqlParameter("@password", password));
                    parameters.Add(new MySqlParameter("@role_id", role_id));
                    parameters.Add(new MySqlParameter("@contact_name", contact_name));
                    parameters.Add(new MySqlParameter("@company", company));
                    parameters.Add(new MySqlParameter("@email", email));
                    parameters.Add(new MySqlParameter("@mobile", mobile));
                    parameters.Add(new MySqlParameter("@wechat", wechat));

                    long userId = MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray(), true);
                    if (userId <= 0)
                    {
                        transaction.Rollback();
                        return ErrorJson("用户添加失败");
                    }
                    transaction.Commit();
                    ActionLog.Finished(conn, logId);
                    return SuccessJson(new { id = userId });
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


        }

        [HttpPut]
        [Power("902")]
        [Route("api/users/{id=0}")]
        public IHttpActionResult Put(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();

            string contact_name = GetString("contact_name");
            string company = GetString("company");
            string mobile = GetString("mobile");
            string email = GetString("email");
            string wechat = GetString("wechat");
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入用户Id"));
            }
            if (string.IsNullOrEmpty(contact_name))
            {
                throw new HttpResponseException(Error("请输入用户姓名"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();

                bool exists = false;

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from ucb_user where id=@id and is_delete=0", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("用户不存在");
                DataRow row = ds.Tables[0].Rows[0];

                if (userInfo.id != id)
                {
                    if (!HasPower("906"))
                    {
                        return ErrorJson(500, "非法请求，未授权访问");
                    }
                }

                if (!string.IsNullOrEmpty(email))
                {
                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@email", email));
                    parameters.Add(new MySqlParameter("@id", id));
                    exists = MysqlHelper.Exists(conn, "select count(*) from ucb_user where email=@email and is_delete=0 and id!=@id", parameters.ToArray());
                    if (exists) return ErrorJson("电子邮箱已存在");
                }
                if (!string.IsNullOrEmpty(mobile))
                {
                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@mobile", mobile));
                    parameters.Add(new MySqlParameter("@id", id));
                    exists = MysqlHelper.Exists(conn, "select count(*) from ucb_user where mobile=@mobile and is_delete=0 and id!=@id", parameters.ToArray());
                    if (exists) return ErrorJson("电话已存在");
                }


                int action_id = 902;
                int device_id = 0;
                string remark = "用户:" + row["username"].ToString();
                long logId = ActionLog.AddLog(conn, action_id, 0, device_id, 0, userInfo.username, userInfo.id, remark);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update ucb_user set ");
                    commandText.Append("contact_name=@contact_name,");
                    commandText.Append("company=@company,");
                    commandText.Append("email=@email,");
                    commandText.Append("mobile=@mobile,");
                    commandText.Append("wechat=@wechat");
                    commandText.Append(" where id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@contact_name", contact_name));
                    parameters.Add(new MySqlParameter("@company", company));
                    parameters.Add(new MySqlParameter("@email", email));
                    parameters.Add(new MySqlParameter("@mobile", mobile));
                    parameters.Add(new MySqlParameter("@wechat", wechat));

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

        [HttpPut]
        [Power("904")]
        [Route("api/users/{id}/status")]
        public IHttpActionResult UpdateStatus(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();
            int status = GetInt("status", -1);

            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入用户Id"));
            }
            if (status == -1)
            {
                throw new HttpResponseException(Error("请选择用户状态"));
            }

            if (id == userInfo.id) return ErrorJson("不可更改自身帐号的状态");

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();


                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from ucb_user where id=@id and is_delete=0", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("用户不存在");
                DataRow row = ds.Tables[0].Rows[0];

                int action_id = 904;
                int device_id = 0;
                string remark = "用户:" + row["username"].ToString() + ",状态：" + ((status == 1) ? "启用" : "禁用");

                long logId = ActionLog.AddLog(conn, action_id, 0, device_id, 0, userInfo.username, userInfo.id, remark);


                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update ucb_user set status=@status where id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@status", status));

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
            if (status == 0)
            {
                RedisHelper.Set("user_login_again_" + id.ToString(), 0);
            }

            return SuccessJson();
        }

        [HttpPut]
        [Power("906")]
        [Route("api/users/{id}/role")]
        public IHttpActionResult UpdateRole(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();
            int role_id = GetInt("role_id", -1);
            string schools = GetString("schools");

            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入用户Id"));
            }
            if (role_id == -1)
            {
                throw new HttpResponseException(Error("请选择用户组"));
            }
            if (string.IsNullOrEmpty(schools)) schools = "";

            if (id == userInfo.id) return ErrorJson("不可更改自身帐号的用户组");

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();


                List<MySqlParameter> parameters = new List<MySqlParameter>();


                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", role_id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from ucb_role where id=@id", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("用户组不存在");
                DataRow role = ds.Tables[0].Rows[0];

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                ds = MySqlHelper.ExecuteDataset(conn, "select * from ucb_user where id=@id and is_delete=0", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("用户不存在");
                DataRow row = ds.Tables[0].Rows[0];

                int action_id = 906;
                int device_id = 0;

                string roleName = role["name"].ToString();

                string remark = "用户:" + row["username"].ToString() + ",用户组：" + roleName;

                long logId = ActionLog.AddLog(conn, action_id, 0, device_id, 0, userInfo.username, userInfo.id, remark);


                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update ucb_user set role_id=@role_id," +
                        "schools=@schools " +
                        " where id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@role_id", role_id));
                    parameters.Add(new MySqlParameter("@schools", schools.Trim()));

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
            RedisHelper.Set("user_login_again_" + id.ToString(), 0);

            return SuccessJson();
        }

        [HttpPut]
        [Power("905")]
        [Route("api/users/{id}/password")]
        public IHttpActionResult UpdatePassword(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();
            string password = GetString("password");

            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入用户Id"));
            }
            if (string.IsNullOrEmpty(password))
            {
                throw new HttpResponseException(Error("请输入新的用户密码"));
            }
            if (password.Length < 6)
            {
                throw new HttpResponseException(Error("新的用户密码至少为6位"));
            }
            password = Helper.md5(password);

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();


                List<MySqlParameter> parameters = new List<MySqlParameter>();


                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from ucb_user where id=@id and is_delete=0", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("用户不存在");
                DataRow row = ds.Tables[0].Rows[0];

                if (userInfo.id != id)
                {
                    if (!HasPower("906"))
                    {
                        return ErrorJson(500, "非法请求，未授权访问");
                    }
                }

                int action_id = 905;
                int device_id = 0;
                string remark = "用户:" + row["username"].ToString();
                long logId = ActionLog.AddLog(conn, action_id, 0, device_id, 0, userInfo.username, userInfo.id, remark);



                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update ucb_user set password=@password where id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@password", password));

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

            RedisHelper.Set("user_login_again_" + id.ToString(), 0);

            return SuccessJson();
        }

        [HttpDelete]
        [Power("903")]
        [Route("api/users/{id=0}")]
        public IHttpActionResult Delete(int id)
        {
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入用户Id"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();


                List<MySqlParameter> parameters = new List<MySqlParameter>();


                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from ucb_user where id=@id and is_delete=0", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("用户不存在");
                DataRow row = ds.Tables[0].Rows[0];

                if (id == userInfo.id) return ErrorJson("不可删除自身帐号");


                int action_id = 903;
                int device_id = 0;
                string remark = "用户:" + row["username"].ToString();
                long logId = ActionLog.AddLog(conn, action_id, 0, device_id, 0, userInfo.username, userInfo.id, remark);


                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update ucb_user set is_delete=1 where id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@id", id));

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

            RedisHelper.Set("user_login_again_" + id.ToString(), 0);

            return SuccessJson();
        }


    }
}
