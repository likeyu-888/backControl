using Elite.WebServer.Base;
using Elite.WebServer.Services;
using Elite.WebServer.Utility;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Http;

namespace Elite.WebServer.Controllers
{
    public class SchoolsController : BaseController
    {
        [HttpGet]
        [Power("207")]
        [Route("api/schools")]
        public IHttpActionResult Get(
            string keyword = "",
            int province_id = 0,
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

            string schoolLimit = "";
            if (!HasPower("209"))
            {
                schoolLimit = " and sch.id in (" + userInfo.schools + ")";
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                String columns = "sch.id, sch.name, sch.province_id,sch.contact_name,sch.create_time,sch.wechat,sch.mobile,sch.email, sch.face_url,(select count(*) from dev_device dev where dev.school_id=sch.id and dev.is_delete=0) as device_count," +
                    "(select count(*) from dev_group grp where grp.school_id=sch.id) as group_count," +
                    "(select count(*) from sch_room rom where rom.school_id=sch.id and rom.is_delete=0) as room_count";

                StringBuilder commandText = new StringBuilder("select SQL_CALC_FOUND_ROWS " + columns +
                    "  from sch_school sch where is_delete=0 " + schoolLimit + " ");
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                if (!string.IsNullOrEmpty(keyword))
                {
                    commandText.Append(" and ((sch.id =@keyword) or (sch.name like CONCAT('%',@keyword,'%')) )");
                    parameters.Add(new MySqlParameter("@keyword", keyword));
                }

                if (province_id != 0)
                {
                    commandText.Append(" and (sch.province_id=@province_id)");
                    parameters.Add(new MySqlParameter("@province_id", province_id));
                }

                commandText.Append(QueryOrder("sch." + sort_column, sort_direction));
                commandText.Append(QueryLimit(page_size, page));

                ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());

                total = TotalCount(conn);

                dictHashtable = GetDict(conn, dict);

            }

            int pageCount = PageCount(total, page_size);


            return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(total, page, page_size, pageCount, ds.Tables[0], dictHashtable));

        }


        [HttpPost]
        [Power("204")]
        [Route("api/schools")]
        public IHttpActionResult Post(JObject obj)
        {
            GetRequest(obj);

            string name = GetString("name");
            string password = GetString("password");
            int province_id = GetInt("province_id", 0);
            string contact_name = GetString("contact_name");
            string mobile = GetString("mobile");
            string email = GetString("email");
            string wechat = GetString("wechat");
            string face_url = GetString("face_url");

            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入学校名称"));
            }
            if (string.IsNullOrEmpty(password))
            {
                throw new HttpResponseException(Error("请填写注册密码"));
            }
            if (province_id == 0)
            {
                throw new HttpResponseException(Error("请选择学校所在省份"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@name", name)
                };

                bool exists = MysqlHelper.Exists(conn, "select count(*) from sch_school where name=@name and is_delete=0", parameters.ToArray());
                if (exists) return ErrorJson("学校名已存在");

                int action_id = 204;

                string remark2 = "学校名:" + name;
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    password = Helper.md5(password);

                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("insert into sch_school set ");
                    commandText.Append("name=@name,");
                    commandText.Append("password=@password,");
                    commandText.Append("contact_name=@contact_name,");
                    commandText.Append("mobile=@mobile,");
                    commandText.Append("email=@email,");
                    commandText.Append("wechat=@wechat,");
                    commandText.Append("face_url=@face_url,");
                    commandText.Append("province_id=@province_id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@password", password));
                    parameters.Add(new MySqlParameter("@contact_name", contact_name));
                    parameters.Add(new MySqlParameter("@mobile", mobile));
                    parameters.Add(new MySqlParameter("@email", email));
                    parameters.Add(new MySqlParameter("@wechat", wechat));
                    parameters.Add(new MySqlParameter("@province_id", province_id));
                    parameters.Add(new MySqlParameter("@face_url", face_url));


                    long groupId = MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray(), true);
                    if (groupId <= 0)
                    {
                        transaction.Rollback();
                        return ErrorJson("学校添加失败");
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
        [Power("205")]
        [Route("api/schools/{id=0}")]
        public IHttpActionResult Put(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();
            string name = GetString("name");
            string password = GetString("password");
            int province_id = GetInt("province_id", 0);
            string contact_name = GetString("contact_name");
            string mobile = GetString("mobile");
            string email = GetString("email");
            string wechat = GetString("wechat");
            string face_url = GetString("face_url");

            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入学校Id"));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入学校名称"));
            }
            if (province_id == 0)
            {
                throw new HttpResponseException(Error("请选择学校所在省份"));
            }

            string schoolLimit = "";
            if (!HasPower("209"))
            {
                schoolLimit = " and id in (" + userInfo.schools + ")";
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();

                bool exists = false;

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from sch_school where id=@id" + schoolLimit, parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("学校不存在");
                DataRow row = ds.Tables[0].Rows[0];

                if (!string.IsNullOrEmpty(name))
                {
                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@id", id));
                    exists = MysqlHelper.Exists(conn, "select count(*) from sch_school where name=@name and id!=@id", parameters.ToArray());
                    if (exists) return ErrorJson("新学校名称已存在");
                }

                int action_id = 205;
                string remark2 = "原学校名称:" + row["name"].ToString() + ",新学校名称:" + name;
                long logId = ActionLog.AddLog(conn, action_id, id, 0, 0, userInfo.username, userInfo.id, remark2);

                try
                {
                    parameters.Clear();

                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update sch_school set ");
                    commandText.Append("name=@name,");

                    commandText.Append("contact_name=@contact_name,");
                    commandText.Append("mobile=@mobile,");
                    commandText.Append("email=@email,");
                    commandText.Append("wechat=@wechat,");
                    commandText.Append("face_url=@face_url,");

                    if (!string.IsNullOrEmpty(password))
                    {
                        password = Helper.md5(password);
                        commandText.Append("password=@password,");
                        parameters.Add(new MySqlParameter("@password", password));
                    }

                    commandText.Append("province_id=@province_id ");

                    commandText.Append(" where id=@id");


                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@contact_name", contact_name));
                    parameters.Add(new MySqlParameter("@mobile", mobile));
                    parameters.Add(new MySqlParameter("@email", email));
                    parameters.Add(new MySqlParameter("@wechat", wechat));
                    parameters.Add(new MySqlParameter("@province_id", province_id));
                    parameters.Add(new MySqlParameter("@face_url", face_url));
                    parameters.Add(new MySqlParameter("@id", id));

                    MysqlHelper.ExecuteNonQuery(conn, CommandType.Text, commandText.ToString(), parameters.ToArray());

                    ActionLog.Finished(conn, logId);
                }
                catch (Exception ex)
                {

                    return ErrorJson(ex.Message);
                }

            }

            return SuccessJson();
        }


        [HttpDelete]
        [Power("206")]
        [Route("api/schools/{id=0}")]
        public IHttpActionResult Delete(int id)
        {

            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入学校Id"));
            }

            string schoolLimit = "";
            if (!HasPower("209"))
            {
                schoolLimit = " and id in (" + userInfo.schools + ")";
            }


            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();


                List<MySqlParameter> parameters = new List<MySqlParameter>();


                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from sch_school where id=@id  and is_delete=0" + schoolLimit, parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("学校不存在");
                DataRow row = ds.Tables[0].Rows[0];

                int action_id = 103;
                string remark = "设备学校:" + row["name"].ToString();
                long logId = ActionLog.AddLog(conn, action_id, id, 0, 0, userInfo.username, userInfo.id, remark);


                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update sch_school set is_delete=1 where id=@id");

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
        [Route("api/schools/tree")]
        public IHttpActionResult SchoolsTree(int user_id = 0)
        {

            if (user_id == 0) throw new HttpResponseException(Error("请输入用户Id"));

            dynamic array = new JArray();

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select schools from ucb_user where id=" + user_id.ToString());
                if (DsEmpty(ds)) throw new HttpResponseException(Error("用户未找到"));
                string schools = ds.Tables[0].Rows[0]["schools"].ToString();
                string[] schoolArr = schools.Split(',');

                DataSet dsSchool = MySqlHelper.ExecuteDataset(conn, "select name,id,province_id from sch_school where is_delete=0 order by CONVERT(name using gbk)");
                DataRowCollection schoolList = dsSchool.Tables[0].Rows;

                ds = MySqlHelper.ExecuteDataset(conn, "select text as name,value as id from sys_config_value where item_id=1012 order by sort");
                int provinceId = 0;
                foreach (DataRow group in ds.Tables[0].Rows)
                {
                    JArray children = new JArray();
                    provinceId = Convert.ToInt32(group["id"]);
                    foreach (DataRow device in schoolList)
                    {
                        if (provinceId == Convert.ToInt32(device["province_id"]))
                        {
                            dynamic child = new JObject();
                            child.title = device["name"].ToString();
                            child.id = Convert.ToInt32(device["id"]);
                            if (schoolArr.Contains(device["id"].ToString()))
                            {
                                child["checked"] = true;
                            }
                            children.Add(child);
                        }
                    }
                    if (children.Count > 0)
                    {
                        dynamic groupJson = new JObject();
                        groupJson.title = group["name"].ToString();
                        groupJson.expand = true;
                        groupJson.children = children;
                        array.Add(groupJson);
                    }
                }
            }


            dynamic json = new JObject();
            json.title = "所有学校";
            json.expand = true;
            json.children = array;



            return SuccessJson(new { school_tree = new JArray() { json } });
        }
    }
}
