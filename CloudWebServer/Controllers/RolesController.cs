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
    public class RolesController : BaseController
    {
        [HttpGet]
        [Power("941")]
        [Route("api/roles")]
        public IHttpActionResult Get(
            string keyword = "",
            int page = 1,
            int page_size = 10,
            string sort_column = "id",
            string sort_direction = "DESC",
            string dict = ""
            )
        {

            DataSet ds = null;
            Hashtable dictHashtable;
            int total = 0;

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                String columns = "role.id, role.name, role.remark,role.role_class,role.create_time, (select count(*) from ucb_user user where user.role_id=role.id and user.is_delete=0) as user_count";

                StringBuilder commandText = new StringBuilder("select SQL_CALC_FOUND_ROWS " + columns +
                    "  from ucb_role role where 1=1 ");
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                if (!string.IsNullOrEmpty(keyword))
                {
                    commandText.Append(" and ((role.id =@keyword) or (role.name like CONCAT('%',@keyword,'%')) )");
                    parameters.Add(new MySqlParameter("@keyword", keyword));
                }

                commandText.Append(QueryOrder("role." + sort_column, sort_direction));
                commandText.Append(QueryLimit(page_size, page));

                ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                total = TotalCount(conn);

                dictHashtable = GetDict(conn, dict);

            }

            int pageCount = PageCount(total, page_size);


            return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(total, page, page_size, pageCount, ds.Tables[0], dictHashtable));
        }

        [HttpGet]
        [Power("941")]
        [Route("api/roles/{id}")]
        public IHttpActionResult Get(int id,
           string dict = ""
           )
        {
            DataSet ds = null;
            Hashtable dictHashtable;
            List<MySqlParameter> parameters = new List<MySqlParameter>();

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                String columns = "role.id, role.name,role.rules,role.status,role.create_time,role.role_class,role.remark";

                StringBuilder commandText = new StringBuilder("select  " + columns +
                    "  from ucb_role role where role.id=@id");

                parameters.Add(new MySqlParameter("@id", id));

                ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                if (DsEmpty(ds))
                {
                    throw new HttpResponseException(Error("用户组不存在"));
                }
                dictHashtable = GetDict(conn, dict);
            }


            Dictionary<string, object> obj = DataRowToDict(ds.Tables[0].Rows[0]);

            var result = new { detail = obj, dict = dictHashtable };

            return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(result));
        }


        [HttpPost]
        [Power("942")]
        [Route("api/roles")]
        public IHttpActionResult Post(JObject obj)
        {
            GetRequest(obj);

            string name = GetString("name");
            string rules = GetString("rules");
            string remark = GetString("remark");
            int role_class = GetInt("role_class", 0);

            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入用户组名称"));
            }
            if (string.IsNullOrEmpty(rules))
            {
                throw new HttpResponseException(Error("请选择用户组权限"));
            }


            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@name", name)
                };

                bool exists = MysqlHelper.Exists(conn, "select count(*) from ucb_role where name=@name", parameters.ToArray());
                if (exists) return ErrorJson("用户组已存在");

                int action_id = 942;

                string remark2 = "用户组名:" + name;
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("insert into ucb_role set ");
                    commandText.Append("name=@name,");
                    commandText.Append("remark=@remark,");
                    commandText.Append("role_class=@role_class,");
                    commandText.Append("rules=@rules");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@remark", remark));
                    parameters.Add(new MySqlParameter("@rules", rules));
                    parameters.Add(new MySqlParameter("@role_class", role_class));

                    long groupId = MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray(), true);
                    if (groupId <= 0)
                    {
                        transaction.Rollback();
                        return ErrorJson("用户组添加失败");
                    }

                    transaction.Commit();
                    ActionLog.Finished(conn, logId);
                    return SuccessJson(new { id = groupId });
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
        [Power("943")]
        [Route("api/roles/{id=0}")]
        public IHttpActionResult Put(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();
            string name = GetString("name");
            string rules = GetString("rules");
            string remark = GetString("remark");
            int role_class = GetInt("role_class");

            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入用户组Id"));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入用户组名称"));
            }
            if (string.IsNullOrEmpty(rules))
            {
                throw new HttpResponseException(Error("请选择用户组权限"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();

                bool exists = false;

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from ucb_role where id=@id", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("用户组不存在");
                DataRow row = ds.Tables[0].Rows[0];

                if (!string.IsNullOrEmpty(name))
                {
                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@id", id));
                    exists = MysqlHelper.Exists(conn, "select count(*) from ucb_role where name=@name and id!=@id", parameters.ToArray());
                    if (exists) return ErrorJson("新用户组名称已存在");
                }

                int action_id = 943;
                string remark2 = "原用户组名称:" + row["name"].ToString() + ",新用户组名称:" + name;
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update ucb_role set ");
                    commandText.Append("name=@name,");
                    commandText.Append("remark=@remark,");
                    commandText.Append("rules=@rules,");
                    commandText.Append("role_class=@role_class ");
                    commandText.Append("where id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@remark", remark));
                    parameters.Add(new MySqlParameter("@rules", rules));
                    parameters.Add(new MySqlParameter("@role_class", role_class));

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


        [HttpDelete]
        [Power("944")]
        [Route("api/roles/{id=0}")]
        public IHttpActionResult Delete(int id)
        {
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入用户组Id"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();


                List<MySqlParameter> parameters = new List<MySqlParameter>();


                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from ucb_role where id=@id", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("用户组不存在");
                DataRow row = ds.Tables[0].Rows[0];

                parameters.Clear();
                parameters.Add(new MySqlParameter("@role_id", id));
                ds = MySqlHelper.ExecuteDataset(conn, "select id from ucb_user where role_id=@role_id and is_delete=0", parameters.ToArray());
                if (!DsEmpty(ds)) return ErrorJson("该用户组下存在用户，不可删除");

                int action_id = 944;
                string remark = "用户组:" + row["name"].ToString();
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark);


                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("delete from  ucb_role where id=@id");

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

            return SuccessJson();
        }

    }
}
