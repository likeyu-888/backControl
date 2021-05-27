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
    public class DevicesController : BaseController
    {
        [HttpGet]
        [Power("114")]
        [Route("api/devices/{schoolId}")]
        public IHttpActionResult Get(
            string keyword = "",
            string groups = "",
            string arm_versions = "",
            string dsp_versions = "",
            string types = "",
            string status = "",
            int page = 1,
            int page_size = 10,
            string sort_column = "id",
            string sort_direction = "DESC",
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

            try
            {
                DataSet ds = null;
                Hashtable dictHashtable;
                int total = 0;

                using (conn = new MySqlConnection(Constr()))
                {
                    conn.Open();

                    String columns = "room.reverb_time,dev.device_type,dev.id, dev.name, dev.ip,arm_version,dsp_version, dev.mark, dev.gateway,dev.mac, dev.group_id, dev.status, grp.name as group_name, dev.room_id,room.name as room_name,dev.create_time ";

                    StringBuilder commandText = new StringBuilder("select SQL_CALC_FOUND_ROWS " + columns +
                        "from dev_device dev " +
                        "left join sch_room room on dev.room_id = room.id and room.is_delete=0 and dev.school_id=room.school_id " +
                        "left join dev_group grp on dev.group_id = grp.id and dev.school_id=grp.school_id " +
                        "where dev.is_delete=0 and dev.school_id=@school_id ");
                    List<MySqlParameter> parameters = new List<MySqlParameter>
                    {
                        new MySqlParameter("@school_id", school_id)
                    };

                    if (!string.IsNullOrEmpty(keyword))
                    {
                        commandText.Append(" and ((dev.ip=@keyword) or (dev.name like CONCAT('%',@keyword,'%')) or (room.name like CONCAT('%',@keyword,'%')) )");
                        parameters.Add(new MySqlParameter("@keyword", keyword));
                    }

                    if (!string.IsNullOrEmpty(groups))
                    {
                        groups = groups.Trim();
                        StringBuilder groupStr = new StringBuilder();
                        int i = 0;
                        foreach (string groupItem in groups.Split(','))
                        {
                            i++;
                            groupStr.Append(" or group_id=@group_id_" + i.ToString());
                            parameters.Add(new MySqlParameter("@group_id_" + i.ToString(), groupItem));
                        }
                        commandText.Append(" and  (" + groupStr.ToString().Substring(4) + ")");
                    }

                    if (!string.IsNullOrEmpty(types))
                    {
                        types = types.Trim();
                        StringBuilder typeStr = new StringBuilder();
                        int i = 0;
                        foreach (string typeItem in types.Split(','))
                        {
                            i++;
                            typeStr.Append(" or device_type=@device_type_" + i.ToString());
                            parameters.Add(new MySqlParameter("@device_type_" + i.ToString(), typeItem));
                        }
                        commandText.Append(" and  (" + typeStr.ToString().Substring(4) + ")");
                    }

                    if (!string.IsNullOrEmpty(arm_versions))
                    {
                        arm_versions = arm_versions.Trim();
                        StringBuilder versionStr = new StringBuilder();
                        int i = 0;
                        foreach (string versionItem in arm_versions.Split(','))
                        {
                            i++;
                            versionStr.Append(" or arm_version=@arm_version_" + i.ToString());
                            parameters.Add(new MySqlParameter("@arm_version_" + i.ToString(), versionItem));
                        }
                        commandText.Append(" and  (" + versionStr.ToString().Substring(4) + ")");
                    }

                    if (!string.IsNullOrEmpty(dsp_versions))
                    {
                        dsp_versions = dsp_versions.Trim();
                        StringBuilder versionStr = new StringBuilder();
                        int i = 0;
                        foreach (string versionItem in dsp_versions.Split(','))
                        {
                            i++;
                            versionStr.Append(" or dsp_version=@dsp_version_" + i.ToString());
                            parameters.Add(new MySqlParameter("@dsp_version_" + i.ToString(), versionItem));
                        }
                        commandText.Append(" and  (" + versionStr.ToString().Substring(4) + ")");
                    }

                    if (!string.IsNullOrEmpty(status))
                    {
                        status = status.Trim();
                        StringBuilder statusStr = new StringBuilder();
                        int i = 0;
                        foreach (string statusItem in status.Split(','))
                        {
                            i++;
                            statusStr.Append(" or status=@status_" + i.ToString());
                            parameters.Add(new MySqlParameter("@status_" + i.ToString(), statusItem));
                        }
                        commandText.Append(" and  (" + statusStr.ToString().Substring(4) + ")");
                    }

                    commandText.Append(QueryOrder("dev." + sort_column, sort_direction));
                    commandText.Append(QueryLimit(page_size, page));

                    ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                    total = TotalCount(conn);

                    dictHashtable = GetDict(conn, dict, school_id);

                }

                int pageCount = PageCount(total, page_size);


                return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(total, page, page_size, pageCount, ds.Tables[0], dictHashtable));
            }
            catch (Exception ex)
            {
                return ErrorJson(ex.Message);
            }
        }


        [HttpPost]
        [Power("111")]
        [Route("api/devices/{schoolId}")]
        public IHttpActionResult Post(JObject obj)
        {
            GetRequest(obj);

            int school_id = GetSchoolId();
            int id = GetInt("id");
            string name = GetString("name");
            string ip = GetString("ip");
            int device_type = GetInt("device_type", -1);
            int room_id = GetInt("room_id");
            int group_id = GetInt("group_id");

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
                throw new HttpResponseException(Error("请输入设备Id"));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入设备名称"));
            }
            if (string.IsNullOrEmpty(ip))
            {
                throw new HttpResponseException(Error("请输入设备Ip"));
            }
            if (device_type == -1)
            {
                throw new HttpResponseException(Error("请选择设备类型"));
            }
            if (group_id == 0)
            {
                throw new HttpResponseException(Error("请选择设备分组"));
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
                    new MySqlParameter("@school_id", school_id),
                    new MySqlParameter("@id", id)
                };

                bool exists = MysqlHelper.Exists(conn, "select count(*) from dev_device where school_id=@school_id and (name=@name or id=@id) and is_delete=0", parameters.ToArray());
                if (exists) return ErrorJson("设备名称已存在");

                parameters.Clear();
                parameters.Add(new MySqlParameter("@ip", ip));
                parameters.Add(new MySqlParameter("@school_id", school_id));

                exists = MysqlHelper.Exists(conn, "select count(*) from dev_device where school_id=@school_id and ip=@ip and is_delete=0", parameters.ToArray());
                if (exists) return ErrorJson("设备Ip已存在" + ip);

                int action_id = 111;

                string remark2 = "设备名:" + name + ",Ip地址:" + ip;
                long logId = ActionLog.AddLog(conn, action_id, school_id, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                string sql = "";
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("insert into dev_device set ");
                    commandText.Append("school_id=@school_id,");
                    commandText.Append("id=@id,");
                    commandText.Append("name=@name,");
                    commandText.Append("ip=@ip,");
                    commandText.Append("device_type=@device_type,");
                    commandText.Append("room_id=@room_id,");
                    commandText.Append("group_id=@group_id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@school_id", school_id));
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@ip", ip));
                    parameters.Add(new MySqlParameter("@device_type", device_type));
                    parameters.Add(new MySqlParameter("@room_id", room_id));
                    parameters.Add(new MySqlParameter("@group_id", group_id));

                    long deviceId = MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray(), true);
                    if (deviceId <= 0)
                    {
                        transaction.Rollback();
                        return ErrorJson("设备添加失败");
                    }

                    sql = "CREATE TABLE `dev_history_" + school_id.ToString() + "_" + id.ToString() + "` (" +
                        "`id` int(10) unsigned NOT NULL AUTO_INCREMENT COMMENT '自增序号'," +
                        "  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间'," +
                        "  `device_id` int(11) NOT NULL DEFAULT '0' COMMENT '设备Id'," +
                        "  `snr` decimal(10,2) NOT NULL DEFAULT '0' COMMENT '信噪比'," +
                        "  `listen_efficiency` int(11) NOT NULL DEFAULT '0' COMMENT '听课效率'," +
                        "  `attendence_difficulty` int(11) NOT NULL DEFAULT '0' COMMENT '听课难度'," +
                        "  `anbient_noice` decimal(10,2) NOT NULL DEFAULT '0' COMMENT '环境噪声'," +
                        "  `school_id` int(11) NOT NULL DEFAULT '0' COMMENT '学校Id'," +
                        "  PRIMARY KEY(`id`)" +
                        ") ENGINE = InnoDB DEFAULT CHARSET = utf8";

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, sql);

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, "update sch_room set device_id=" + deviceId.ToString() + " where school_id=" + school_id.ToString() + " and id=" + room_id.ToString());
                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, "update sch_room set device_id=0 where  school_id=" + school_id.ToString() + " and device_id=" + deviceId.ToString() + " and id!=" + room_id.ToString());


                    transaction.Commit();
                    ActionLog.Finished(conn, logId);
                    return SuccessJson(new { id = deviceId });
                }
                catch (Exception ex)
                {
                    try
                    {
                        transaction.Rollback();
                        return ErrorJson(ex.Message + "|" + sql + "|");
                    }
                    catch (Exception en)
                    {
                        return ErrorJson(en.Message);
                    }

                }
            }
        }

        [HttpPut]
        [Power("112")]
        [Route("api/devices/{schoolId}/{id}")]
        public IHttpActionResult Put(JObject obj)
        {
            GetRequest(obj);

            int school_id = GetSchoolId();
            int id = GetId();
            string name = GetString("name");
            string ip = GetString("ip");
            int device_type = GetInt("device_type");
            int room_id = GetInt("room_id");
            int group_id = GetInt("group_id");
            int is_auto_save = GetInt("is_auto_save", -1);
            int is_auto_record = GetInt("is_auto_record", -1);

            LogHelper.GetInstance.Write("is_auto_save", is_auto_save.ToString());
            LogHelper.GetInstance.Write("is_auto_record", is_auto_record.ToString());

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
                throw new HttpResponseException(Error("请输入设备Id"));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入设备名称"));
            }
            if (string.IsNullOrEmpty(ip))
            {
                throw new HttpResponseException(Error("请输入设备Ip"));
            }
            if (device_type == -1)
            {
                throw new HttpResponseException(Error("请选择设备类型"));
            }
            if (group_id == 0)
            {
                throw new HttpResponseException(Error("请选择设备分组"));
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
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_device where school_id=@school_id and id=@id and is_delete=0", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("设备不存在");
                DataRow row = ds.Tables[0].Rows[0];

                parameters.Clear();
                parameters.Add(new MySqlParameter("@name", name));
                parameters.Add(new MySqlParameter("@id", id));
                parameters.Add(new MySqlParameter("@school_id", school_id));

                bool exists = MysqlHelper.Exists(conn, "select count(*) from dev_device where school_id=@school_id and name=@name and is_delete=0 and id!=@id", parameters.ToArray());
                if (exists) return ErrorJson("设备名称已存在");

                parameters.Clear();
                parameters.Add(new MySqlParameter("@ip", ip));
                parameters.Add(new MySqlParameter("@id", id));
                parameters.Add(new MySqlParameter("@school_id", school_id));

                exists = MysqlHelper.Exists(conn, "select count(*) from dev_device where school_id=@school_id and ip=@ip and is_delete=0 and id!=@id", parameters.ToArray());
                if (exists) return ErrorJson("设备Ip已存在");

                int action_id = 112;

                string remark2 = "设备名:" + name + ",Ip地址:" + ip;
                long logId = ActionLog.AddLog(conn, action_id, school_id, id, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update dev_device set ");
                    commandText.Append("name=@name,");
                    commandText.Append("ip=@ip,");
                    commandText.Append("device_type=@device_type,");
                    commandText.Append("room_id=@room_id,");

                    if (is_auto_record >= 0)
                    {
                        commandText.Append("is_auto_save=@is_auto_save,");
                        commandText.Append("is_auto_record=@is_auto_record,");
                    }

                    commandText.Append("group_id=@group_id ");
                    commandText.Append(" where id=@id and school_id=@school_id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@ip", ip));
                    parameters.Add(new MySqlParameter("@device_type", device_type));
                    parameters.Add(new MySqlParameter("@room_id", room_id));
                    parameters.Add(new MySqlParameter("@group_id", group_id));
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@school_id", school_id));


                    if (is_auto_record >= 0)
                    {
                        parameters.Add(new MySqlParameter("@is_auto_save", is_auto_save));
                        parameters.Add(new MySqlParameter("@is_auto_record", is_auto_record));
                    }


                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray(), false);

                    int deviceId = id;
                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, "update sch_room set device_id=" + deviceId.ToString() + " where school_id=" + school_id.ToString() + " and id=" + room_id.ToString());
                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, "update sch_room set device_id=0 where  school_id=" + school_id.ToString() + " and device_id=" + deviceId.ToString() + " and id!=" + room_id.ToString());


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

        [HttpPut]
        [Power("112")]
        [Route("api/devices/{schoolId}/{id}/version")]
        public IHttpActionResult version(JObject obj)
        {
            GetRequest(obj);

            int school_id = GetSchoolId();
            int id = GetId();
            string mark = GetString("mark");
            string gateway = GetString("gateway");
            string mac = GetString("mac");
            int arm_version = GetInt("arm_version");
            int dsp_version = GetInt("dsp_version");

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
                throw new HttpResponseException(Error("请输入设备Id"));
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
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_device where school_id=@school_id and id=@id and is_delete=0", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("设备不存在");
                DataRow row = ds.Tables[0].Rows[0];

                int action_id = 112;

                string remark2 = "设备名:" + row["name"].ToString() + ",版本更新";
                long logId = ActionLog.AddLog(conn, action_id, school_id, id, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update dev_device set ");
                    commandText.Append("gateway=@gateway,");
                    commandText.Append("mac=@mac,");
                    commandText.Append("mark=@mark,");
                    commandText.Append("arm_version=@arm_version,");
                    commandText.Append("dsp_version=@dsp_version ");
                    commandText.Append(" where id=@id and school_id=@school_id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@gateway", gateway));
                    parameters.Add(new MySqlParameter("@mac", mac));
                    parameters.Add(new MySqlParameter("@mark", mark));
                    parameters.Add(new MySqlParameter("@arm_version", arm_version));
                    parameters.Add(new MySqlParameter("@dsp_version", dsp_version));
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@school_id", school_id));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray(), false);

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
        [Power("113")]
        [Route("api/devices/{schoolId}/{id=0}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                int school_id = GetSchoolId();

                if (school_id == 0)
                {
                    throw new HttpResponseException(Error("请输入学校Id"));
                }
                if (id == 0)
                {
                    throw new HttpResponseException(Error("请输入设备Id"));
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
                    DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_device where school_id=@school_id and id=@id and is_delete=0", parameters.ToArray());
                    if (DsEmpty(ds)) return ErrorJson("设备不存在");
                    DataRow row = ds.Tables[0].Rows[0];

                    int action_id = 113;
                    int device_id = id;
                    string remark = "设备:" + row["name"].ToString() + ",Ip地址:" + row["ip"].ToString();
                    long logId = ActionLog.AddLog(conn, action_id, school_id, device_id, 0, userInfo.username, userInfo.id, remark);


                    MySqlTransaction transaction = conn.BeginTransaction();
                    try
                    {
                        StringBuilder commandText = new StringBuilder();
                        commandText.Append("update dev_device set is_delete = 1 where id=@id and school_id=@school_id");

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
            catch (Exception ex)
            {
                return ErrorJson(ex.Message);
            }
        }


        [HttpGet]
        [Route("api/devices/tree/{schoolId}")]
        public IHttpActionResult Get(int group_id = 0)
        {
            int school_id = GetSchoolId();

            if (!HasPower("209"))
            {
                if (!((IList)userInfo.schools.Split(',')).Contains(school_id.ToString()))
                {
                    throw new HttpResponseException(Error("您没有权限管理该学校"));
                }
            }


            dynamic array = new JArray();
            List<int> selectedArr = new List<int>();


            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@school_id", school_id)
                };

                DataSet childDs;
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select name,id from dev_group where school_id=@school_id order by sort", parameters.ToArray());
                foreach (DataRow group in ds.Tables[0].Rows)
                {
                    dynamic children = new JArray();

                    childDs = MySqlHelper.ExecuteDataset(conn, "select name,ip,id from dev_device where  school_id=@school_id and group_id=" + group["id"].ToString() + " and is_delete = 0 order by CONVERT(name using gbk);", parameters.ToArray());
                    foreach (DataRow device in childDs.Tables[0].Rows)
                    {
                        dynamic child = new JObject();
                        child.title = device["name"].ToString() + " [ " + device["ip"].ToString() + " ]";
                        child.id = Convert.ToInt32(device["id"]);
                        if (group_id == Convert.ToInt32(group["id"]))
                        {
                            child["checked"] = true;
                            selectedArr.Add(Convert.ToInt32(device["id"]));
                        }
                        children.Add(child);
                    }

                    dynamic groupJson = new JObject();
                    groupJson.title = group["name"].ToString();
                    groupJson.expand = true;
                    groupJson.children = children;
                    array.Add(groupJson);
                }
            }


            dynamic json = new JObject();
            json.title = "所有设备";
            json.expand = true;
            json.children = array;



            return SuccessJson(new { device_tree = new JArray() { json }, selected_values = selectedArr });
        }

        [HttpGet]
        [Power("114")]
        [Route("api/devices/{school_id}/{id}")]
        public IHttpActionResult Get(int id,
            int school_id = 0,
               string dict = ""
               )
        {

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

            Hashtable dictHashtable;

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                String columns = "dev.*,grp.name as group_name,room.name as room_name";

                StringBuilder commandText = new StringBuilder("select  " + columns +
                    "  from dev_device  dev " +
                    "left join sch_room room on dev.room_id = room.id and room.is_delete=0 " +
                    "left join dev_group grp on dev.group_id = grp.id " +
                    " where dev.school_id=@school_id and dev.id=@id and dev.is_delete=0");
                List<MySqlParameter> parameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@school_id", school_id)
                };

                DataSet ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                if (DsEmpty(ds))
                {
                    throw new HttpResponseException(Error("设备不存在"));
                }
                DataRow row = ds.Tables[0].Rows[0];



                DeviceCommand devCommand = new DeviceCommand(this.tokenHex, school_id, id);
                byte[] command = devCommand.CreateQueryParamsCmd();
                IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(Helper.GetLocalServIp()), Helper.GetLocalServPort());
                JsonMsg<byte[]> msg = UdpHelper.SendCommand(command, iPEndPoint);

                if (msg.code != 200) return ErrorJson(msg.code, msg.message);


                dynamic json = null;

                switch (Convert.ToInt32(row["device_type"]))
                {
                    case 4:
                        Services.Version4.DeviceInfoJson infoJson4 = new Services.Version4.DeviceInfoJson();
                        json = infoJson4.GetJsonFromHex(row, msg.data);
                        break;
                    case 5:
                        Services.Version5.DeviceInfoJson infoJson5 = new Services.Version5.DeviceInfoJson();
                        json = infoJson5.GetJsonFromHex(row, msg.data);
                        break;
                }


                commandText = new StringBuilder();
                commandText.Append("update dev_device set ");
                commandText.Append("name=@name,");
                commandText.Append("gateway=@gateway,");
                commandText.Append("mark=@mark,");
                commandText.Append("mac=@mac,");
                commandText.Append("dsp_version=@dsp_version,");
                commandText.Append("arm_version=@arm_version");
                commandText.Append(" where id=@id and school_id=@school_id");

                parameters.Clear();
                parameters.Add(new MySqlParameter("@name", json.id));
                parameters.Add(new MySqlParameter("@school_id", school_id));
                parameters.Add(new MySqlParameter("@id", id));
                parameters.Add(new MySqlParameter("@gateway", json.arm.gateway));
                parameters.Add(new MySqlParameter("@mark", json.arm.mark));
                parameters.Add(new MySqlParameter("@mac", json.arm.mac));
                parameters.Add(new MySqlParameter("@arm_version", json.arm.version));
                parameters.Add(new MySqlParameter("@dsp_version", json.dsp.version));

                MySqlHelper.ExecuteNonQuery(conn, commandText.ToString(), parameters.ToArray());


                dictHashtable = GetDict(conn, dict, school_id);

                var result = new { detail = json, dict = dictHashtable };

                return SuccessJson(result);
            }

        }

    }
}
