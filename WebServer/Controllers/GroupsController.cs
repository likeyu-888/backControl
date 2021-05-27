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
    public class GroupsController : BaseController
    {
        [HttpGet]
        [Route("api/groups")]
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

                String columns = "gro.id, gro.name, gro.remark,gro.create_time, gro.sort,(select count(*) from dev_device dev where dev.group_id=gro.id and dev.is_delete=0) as device_count";

                StringBuilder commandText = new StringBuilder("select SQL_CALC_FOUND_ROWS " + columns +
                    "  from dev_group gro where 1=1 ");
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                if (!string.IsNullOrEmpty(keyword))
                {
                    commandText.Append(" and ((gro.id =@keyword) or (gro.name like CONCAT('%',@keyword,'%')) )");
                    parameters.Add(new MySqlParameter("@keyword", keyword));
                }

                commandText.Append(QueryOrder("gro." + sort_column, sort_direction));
                commandText.Append(QueryLimit(page_size, page));

                ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                total = TotalCount(conn);

                dictHashtable = GetDict(conn, dict);

            }

            int pageCount = PageCount(total, page_size);


            return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(total, page, page_size, pageCount, ds.Tables[0], dictHashtable));
        }


        [HttpPost]
        [Power("admin,manage")]
        [Route("api/groups")]
        public IHttpActionResult Post(JObject obj)
        {
            GetRequest(obj);

            string name = GetString("name");
            string devices = GetString("devices");
            string remark = GetString("remark");
            int sort = GetInt("sort");
            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入分组名称"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();
                parameters.Add(new MySqlParameter("@name", name));

                bool exists = MysqlHelper.Exists(conn, "select count(*) from dev_group where name=@name", parameters.ToArray());
                if (exists) return ErrorJson("分组名已存在");

                int action_id = 101;

                string remark2 = "分组名:" + name;
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("insert into dev_group set ");
                    commandText.Append("name=@name,");
                    commandText.Append("remark=@remark,");
                    commandText.Append("sort=@sort");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@remark", remark));
                    parameters.Add(new MySqlParameter("@sort", sort));

                    long groupId = MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray(), true);
                    if (groupId <= 0)
                    {
                        transaction.Rollback();
                        return ErrorJson("分组添加失败");
                    }
                    devices = devices.Trim();

                    if (!string.IsNullOrEmpty(devices))
                    {
                        commandText.Clear();
                        commandText.Append("update dev_device set group_id=@group_id where 1=2 ");

                        parameters.Clear();
                        parameters.Add(new MySqlParameter("@group_id", groupId));

                        string[] deviceArr = devices.Split(',');
                        int i = 0;
                        foreach (string device in deviceArr)
                        {
                            i++;
                            commandText.Append(" or id=@id_" + i.ToString());
                            parameters.Add(new MySqlParameter("@id_" + i.ToString(), device));
                        }

                        MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());
                    }
                    if (SendToCloudEnable())
                    {
                        SyncActions.Request("api/groups/" + GetSchoolId().ToString(), RestSharp.Method.POST, new
                        {
                            id = groupId,
                            name,
                            devices,
                            remark,
                            sort
                        });
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
        [Power("admin,manage")]
        [Route("api/groups/{id=0}")]
        public IHttpActionResult Put(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();
            string name = GetString("name");
            string devices = GetString("devices");
            string remark = GetString("remark");
            int sort = GetInt("sort");

            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入分组Id"));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入分组名称"));
            }
            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();

                bool exists = false;

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_group where id=@id", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("设备分组不存在");
                DataRow row = ds.Tables[0].Rows[0];

                if (!string.IsNullOrEmpty(name))
                {
                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@id", id));
                    exists = MysqlHelper.Exists(conn, "select count(*) from dev_group where name=@name and id!=@id", parameters.ToArray());
                    if (exists) return ErrorJson("新分组名称已存在");
                }

                int action_id = 102;
                string remark2 = "原分组名称:" + row["name"].ToString() + ",新分组名称:" + name;
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update dev_group set ");
                    commandText.Append("name=@name,");
                    commandText.Append("remark=@remark,");
                    commandText.Append("sort=@sort ");
                    commandText.Append("where id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@remark", remark));
                    parameters.Add(new MySqlParameter("@sort", sort));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                    commandText.Clear();
                    commandText.Append("update dev_device set group_id=0 where group_id=@group_id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@group_id", id));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                    devices = devices.Trim();

                    if (!string.IsNullOrEmpty(devices))
                    {
                        commandText.Clear();
                        commandText.Append("update dev_device set group_id=@group_id where 1=2 ");

                        parameters.Clear();
                        parameters.Add(new MySqlParameter("@group_id", id));

                        string[] deviceArr = devices.Split(',');
                        int i = 0;
                        foreach (string device in deviceArr)
                        {
                            i++;
                            commandText.Append(" or id=@id_" + i.ToString());
                            parameters.Add(new MySqlParameter("@id_" + i.ToString(), device));
                        }

                        MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());
                    }

                    transaction.Commit();

                    ActionLog.Finished(conn, logId);

                    if (SendToCloudEnable())
                    {
                        SyncActions.Request("api/groups/" + GetSchoolId().ToString() + "/" + id.ToString(), RestSharp.Method.PUT, new
                        {
                            name,
                            devices,
                            remark,
                            sort
                        });
                    }
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
        [Power("admin,manage")]
        [Route("api/groups/{id=0}")]
        public IHttpActionResult Delete(int id)
        {
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入分组Id"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();


                List<MySqlParameter> parameters = new List<MySqlParameter>();


                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_group where id=@id", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("设备分组不存在");
                DataRow row = ds.Tables[0].Rows[0];

                int action_id = 103;
                string remark = "设备分组:" + row["name"].ToString();
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, userInfo.username, userInfo.id, remark);


                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("delete from  dev_group where id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@id", id));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                    commandText = new StringBuilder();
                    commandText.Append("update  dev_device set group_id=0 where group_id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@id", id));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                    transaction.Commit();
                    ActionLog.Finished(conn, logId);

                    if (SendToCloudEnable())
                    {
                        SyncActions.Request("api/groups/" + GetSchoolId().ToString() + "/" + id.ToString(), RestSharp.Method.DELETE, new
                        {
                        });
                    }
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
