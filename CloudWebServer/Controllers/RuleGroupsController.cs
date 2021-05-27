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
    public class RuleGroupsController : BaseController
    {
        [HttpGet]
        [Power("931")]
        [Route("api/rulegroups")]
        public IHttpActionResult Get(
            string keyword = "",
            int page = 1,
            int page_size = 10,
            string sort_column = "sort",
            string sort_direction = "ASC",
            string dict = ""
            )
        {
            DataSet ds = null;
            Hashtable dictHashtable;
            int total = 0;

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                String columns = "itm.id,itm.name,itm.create_time,itm.sort";

                StringBuilder commandText = new StringBuilder("select SQL_CALC_FOUND_ROWS " + columns +
                    "  from ucb_rule itm where parent_id=0 ");
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                if (!string.IsNullOrEmpty(keyword))
                {
                    commandText.Append(" and (itm.name like CONCAT('%',@keyword,'%') )");
                    parameters.Add(new MySqlParameter("@keyword", keyword));
                }

                commandText.Append(QueryOrder("itm." + sort_column, sort_direction));
                commandText.Append(QueryLimit(page_size, page));

                ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                total = TotalCount(conn);

                dictHashtable = GetDict(conn, dict);

            }

            int pageCount = PageCount(total, page_size);


            return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(total, page, page_size, pageCount, ds.Tables[0], dictHashtable));
        }


        [HttpPost]
        [Power("932")]
        [Route("api/rulegroups")]
        public IHttpActionResult Post(JObject obj)
        {
            GetRequest(obj);

            string name = GetString("name");
            int id = GetInt("id");
            int sort = GetInt("sort");
            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入权限组名"));
            }
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入权限组Id"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();
                parameters.Add(new MySqlParameter("@name", name));

                bool exists = MysqlHelper.Exists(conn, "select count(*) from ucb_rule where parent_id=0 and name=@name", parameters.ToArray());
                if (exists) return ErrorJson("权限组名已存在");

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                exists = MysqlHelper.Exists(conn, "select count(*) from ucb_rule where parent_id=0 and id=@id", parameters.ToArray());
                if (exists) return ErrorJson("权限组Id已存在");

                int action_id = 932;

                string remark2 = "权限组添加:" + name;
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("insert into ucb_rule set ");
                    commandText.Append("id=@id,");
                    commandText.Append("name=@name,");
                    commandText.Append("sort=@sort");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@sort", sort));

                    long groupId = MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray(), true);
                    if (groupId <= 0)
                    {
                        transaction.Rollback();
                        return ErrorJson("权限组添加失败");
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
        [Power("933")]
        [Route("api/rulegroups/{id=0}")]
        public IHttpActionResult Put(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();
            string name = GetString("name");
            int sort = GetInt("sort");
            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入权限组名"));
            }
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入权限组Id"));
            }


            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();

                bool exists = false;

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from ucb_rule where id=@id", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("权限组不存在");
                DataRow row = ds.Tables[0].Rows[0];

                if (!string.IsNullOrEmpty(name))
                {
                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@id", id));
                    exists = MysqlHelper.Exists(conn, "select count(*) from ucb_rule where parent_id=0 and name=@name and id!=@id", parameters.ToArray());
                    if (exists) return ErrorJson("新权限组名称已存在");
                }


                int action_id = 933;
                string remark2 = "原权限组名:" + row["name"].ToString() + ",新权限组名:" + name;
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update ucb_rule set ");
                    commandText.Append("name=@name,");
                    commandText.Append("sort=@sort ");
                    commandText.Append("where id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@sort", sort));

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
        [Power("934")]
        [Route("api/rulegroups/{id=0}")]
        public IHttpActionResult Delete(int id)
        {
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入权限组Id"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();


                List<MySqlParameter> parameters = new List<MySqlParameter>();


                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from ucb_rule where id=@id", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("权限组不存在");
                DataRow row = ds.Tables[0].Rows[0];

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                bool exists = MysqlHelper.Exists(conn, "select count(*) from ucb_rule where  parent_id=@id", parameters.ToArray());
                if (exists) return ErrorJson("该权限组下还存在权限项，不可删除");

                int action_id = 934;
                string remark = "权限组:" + row["name"].ToString();
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark);


                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("delete from  ucb_rule where id=@id");

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

        [HttpGet]
        [Route("api/rulegroups/tree")]
        public IHttpActionResult Get(
          string dict = ""
          )
        {

            Hashtable dictHashtable;
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            dynamic ruleList = new JArray();

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                DataSet childDs;
                DataSet dd = MySqlHelper.ExecuteDataset(conn, "select name,id from ucb_rule where parent_id=0 order by sort");
                foreach (DataRow group in dd.Tables[0].Rows)
                {
                    dynamic children = new JArray();

                    childDs = MySqlHelper.ExecuteDataset(conn, "select name,id from ucb_rule where parent_id=" + group["id"].ToString() + " order by sort");
                    foreach (DataRow rule in childDs.Tables[0].Rows)
                    {
                        dynamic child = new JObject();
                        child.name = rule["name"].ToString();
                        child.id = Convert.ToInt32(rule["id"]);
                        children.Add(child);
                    }

                    dynamic groupJson = new JObject();
                    groupJson.name = group["name"].ToString();
                    groupJson.id = Convert.ToInt32(group["id"]);
                    groupJson.children = children;
                    ruleList.Add(groupJson);
                }

                dictHashtable = GetDict(conn, dict);
            }

            return SuccessJson(new { rule_list = ruleList, dict = dictHashtable });
        }


    }
}
