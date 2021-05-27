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
    public class DictKeysController : BaseController
    {
        [HttpGet]
        [Power("921")]
        [Route("api/dictkeys")]
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

                String columns = "itm.id,itm.name,itm.item_key as dict_key,itm.create_time,itm.sort";

                StringBuilder commandText = new StringBuilder("select SQL_CALC_FOUND_ROWS " + columns +
                    "  from sys_config_item itm where group_id=0 ");
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
        [Power("922")]
        [Route("api/dictkeys")]
        public IHttpActionResult Post(JObject obj)
        {
            GetRequest(obj);

            string name = GetString("name");
            string item_key = GetString("dict_key");
            int sort = GetInt("sort");
            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入参数名"));
            }
            if (string.IsNullOrEmpty(item_key))
            {
                throw new HttpResponseException(Error("请输入参数唯一编号"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();
                parameters.Add(new MySqlParameter("@name", name));

                bool exists = MysqlHelper.Exists(conn, "select count(*) from sys_config_item where group_id=0 and name=@name", parameters.ToArray());
                if (exists) return ErrorJson("参数名已存在");

                parameters.Clear();
                parameters.Add(new MySqlParameter("@item_key", item_key));
                exists = MysqlHelper.Exists(conn, "select count(*) from sys_config_item where group_id=0 and item_key=@item_key", parameters.ToArray());
                if (exists) return ErrorJson("参数唯一编号已存在");

                int action_id = 922;

                string remark2 = "参数名:" + name;
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("insert into sys_config_item set ");
                    commandText.Append("group_id=0,");
                    commandText.Append("name=@name,");
                    commandText.Append("item_key=@item_key,");
                    commandText.Append("sort=@sort");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@item_key", item_key));
                    parameters.Add(new MySqlParameter("@sort", sort));

                    long keyId = MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray(), true);
                    if (keyId <= 0)
                    {
                        transaction.Rollback();
                        return ErrorJson("参数名添加失败");
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
        [Power("923")]
        [Route("api/dictkeys/{id=0}")]
        public IHttpActionResult Put(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();
            string name = GetString("name");
            string item_key = GetString("dict_key");
            int sort = GetInt("sort");
            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入参数名"));
            }
            if (string.IsNullOrEmpty(item_key))
            {
                throw new HttpResponseException(Error("请输入参数唯一编号"));
            }


            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();

                bool exists = false;

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from sys_config_item where id=@id", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("参数名不存在");
                DataRow row = ds.Tables[0].Rows[0];

                if (!string.IsNullOrEmpty(name))
                {
                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@id", id));
                    exists = MysqlHelper.Exists(conn, "select count(*) from sys_config_item where group_id=0 and name=@name and id!=@id", parameters.ToArray());
                    if (exists) return ErrorJson("新参数名称已存在");
                }
                if (!string.IsNullOrEmpty(item_key))
                {
                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@item_key", item_key));
                    parameters.Add(new MySqlParameter("@id", id));
                    exists = MysqlHelper.Exists(conn, "select count(*) from sys_config_item where group_id=0 and item_key=@item_key and id!=@id", parameters.ToArray());
                    if (exists) return ErrorJson("新参数名称已存在");
                }

                int action_id = 923;
                string remark2 = "原参数名:" + row["name"].ToString() + ",新参数名:" + name + ",原参数唯一编号: " + row["item_key"].ToString() + ",新参数唯一编号: " + item_key;
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update sys_config_item set ");
                    commandText.Append("name=@name,");
                    commandText.Append("item_key=@item_key,");
                    commandText.Append("sort=@sort ");
                    commandText.Append("where id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@item_key", item_key));
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
        [Power("924")]
        [Route("api/dictkeys/{id=0}")]
        public IHttpActionResult Delete(int id)
        {
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入参数名Id"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();


                List<MySqlParameter> parameters = new List<MySqlParameter>();


                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from sys_config_item where id=@id", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("参数名不存在");
                DataRow row = ds.Tables[0].Rows[0];

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                bool exists = MysqlHelper.Exists(conn, "select count(*) from sys_config_value where  item_id=@id", parameters.ToArray());
                if (exists) return ErrorJson("该参数名下还存在参数值，不可删除");

                int action_id = 924;
                string remark = "参数名:" + row["name"].ToString();
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark);


                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("delete from  sys_config_item where id=@id");

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
