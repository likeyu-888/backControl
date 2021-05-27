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
    public class RoomsController : BaseController
    {
        [HttpGet]
        [Power("208")]
        [Route("api/rooms/{schoolId}")]
        public IHttpActionResult Get(
            string devices = "",
            string keyword = "",
            int page = 1,
            int page_size = 10,
            string sort_column = "sort",
            string sort_direction = "ASC",
            string dict = ""
            )
        {
            int school_id = GetSchoolId();

            if (school_id == 0)
            {
                throw new HttpResponseException(Error("请输入学校Id"));
            }
            if (!HasPower("209"))
            {
                if (!((IList)userInfo.schools.Split(',')).Contains(school_id.ToString()))
                {
                    throw new HttpResponseException(Error("您没有权限管理该学校"));
                }
            }

            DataSet ds = null;
            Hashtable dictHashtable;
            int total = 0;

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                String columns = "room.id,room.name,room.reverb_time,room.remark,room.sort,room.device_id,room.create_time,dev.ip,dev.name as device_name";

                StringBuilder commandText = new StringBuilder("select SQL_CALC_FOUND_ROWS " + columns +
                    "  from sch_room  room" +
                    "  left join dev_device dev on room.device_id=dev.id  and dev.school_id=room.school_id " +
                    " where room.is_delete=0 and room.school_id=@school_id ");
                List<MySqlParameter> parameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@school_id", school_id)
                };

                if (!string.IsNullOrEmpty(devices))
                {
                    StringBuilder deviceStr = new StringBuilder();
                    int i = 0;
                    foreach (string deviceItem in devices.Split(','))
                    {
                        i++;
                        deviceStr.Append(" or room.device_id=@device_id_" + i.ToString());
                        parameters.Add(new MySqlParameter("@device_id_" + i.ToString(), deviceItem));
                    }
                    commandText.Append(" and  (" + deviceStr.ToString().Substring(4) + ")");
                }
                if (!string.IsNullOrEmpty(keyword))
                {
                    commandText.Append(" and (room.school_id=@school_id) and ((room.id = @keyword) or (room.name like CONCAT('%',@keyword,'%')) )");
                    parameters.Add(new MySqlParameter("@keyword", keyword));
                }

                commandText.Append(QueryOrder(sort_column, sort_direction));
                commandText.Append(QueryLimit(page_size, page));

                ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                total = TotalCount(conn);

                dictHashtable = GetDict(conn, dict, school_id);

            }

            int pageCount = PageCount(total, page_size);


            return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(total, page, page_size, pageCount, ds.Tables[0], dictHashtable));
        }


        [HttpPost]
        [Power("201")]
        [Route("api/rooms/{schoolId}")]
        public IHttpActionResult Post(JObject obj)
        {
            GetRequest(obj);

            int school_id = GetSchoolId();
            int id = GetInt("id");
            string name = GetString("name");
            decimal reverb_time = GetDecimal("reverb_time", -1);
            string remark = GetString("remark");
            int sort = GetInt("sort", 0);
            int device_id = GetInt("device_id", 0);

            if (school_id == 0)
            {
                school_id = userInfo.school_id;
            }
            if (school_id == 0)
            {
                throw new HttpResponseException(Error("请输入学校Id"));
            }
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入教室Id"));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入教室名称"));
            }
            if (reverb_time == -1)
            {
                throw new HttpResponseException(Error("请输入教室混响时间"));
            }
            if (!HasPower("209"))
            {
                if (!((IList)userInfo.schools.Split(',')).Contains(school_id.ToString()))
                {
                    throw new HttpResponseException(Error("您没有权限管理该学校"));
                }
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@name", name),
                    new MySqlParameter("@school_id", school_id)
                };

                bool exists = MysqlHelper.Exists(conn, "select count(*) from sch_room where name=@name and is_delete=0 and school_id=@school_id", parameters.ToArray());
                if (exists) return ErrorJson("教室名已存在");

                int action_id = 201;


                string remark2 = "教室名:" + name + ",混响时间：" + reverb_time.ToString();
                long logId = ActionLog.AddLog(conn, action_id, school_id, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("insert into sch_room set ");
                    commandText.Append("school_id=@school_id,");
                    commandText.Append("id=@id,");
                    commandText.Append("name=@name,");
                    commandText.Append("reverb_time=@reverb_time,");
                    commandText.Append("remark=@remark,");
                    commandText.Append("sort=@sort,");
                    commandText.Append("device_id=@device_id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@remark", remark));
                    parameters.Add(new MySqlParameter("@sort", sort));
                    parameters.Add(new MySqlParameter("@school_id", school_id));
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@device_id", device_id));
                    MySqlParameter para = new MySqlParameter("@reverb_time", MySqlDbType.Decimal)
                    {
                        Scale = 2,
                        Value = reverb_time
                    };
                    parameters.Add(para);
                    long roomId = MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray(), true);
                    if (roomId <= 0)
                    {
                        transaction.Rollback();
                        return ErrorJson("教室添加失败");
                    }

                    if (device_id != 0)
                    {
                        commandText.Clear();
                        commandText.Append("update dev_device set room_id=@room_id where id=@device_id and school_id=@school_id");

                        parameters.Clear();
                        parameters.Add(new MySqlParameter("@room_id", id));
                        parameters.Add(new MySqlParameter("@device_id", device_id));
                        parameters.Add(new MySqlParameter("@school_id", school_id));

                        MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                        commandText.Clear();
                        commandText.Append("update sch_room set device_id=0 where id!=@roomId and device_id=@device_id and school_id=@school_id");

                        parameters.Clear();
                        parameters.Add(new MySqlParameter("@roomId", id));
                        parameters.Add(new MySqlParameter("@device_id", device_id));
                        parameters.Add(new MySqlParameter("@school_id", school_id));

                        MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());
                    }
                    transaction.Commit();
                    ActionLog.Finished(conn, logId);
                    return SuccessJson(new { id = roomId });
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
        [Power("202")]
        [Route("api/rooms/{schoolId}/{id=0}")]
        public IHttpActionResult Put(JObject obj)
        {
            GetRequest(obj);

            int school_id = GetSchoolId();
            int id = GetId();
            string name = GetString("name");
            decimal reverb_time = GetDecimal("reverb_time", -1);
            string remark = GetString("remark");
            int sort = GetInt("sort", 0);
            int device_id = GetInt("device_id", 0);

            if (school_id == 0)
            {
                school_id = userInfo.school_id;
            }
            if (school_id == 0)
            {
                throw new HttpResponseException(Error("请输入学校Id"));
            }
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入教室Id"));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入教室名"));
            }
            if (reverb_time == -1)
            {
                throw new HttpResponseException(Error("请输入教室混响时间"));
            }
            if (!HasPower("209"))
            {
                if (!((IList)userInfo.schools.Split(',')).Contains(school_id.ToString()))
                {
                    throw new HttpResponseException(Error("您没有权限管理该学校"));
                }
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();

                bool exists = false;

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                parameters.Add(new MySqlParameter("@school_id", school_id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from sch_room where id=@id and is_delete=0 and school_id=@school_id", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("教室不存在");
                DataRow row = ds.Tables[0].Rows[0];
                int oldDeviceId = Convert.ToInt32(row["device_id"]);

                if (!string.IsNullOrEmpty(name))
                {
                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@school_id", school_id));
                    exists = MysqlHelper.Exists(conn, "select count(*) from sch_room where name=@name and is_delete=0 and id!=@id and school_id=@school_id", parameters.ToArray());
                    if (exists) return ErrorJson("教室名称已存在");
                }

                int action_id = 202;
                string remark2 = "教室名称:" + name;
                long logId = ActionLog.AddLog(conn, action_id, school_id, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update sch_room set ");
                    commandText.Append("name=@name,");
                    commandText.Append("reverb_time=@reverb_time,");
                    commandText.Append("remark=@remark,");
                    commandText.Append("sort=@sort,");
                    commandText.Append("device_id=@device_id ");
                    commandText.Append("where id=@id and school_id=@school_id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@reverb_time", reverb_time));
                    parameters.Add(new MySqlParameter("@remark", remark));
                    parameters.Add(new MySqlParameter("@sort", sort));
                    parameters.Add(new MySqlParameter("@device_id", device_id));
                    parameters.Add(new MySqlParameter("@school_id", school_id));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());


                    if (oldDeviceId != device_id)
                    {
                        if (oldDeviceId == 0)
                        {
                            commandText.Clear();
                            commandText.Append("update sch_room set device_id=0 where id!=@id and device_id=@device_id and school_id=@school_id");

                            parameters.Clear();
                            parameters.Add(new MySqlParameter("@id", id));
                            parameters.Add(new MySqlParameter("@device_id", device_id));
                            parameters.Add(new MySqlParameter("@school_id", school_id));

                            MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                            commandText.Clear();
                            commandText.Append("update dev_device set room_id=@room_id where id=@device_id and  school_id=@school_id");

                            parameters.Clear();
                            parameters.Add(new MySqlParameter("@room_id", id));
                            parameters.Add(new MySqlParameter("@device_id", device_id));
                            parameters.Add(new MySqlParameter("@school_id", school_id));

                            MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                        }
                        else if (device_id == 0)
                        {

                            commandText.Clear();
                            commandText.Append("update dev_device set room_id=0 where room_id=@room_id and  school_id=@school_id");

                            parameters.Clear();
                            parameters.Add(new MySqlParameter("@room_id", id));
                            parameters.Add(new MySqlParameter("@school_id", school_id));

                            MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());
                        }
                        else
                        {
                            commandText.Clear();
                            commandText.Append("update dev_device set room_id=0 where id=@oldDeviceId and  school_id=@school_id");

                            parameters.Clear();
                            parameters.Add(new MySqlParameter("@oldDeviceId", oldDeviceId));
                            parameters.Add(new MySqlParameter("@school_id", school_id));

                            MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                            commandText.Clear();
                            commandText.Append("update dev_device set room_id=@room_id where id=@device_id and  school_id=@school_id");

                            parameters.Clear();
                            parameters.Add(new MySqlParameter("@room_id", id));
                            parameters.Add(new MySqlParameter("@device_id", device_id));
                            parameters.Add(new MySqlParameter("@school_id", school_id));

                            MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                            commandText.Clear();
                            commandText.Append("update sch_room set device_id=0 where id!=@id and device_id=@device_id and  school_id=@school_id");

                            parameters.Clear();
                            parameters.Add(new MySqlParameter("@id", id));
                            parameters.Add(new MySqlParameter("@device_id", device_id));
                            parameters.Add(new MySqlParameter("@school_id", school_id));

                            MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());
                        }
                    }
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
        [Power("203")]
        [Route("api/rooms/{schoolId}/{id=0}")]
        public IHttpActionResult Delete(int id)
        {
            int school_id = GetSchoolId();

            if (school_id == 0)
            {
                throw new HttpResponseException(Error("请输入学校Id"));
            }
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入教室Id"));
            }
            if (!HasPower("209"))
            {
                if (!((IList)userInfo.schools.Split(',')).Contains(school_id.ToString()))
                {
                    throw new HttpResponseException(Error("您没有权限管理该学校"));
                }
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();


                List<MySqlParameter> parameters = new List<MySqlParameter>();


                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                parameters.Add(new MySqlParameter("@school_id", school_id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from sch_room where id=@id and is_delete=0 and is_delete=0 and  school_id=@school_id", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("教室不存在");
                DataRow row = ds.Tables[0].Rows[0];

                int action_id = 203;
                string remark = "教室:" + row["name"].ToString();
                long logId = ActionLog.AddLog(conn, action_id, school_id, 0, 0, userInfo.username, userInfo.id, remark);


                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update sch_room set is_delete=1 where id=@id and  school_id=@school_id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@school_id", school_id));

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
