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
    public class DictValuesController : BaseController
    {
        [HttpGet]
        [Power("925")]
        [Route("api/dictvalues")]
        public IHttpActionResult Get(
            string keyword = "",
            string dict_key = "",
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

            string item_key = dict_key;

            if (string.IsNullOrEmpty(item_key))
            {
                throw new HttpResponseException(Error("请输入参数名唯一编号"));
            }


            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();


                String columns = "val.id,val.text as name,itm.item_key as dict_key,val.create_time,val.sort,val.value";

                StringBuilder commandText = new StringBuilder("select SQL_CALC_FOUND_ROWS " + columns +
                    "  from sys_config_value val" +
                    " left join sys_config_item itm on itm.id=val.item_id " +
                    " where itm.item_key=@item_key ");

                List<MySqlParameter> parameters = new List<MySqlParameter>();
                parameters.Add(new MySqlParameter("@item_key", item_key));

                if (!string.IsNullOrEmpty(keyword))
                {
                    commandText.Append(" and (val.text like CONCAT('%',@keyword,'%') )");
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
        [Power("926")]
        [Route("api/dictvalues")]
        public IHttpActionResult Post(JObject obj)
        {

            GetRequest(obj);

            string item_key = GetString("dict_key");
            string text = GetString("name");
            string value = GetString("value");
            int sort = GetInt("sort");

            if (string.IsNullOrEmpty(item_key))
            {
                throw new HttpResponseException(Error("请输入参数名唯一编号"));
            }
            if (string.IsNullOrEmpty(text))
            {
                throw new HttpResponseException(Error("请输入参数值名称"));
            }
            if (string.IsNullOrEmpty(value))
            {
                throw new HttpResponseException(Error("请输入参数值"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();
                parameters.Clear();
                parameters.Add(new MySqlParameter("@item_key", item_key));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from sys_config_item where item_key=@item_key", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("参数名不存在");
                DataRow item = ds.Tables[0].Rows[0];
                int item_id = Convert.ToInt32(item["id"]);

                parameters.Clear();
                parameters.Add(new MySqlParameter("@text", text));
                parameters.Add(new MySqlParameter("@item_id", item_id));

                bool exists = MysqlHelper.Exists(conn, "select count(*) from sys_config_value where item_id=@item_id and text=@text", parameters.ToArray());
                if (exists) return ErrorJson("参数值已存在");

                parameters.Clear();
                parameters.Add(new MySqlParameter("@value", value));
                parameters.Add(new MySqlParameter("@item_id", item_id));
                exists = MysqlHelper.Exists(conn, "select count(*) from sys_config_value where item_id=@item_id and value=@value", parameters.ToArray());
                if (exists) return ErrorJson("参数值已存在");

                int action_id = 926;

                string remark2 = "参数名:" + item["name"].ToString() + ",参数值:" + text;
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("insert into sys_config_value set ");
                    commandText.Append("item_id=@item_id,");
                    commandText.Append("text=@text,");
                    commandText.Append("value=@value,");
                    commandText.Append("sort=@sort");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@text", text));
                    parameters.Add(new MySqlParameter("@item_id", item_id));
                    parameters.Add(new MySqlParameter("@value", value));
                    parameters.Add(new MySqlParameter("@sort", sort));

                    long keyId = MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray(), true);
                    if (keyId <= 0)
                    {
                        transaction.Rollback();
                        return ErrorJson("参数值添加失败");
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
        [Power("927")]
        [Route("api/dictvalues/{id=0}")]
        public IHttpActionResult Put(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();
            string text = GetString("name");
            string value = GetString("value");
            int sort = GetInt("sort");

            if (string.IsNullOrEmpty(text))
            {
                throw new HttpResponseException(Error("请输入参数值名称"));
            }
            if (string.IsNullOrEmpty(value))
            {
                throw new HttpResponseException(Error("请输入参数值"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();
                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from sys_config_value where id=@id", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("参数值不存在");
                int item_id = Convert.ToInt32(ds.Tables[0].Rows[0]["item_id"]);

                parameters.Clear();
                parameters.Add(new MySqlParameter("@item_id", item_id));
                DataSet da = MySqlHelper.ExecuteDataset(conn, "select * from sys_config_item where id=@item_id", parameters.ToArray());
                if (DsEmpty(da)) return ErrorJson("参数名不存在");
                DataRow item = da.Tables[0].Rows[0];

                parameters.Clear();
                parameters.Add(new MySqlParameter("@text", text));
                parameters.Add(new MySqlParameter("@item_id", item_id));
                parameters.Add(new MySqlParameter("@id", id));

                bool exists = MysqlHelper.Exists(conn, "select count(*) from sys_config_value where item_id=@item_id and text=@text and id!=@id", parameters.ToArray());
                if (exists) return ErrorJson("参数值已存在");

                parameters.Clear();
                parameters.Add(new MySqlParameter("@value", value));
                parameters.Add(new MySqlParameter("@id", id));
                parameters.Add(new MySqlParameter("@item_id", item_id));
                exists = MysqlHelper.Exists(conn, "select count(*) from sys_config_value where item_id=@item_id and value=@value and id!=@id", parameters.ToArray());
                if (exists) return ErrorJson("参数值已存在");

                int action_id = 927;

                string remark2 = "参数名:" + item["name"].ToString() + ",参数值:" + text;
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update sys_config_value set ");
                    commandText.Append("text=@text,");
                    commandText.Append("value=@value,");
                    commandText.Append("sort=@sort");
                    commandText.Append(" where id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@text", text));
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@value", value));
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
        [Power("928")]
        [Route("api/dictvalues/{id=0}")]
        public IHttpActionResult Delete(int id)
        {
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入参数值Id"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();


                List<MySqlParameter> parameters = new List<MySqlParameter>();

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from sys_config_value where id=@id", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("参数值不存在");
                DataRow row = ds.Tables[0].Rows[0];

                parameters.Add(new MySqlParameter("@item_id", Convert.ToInt32(row["item_id"])));

                DataSet dd = MySqlHelper.ExecuteDataset(conn, "select * from sys_config_item where id=@item_id", parameters.ToArray());
                if (DsEmpty(dd)) return ErrorJson("参数名不存在");
                DataRow item = dd.Tables[0].Rows[0];


                int action_id = 928;
                string remark2 = "参数名:" + item["name"].ToString() + ",参数值:" + row["text"].ToString();
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark2);


                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("delete from  sys_config_value where id=@id");

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
