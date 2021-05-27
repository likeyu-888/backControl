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
    public class RuleItemsController : BaseController
    {
        [HttpGet]
        [Power("935")]
        [Route("api/ruleitems")]
        public IHttpActionResult Get(
            string keyword = "",
            int parent_id = 0,
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


            if (parent_id == 0)
            {
                throw new HttpResponseException(Error("请输入权限组Id"));
            }


            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();


                String columns = "val.id, val.name,val.create_time,val.sort";

                StringBuilder commandText = new StringBuilder("select SQL_CALC_FOUND_ROWS " + columns +
                    "  from ucb_rule val" +
                    " left join ucb_rule itm on itm.id=val.parent_id " +
                    " where itm.id=@parent_id ");

                List<MySqlParameter> parameters = new List<MySqlParameter>();
                parameters.Add(new MySqlParameter("@parent_id", parent_id));

                if (!string.IsNullOrEmpty(keyword))
                {
                    commandText.Append(" and (val.name like CONCAT('%',@keyword,'%') )");
                    parameters.Add(new MySqlParameter("@keyword", keyword));
                }

                commandText.Append(QueryOrder("val." + sort_column, sort_direction));
                commandText.Append(QueryLimit(page_size, page));

                ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                total = TotalCount(conn);

                dictHashtable = GetDict(conn, dict);

            }

            int pageCount = PageCount(total, page_size);


            return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(total, page, page_size, pageCount, ds.Tables[0], dictHashtable));
        }


        [HttpPost]
        [Power("936")]
        [Route("api/ruleitems")]
        public IHttpActionResult Post(JObject obj)
        {

            GetRequest(obj);

            int parent_id = GetInt("parent_id", 0);
            int id = GetInt("id", 0);
            string name = GetString("name");
            int sort = GetInt("sort");

            if (parent_id == 0)
            {
                throw new HttpResponseException(Error("请输入权限组Id"));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入权限项名称"));
            }
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入权限项Id"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();
                parameters.Clear();
                parameters.Add(new MySqlParameter("@parent_id", parent_id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from ucb_rule where id=@parent_id", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("权限组不存在");
                DataRow item = ds.Tables[0].Rows[0];
                int item_id = Convert.ToInt32(item["id"]);

                parameters.Clear();
                parameters.Add(new MySqlParameter("@name", name));
                parameters.Add(new MySqlParameter("@parent_id", parent_id));

                bool exists = MysqlHelper.Exists(conn, "select count(*) from ucb_rule where parent_id=@parent_id and name=@name", parameters.ToArray());
                if (exists) return ErrorJson("权限项已存在");

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                exists = MysqlHelper.Exists(conn, "select count(*) from ucb_rule where  id=@id", parameters.ToArray());
                if (exists) return ErrorJson("权限项Id已存在");

                int action_id = 936;

                string remark2 = "权限组:" + item["name"].ToString() + ",权限项:" + name;
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("insert into ucb_rule set ");
                    commandText.Append("parent_id=@parent_id,");
                    commandText.Append("name=@name,");
                    commandText.Append("id=@id,");
                    commandText.Append("sort=@sort");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@parent_id", parent_id));
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@sort", sort));

                    long keyId = MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray(), true);
                    if (keyId <= 0)
                    {
                        transaction.Rollback();
                        return ErrorJson("权限项添加失败");
                    }

                    transaction.Commit();
                    ActionLog.Finished(conn, logId);
                    return SuccessJson(new { id = keyId });
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
        [Power("937")]
        [Route("api/ruleitems/{id=0}")]
        public IHttpActionResult Put(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();
            string name = GetString("name");
            int sort = GetInt("sort");

            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入权限Id"));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入权限项名称"));
            }


            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();
                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from ucb_rule where id=@id", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("参数项不存在");
                int parent_id = Convert.ToInt32(ds.Tables[0].Rows[0]["parent_id"]);

                parameters.Clear();
                parameters.Add(new MySqlParameter("@parent_id", parent_id));
                DataSet da = MySqlHelper.ExecuteDataset(conn, "select * from ucb_rule where id=@parent_id", parameters.ToArray());
                if (DsEmpty(da)) return ErrorJson("权限组不存在");
                DataRow item = da.Tables[0].Rows[0];

                parameters.Clear();
                parameters.Add(new MySqlParameter("@name", name));
                parameters.Add(new MySqlParameter("@parent_id", parent_id));
                parameters.Add(new MySqlParameter("@id", id));

                bool exists = MysqlHelper.Exists(conn, "select count(*) from ucb_rule where parent_id=@parent_id and name=@name and id!=@id", parameters.ToArray());
                if (exists) return ErrorJson("参数项已存在");

                int action_id = 937;

                string remark2 = "权限组:" + item["name"].ToString() + ",参数项:" + name;
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update ucb_rule set ");
                    commandText.Append("name=@name,");
                    commandText.Append("sort=@sort");
                    commandText.Append(" where id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@sort", sort));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray(), true);

                    transaction.Commit();
                    ActionLog.Finished(conn, logId);
                    return SuccessJson();
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


        [HttpDelete]
        [Power("938")]
        [Route("api/ruleitems/{id=0}")]
        public IHttpActionResult Delete(int id)
        {
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入权限项Id"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();


                List<MySqlParameter> parameters = new List<MySqlParameter>();

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from ucb_rule where id=@id", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("权限项不存在");
                DataRow row = ds.Tables[0].Rows[0];

                parameters.Add(new MySqlParameter("@parent_id", Convert.ToInt32(row["parent_id"])));

                DataSet dd = MySqlHelper.ExecuteDataset(conn, "select * from ucb_rule where id=@parent_id", parameters.ToArray());
                if (DsEmpty(dd)) return ErrorJson("权限组不存在");
                DataRow item = dd.Tables[0].Rows[0];


                int action_id = 938;
                string remark2 = "权限组:" + item["name"].ToString() + ",权限项:" + row["name"].ToString();
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark2);


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

    }
}
