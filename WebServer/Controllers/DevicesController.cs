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
using System.Web.Http.Results;

namespace Elite.WebServer.Controllers
{
    public class DevicesController : BaseController
    {
        private long addRoomId = 0;
        private int deviceStatus = 100;
        private byte[] msgData;

        [HttpGet]
        [Route("api/devices")]
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
            try
            {
                DataSet ds = null;
                Hashtable dictHashtable;
                int total = 0;

                using (conn = new MySqlConnection(Constr()))
                {
                    conn.Open();

                    String columns = "dev.device_type,dev.id, dev.name, dev.ip,arm_version,dsp_version, dev.mark, dev.gateway,dev.mac, dev.group_id, dev.status, grp.name as group_name, dev.room_id,room.reverb_time,room.name as room_name,dev.create_time ";

                    StringBuilder commandText = new StringBuilder("select SQL_CALC_FOUND_ROWS " + columns +
                        "from dev_device dev " +
                        "left join sch_room room on dev.room_id = room.id and room.is_delete=0 " +
                        "left join dev_group grp on dev.group_id = grp.id " +
                        "where dev.is_delete=0 ");
                    List<MySqlParameter> parameters = new List<MySqlParameter>();

                    if (!string.IsNullOrEmpty(keyword))
                    {
                        commandText.Append(" and ((dev.ip=@keyword) or (dev.name like CONCAT('%',@keyword,'%')) or ( (room.name like CONCAT('%',@keyword,'%')) and (room.name is not null)   ) )");
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

                    dictHashtable = GetDict(conn, dict);

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
        [Power("admin,manage")]
        [Route("api/devices")]
        public IHttpActionResult Post(JObject obj)
        {
            GetRequest(obj);

            string name = GetString("name");
            string ip = GetString("ip");
            int device_type = GetInt("device_type", -1);
            long room_id = GetInt("room_id");
            int group_id = GetInt("group_id");

            string room_name = GetString("room_name");
            string room_remark = GetString("room_remark");
            int room_sort = GetInt("room_sort");
            decimal reverb_time = GetDecimal("reverb_time");

            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入设备名称"));
            }
            if (string.IsNullOrEmpty(ip))
            {
                throw new HttpResponseException(Error("请输入设备Ip"));
            }
            if (!DataValidate.IsIp(ip))
            {
                throw new HttpResponseException(Error("请输入格式正确的Ip地址"));
            }
            if (device_type == -1)
            {
                throw new HttpResponseException(Error("请选择设备类型"));
            }
            if (group_id == 0)
            {
                throw new HttpResponseException(Error("请选择设备分组"));
            }
            if (room_id == 0)
            {
                if (string.IsNullOrEmpty(room_name))
                {
                    throw new HttpResponseException(Error("请选择已有教室或输入新教室名称"));
                }
                if (reverb_time == 0)
                {
                    throw new HttpResponseException(Error("请输入教室混响时间"));
                }
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@name", name)
                };

                bool exists = MysqlHelper.Exists(conn, "select count(*) from dev_device where name=@name and is_delete=0", parameters.ToArray());
                if (exists) return ErrorJson("设备名称已存在");

                parameters.Clear();
                parameters.Add(new MySqlParameter("@ip", ip));

                exists = MysqlHelper.Exists(conn, "select count(*) from dev_device where ip=@ip and is_delete=0", parameters.ToArray());
                if (exists) return ErrorJson("设备Ip已存在" + ip);

                MySqlTransaction transaction = conn.BeginTransaction();

                if (room_id == 0)
                {
                    NegotiatedContentResult<JsonMsg> addResult = AddRoom(conn, transaction, room_name, room_remark, reverb_time, 0, room_sort);
                    if (addResult.StatusCode != HttpStatusCode.OK)
                    {
                        throw new HttpResponseException(Error("教室添加失败:" + addResult.Content.message));
                    }
                    room_id = this.addRoomId;
                }

                int action_id = 111;

                string remark2 = "设备名:" + name + ",Ip地址:" + ip;
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, userInfo.username, userInfo.id, remark2);



                string sql = "";
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("insert into dev_device set ");
                    commandText.Append("name=@name,");
                    commandText.Append("ip=@ip,");
                    commandText.Append("device_type=@device_type,");
                    commandText.Append("room_id=@room_id,");
                    commandText.Append("group_id=@group_id");

                    parameters.Clear();
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

                    sql = "CREATE TABLE `dev_history_" + deviceId.ToString() + "` (" +
                        "`id` int(10) unsigned NOT NULL AUTO_INCREMENT COMMENT '自增序号'," +
                        "  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间'," +
                        "  `device_id` int(11) NOT NULL DEFAULT '0' COMMENT '设备Id'," +
                        "  `snr` decimal(10,2) NOT NULL DEFAULT '0' COMMENT '信噪比'," +
                        "  `listen_efficiency` int(11) NOT NULL DEFAULT '0' COMMENT '听课效率'," +
                        "  `attendence_difficulty` int(11) NOT NULL DEFAULT '0' COMMENT '听课难度'," +
                        "  `anbient_noice` decimal(10,2) NOT NULL DEFAULT '0' COMMENT '环境噪声'," +
                        "  PRIMARY KEY(`id`)" +
                        ") ENGINE = InnoDB DEFAULT CHARSET = utf8";

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, sql);

                    if (room_id > 0)
                    {
                        MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, "update dev_device set room_id=0 where room_id=" + room_id.ToString() + " and id!=" + deviceId.ToString());

                        MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, "update sch_room set device_id=" + deviceId.ToString() + " where id=" + room_id.ToString());
                        MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, "update sch_room set device_id=0 where device_id=" + deviceId.ToString() + " and id!=" + room_id.ToString());
                    }


                    try
                    {
                        if (SendToCloudEnable())
                        {
                            SyncActions.Request("api/devices/" + GetSchoolId().ToString(), RestSharp.Method.POST, new
                            {
                                id = deviceId,
                                name,
                                ip,
                                device_type,
                                room_id,
                                group_id
                            });
                        }
                    }
                    catch (Exception) { };




                    ActionLog.Finished(conn, logId);

                    transaction.Commit();

                    return SuccessJson(new { id = deviceId });
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

        private NegotiatedContentResult<JsonMsg> AddRoom(MySqlConnection conn, MySqlTransaction transaction, string name,
            string remark,
            decimal reverb_time,
            int device_id = 0,
            int sort = 0
            )
        {
            if (string.IsNullOrEmpty(name))
            {
                return ErrorJson("请输入教室名称");
            }
            if (reverb_time == -1)
            {
                return ErrorJson("请输入教室混响时间");
            }



            List<MySqlParameter> parameters = new List<MySqlParameter>
            {
                new MySqlParameter("@name", name)
            };

            bool exists = MysqlHelper.Exists(conn, "select count(*) from sch_room where name=@name and is_delete=0", parameters.ToArray());
            if (exists) return ErrorJson("教室名已存在");

            int action_id = 201;


            string remark2 = "教室名:" + name + ",混响时间：" + reverb_time.ToString();
            long logId = ActionLog.AddLog(transaction, action_id, 0, 0, userInfo.username, userInfo.id, remark2);

            try
            {
                StringBuilder commandText = new StringBuilder();
                commandText.Append("insert into sch_room set ");
                commandText.Append("name=@name,");
                commandText.Append("reverb_time=@reverb_time,");
                commandText.Append("remark=@remark,");
                commandText.Append("sort=@sort,");
                commandText.Append("device_id=@device_id");

                parameters.Clear();
                parameters.Add(new MySqlParameter("@name", name));
                parameters.Add(new MySqlParameter("@remark", remark));
                parameters.Add(new MySqlParameter("@sort", sort));
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
                    commandText.Append("update dev_device set room_id=@room_id where id=@device_id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@room_id", roomId));
                    parameters.Add(new MySqlParameter("@device_id", device_id));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());

                    commandText.Clear();
                    commandText.Append("update sch_room set device_id=0 where id!=@roomId and device_id=@device_id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@roomId", roomId));
                    parameters.Add(new MySqlParameter("@device_id", device_id));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());
                }

                if (SendToCloudEnable())
                {
                    SyncActions.Request("api/rooms/" + GetSchoolId().ToString(), RestSharp.Method.POST, new
                    {
                        id = roomId,
                        name,
                        reverb_time,
                        remark,
                        sort,
                        device_id
                    });
                }
                addRoomId = roomId;

                ActionLog.Finished(transaction, logId);
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

        private int ArmInt(int index)
        {
            return Convert.ToInt32(this.msgData[index + 8]);
        }

        private JsonMsg<byte[]> UpdateDeviceInfo(MySqlConnection transaction, int deviceId, string name, float reverb_time)
        {

            DeviceCommand devCommand = new DeviceCommand(this.tokenHex, deviceId);
            byte[] command = devCommand.CreateStatusCmd();
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(Helper.GetLocalServIp()), Helper.GetLocalServPort());
            JsonMsg<byte[]> msg = UdpHelper.SendCommand(command, iPEndPoint);

            if (msg.code != 200) return msg;

            byte[] returns = msg.data;

            float snr = BitConverter.ToSingle(returns, 112);
            int listen_efficiency = Convert.ToInt16(returns[192]);
            int attendence_difficulty = 0;
            float anbient_noice = BitConverter.ToSingle(returns, 80);

            command = devCommand.CreateQueryParamsCmd();
            msg = UdpHelper.SendCommand(command, iPEndPoint);

            if (msg.code != 200) return msg;

            msgData = msg.data;

            deviceStatus = (int)msg.data[5];
            int armVersion = (int)msg.data[297 + 8];
            int dspVersion = (int)msg.data[300 + 8 + 5];
            string gateway = ArmInt(36).ToString() + "." + ArmInt(37).ToString() + "." + ArmInt(38).ToString() + "." + ArmInt(39).ToString();
            string mark = ArmInt(40).ToString() + "." + ArmInt(41).ToString() + "." + ArmInt(42).ToString() + "." + ArmInt(43).ToString();
            string mac = ArmInt(44).ToString("X2") + "." + ArmInt(45).ToString("X2") + "." + ArmInt(46).ToString("X2") + "." + ArmInt(47).ToString("X2") + "." + ArmInt(48).ToString("X2") + "." + ArmInt(49).ToString("X2");
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, "update dev_device set status=" + deviceStatus.ToString() + ",snr = " + snr.ToString() + ", " +
                          "listen_efficiency=" + listen_efficiency.ToString() + "," +
                          "attendence_difficulty=" + attendence_difficulty.ToString() + "," +
                          "arm_version=" + armVersion.ToString() + "," +
                          "dsp_version=" + dspVersion.ToString() + "," +
                          "gateway='" + gateway + "'," +
                          "mark='" + mark + "'," +
                          "mac='" + mac + "'," +
                          "anbient_noice=" + anbient_noice.ToString() + " where id=" + deviceId.ToString(), parameters.ToArray());


            byte[] buff = new byte[620];
            Array.Copy(msg.data, 8, buff, 0, msg.data.Length - 12);

            if (reverb_time != 0)
            {
                byte[] reverbTime = BitConverter.GetBytes(reverb_time);
                Array.Copy(reverbTime, 0, buff, 608, reverbTime.Length);
            }


            byte[] nameBytes = Encoding.Default.GetBytes(name);
            Array.Copy(new byte[32], 0, buff, 0, 32);
            Array.Copy(nameBytes, 0, buff, 0, nameBytes.Length);

            command = devCommand.CreateUpdateParamsCmd(buff);


            msg = UdpHelper.SendCommand(command, iPEndPoint);
            if (msg.code != 200) return msg;
            return msg;
        }

        [HttpPut]
        [Power("admin,manage")]
        [Route("api/devices/{id}")]
        public IHttpActionResult Put(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();
            string name = GetString("name");
            string ip = GetString("ip");
            int device_type = GetInt("device_type");
            long room_id = GetInt("room_id");
            int group_id = GetInt("group_id");


            string room_name = GetString("room_name");
            string room_remark = GetString("room_remark");
            int room_sort = GetInt("room_sort");
            decimal reverb_time = GetDecimal("reverb_time");

            int is_auto_save = -1;
            int is_auto_record = -1;
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
            if (!DataValidate.IsIp(ip))
            {
                throw new HttpResponseException(Error("请输入格式正确的Ip地址"));
            }
            if (device_type == -1)
            {
                throw new HttpResponseException(Error("请选择设备类型"));
            }
            if (group_id == 0)
            {
                throw new HttpResponseException(Error("请选择设备分组"));
            }

            if (room_id == 0)
            {
                if (string.IsNullOrEmpty(room_name))
                {
                    throw new HttpResponseException(Error("请选择已有教室或输入新教室名称"));
                }
                if (reverb_time == 0)
                {
                    throw new HttpResponseException(Error("请输入教室混响时间"));
                }
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_device where id=@id and is_delete=0", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("设备不存在");
                DataRow row = ds.Tables[0].Rows[0];

                is_auto_save = Int32.Parse(row["is_auto_save"].ToString());
                is_auto_record = Int32.Parse(row["is_auto_record"].ToString());


                parameters.Clear();
                parameters.Add(new MySqlParameter("@name", name));
                parameters.Add(new MySqlParameter("@id", id));

                bool exists = MysqlHelper.Exists(conn, "select count(*) from dev_device where name=@name and is_delete=0 and id!=@id", parameters.ToArray());
                if (exists) return ErrorJson("设备名称已存在");

                parameters.Clear();
                parameters.Add(new MySqlParameter("@ip", ip));
                parameters.Add(new MySqlParameter("@id", id));

                exists = MysqlHelper.Exists(conn, "select count(*) from dev_device where ip=@ip and is_delete=0 and id!=@id", parameters.ToArray());
                if (exists) return ErrorJson("设备Ip已存在");

                MySqlTransaction transaction = conn.BeginTransaction();

                if (room_id == 0)
                {
                    NegotiatedContentResult<JsonMsg> addResult = AddRoom(conn, transaction, room_name, room_remark, reverb_time, 0, room_sort);
                    if (addResult.StatusCode != HttpStatusCode.OK)
                    {
                        throw new HttpResponseException(Error("教室添加失败:" + addResult.Content.message));
                    }
                    room_id = this.addRoomId;
                }

                int action_id = 111;

                string remark2 = "设备名:" + name + ",Ip地址:" + ip;
                long logId = ActionLog.AddLog(conn, action_id, id, 0, userInfo.username, userInfo.id, remark2);


                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update dev_device set ");
                    commandText.Append("name=@name,");
                    commandText.Append("ip=@ip,");
                    commandText.Append("device_type=@device_type,");
                    commandText.Append("room_id=@room_id,");
                    commandText.Append("group_id=@group_id ");
                    commandText.Append(" where id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@ip", ip));
                    parameters.Add(new MySqlParameter("@device_type", device_type));
                    parameters.Add(new MySqlParameter("@room_id", room_id));
                    parameters.Add(new MySqlParameter("@group_id", group_id));
                    parameters.Add(new MySqlParameter("@id", id));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray(), false);

                    if (room_id > 0)
                    {
                        MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, "update dev_device set room_id=0 where room_id=" + room_id.ToString() + " and id!=" + id.ToString());
                        MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, "update sch_room set device_id=0 where device_id=" + id.ToString());
                        MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, "update sch_room set device_id=" + id.ToString() + " where id=" + room_id.ToString());
                    }

                    if (SendToCloudEnable())
                    {
                        SyncActions.Request("api/devices/" + GetSchoolId().ToString() + "/" + id.ToString(), RestSharp.Method.PUT, new
                        {
                            id,
                            name,
                            ip,
                            device_type,
                            room_id,
                            group_id,
                            is_auto_save,
                            is_auto_record
                        });
                    }

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
        [Power("admin,manage")]
        [Route("api/devices/{id}/record")]
        public IHttpActionResult UpdateRecord(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();
            int status = GetInt("status", 0);

            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入设备Id"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_device where id=@id and is_delete=0", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("设备不存在");
                DataRow row = ds.Tables[0].Rows[0];

                int action_id = 135;

                string remark2 = "设备名:" + row["name"].ToString();

                remark2 += ",是否录音:" + (status == 1 ? "是" : "否");

                long logId = ActionLog.AddLog(conn, action_id, id, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();

                    commandText.Append("update dev_device set ");
                    commandText.Append("is_auto_record=@is_auto_record ");
                    commandText.Append(" where id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@is_auto_record", status));
                    parameters.Add(new MySqlParameter("@id", id));
                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray(), false);

                    transaction.Commit();

                    DeviceCommand devCommand = new DeviceCommand(this.tokenHex, id);
                    byte[] command = devCommand.CreateRecordCmd(status == 1);
                    IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(Helper.GetLocalServIp()), Helper.GetLocalServPort());
                    JsonMsg<byte[]> msg = UdpHelper.SendCommand(command, iPEndPoint);

                    if (msg.code != 200) return ErrorJson(msg.message);


                    if (SendToCloudEnable())
                    {
                        SyncActions.Request("api/devices/" + GetSchoolId().ToString() + "/" + id.ToString(), RestSharp.Method.PUT, new
                        {
                            id,
                            name = row["name"].ToString(),
                            ip = row["ip"].ToString(),
                            device_type = Int32.Parse(row["device_type"].ToString()),
                            room_id = Int32.Parse(row["room_id"].ToString()),
                            group_id = Int32.Parse(row["group_id"].ToString()),
                            is_auto_save = Int32.Parse(row["is_auto_save"].ToString()),
                            is_auto_record = status
                        });
                    }

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
        [Power("admin,manage")]
        [Route("api/devices/{id}/autosave")]
        public IHttpActionResult UpdateAutoSave(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();
            int status = GetInt("status", 0);

            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入设备Id"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_device where id=@id and is_delete=0", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("设备不存在");
                DataRow row = ds.Tables[0].Rows[0];

                int action_id = 136;

                string remark2 = "设备名:" + row["name"].ToString();

                remark2 += ",是否保存声学数据:" + (status == 1 ? "是" : "否");

                long logId = ActionLog.AddLog(conn, action_id, id, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();

                    commandText.Append("update dev_device set ");
                    commandText.Append("is_auto_save=@is_auto_save ");
                    commandText.Append(" where id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@is_auto_save", status));
                    parameters.Add(new MySqlParameter("@id", id));
                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray(), false);

                    transaction.Commit();

                    if (SendToCloudEnable())
                    {
                        SyncActions.Request("api/devices/" + GetSchoolId().ToString() + "/" + id.ToString(), RestSharp.Method.PUT, new
                        {
                            id,
                            name = row["name"].ToString(),
                            ip = row["ip"].ToString(),
                            device_type = Int32.Parse(row["device_type"].ToString()),
                            room_id = Int32.Parse(row["room_id"].ToString()),
                            group_id = Int32.Parse(row["group_id"].ToString()),
                            is_auto_save = status,
                            is_auto_record = Int32.Parse(row["is_auto_record"].ToString())
                        });
                    }

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
        [Power("admin,manage")]
        [Route("api/devices/{id=0}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                if (id == 0)
                {
                    throw new HttpResponseException(Error("请输入设备Id"));
                }

                using (conn = new MySqlConnection(Constr()))
                {
                    conn.Open();


                    List<MySqlParameter> parameters = new List<MySqlParameter>();


                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@id", id));
                    DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_device where id=@id and is_delete=0", parameters.ToArray());
                    if (DsEmpty(ds)) return ErrorJson("设备不存在");
                    DataRow row = ds.Tables[0].Rows[0];

                    int action_id = 113;
                    int device_id = id;
                    string remark = "设备:" + row["name"].ToString() + ",Ip地址:" + row["ip"].ToString();
                    long logId = ActionLog.AddLog(conn, action_id, device_id, 0, userInfo.username, userInfo.id, remark);


                    MySqlTransaction transaction = conn.BeginTransaction();
                    try
                    {
                        DeviceCommand devCommand = new DeviceCommand(this.tokenHex, id);
                        byte[] command = devCommand.CreateDeleteCmd();
                        IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(Helper.GetLocalServIp()), Helper.GetLocalServPort());
                        JsonMsg<byte[]> msg = UdpHelper.SendCommand(command, iPEndPoint);



                        StringBuilder commandText = new StringBuilder();
                        commandText.Append("update dev_device set is_delete = 1 where id=@id");

                        parameters.Clear();
                        parameters.Add(new MySqlParameter("@id", id));

                        MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray());


                        parameters.Clear();
                        MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, "update sch_room set device_id=0 where device_id=" + id.ToString(), parameters.ToArray());

                        if (SendToCloudEnable())
                        {
                            SyncActions.Request("api/devices/" + GetSchoolId().ToString() + "/" + id.ToString(), RestSharp.Method.DELETE, new
                            {
                            });
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
            catch (Exception ex)
            {
                return ErrorJson(ex.Message);
            }
        }


        [HttpGet]
        [Route("api/devices/tree")]
        public IHttpActionResult Get(int group_id = 0)
        {
            dynamic array = new JArray();
            List<int> selectedArr = new List<int>();


            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                DataSet childDs;
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select name,id from dev_group order by sort");
                foreach (DataRow group in ds.Tables[0].Rows)
                {
                    dynamic children = new JArray();

                    childDs = MySqlHelper.ExecuteDataset(conn, "select name,ip,id from dev_device where group_id=" + group["id"].ToString() + " and is_delete = 0 order by CONVERT(name using gbk);");
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
        [Route("api/devices/{id}")]
        public IHttpActionResult Get(int id,
               string dict = ""
               )
        {
            try
            {
                Hashtable dictHashtable;

                using (conn = new MySqlConnection(Constr()))
                {
                    conn.Open();

                    String columns = "dev.*,grp.name as group_name,room.name as room_name,room.reverb_time";

                    StringBuilder commandText = new StringBuilder("select  " + columns +
                        "  from dev_device  dev " +
                        "left join sch_room room on dev.room_id = room.id and room.is_delete=0 " +
                        "left join dev_group grp on dev.group_id = grp.id " +
                        " where dev.id=@id and dev.is_delete=0");
                    List<MySqlParameter> parameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@id", id)
                };

                    DataSet ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                    if (DsEmpty(ds))
                    {
                        throw new HttpResponseException(Error("设备不存在"));
                    }
                    DataRow row = ds.Tables[0].Rows[0];

                    DeviceCommand devCommand = new DeviceCommand(this.tokenHex, id);
                    byte[] command = devCommand.CreateQueryParamsCmd();
                    IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(Helper.GetLocalServIp()), Helper.GetLocalServPort());
                    JsonMsg<byte[]> msg = UdpHelper.SendCommand(command, iPEndPoint);
                    LogHelper.GetInstance.Write("getDeviceDetail", msg.message + ",ip=" + Helper.GetLocalServIp() + ",port=" + Helper.GetLocalServPort());


                    if (msg.code != 200) return ErrorJson(msg.code, msg.message);

                    dynamic json = null;

                    switch (Convert.ToInt32(row["device_type"]))
                    {
                        case 4:
                            Services.Version4.DeviceInfoJson infoJson4 = new Services.Version4.DeviceInfoJson();
                            if (msg.data[0x138] != 4)
                            {
                                return ErrorJson("设备类型错误！");
                            }
                            json = infoJson4.GetJsonFromHex(row, msg.data);
                            break;
                        case 5:
                            Services.Version5.DeviceInfoJson infoJson5 = new Services.Version5.DeviceInfoJson();
                            if(msg.data[0x138]!=5)
                            {
                                return ErrorJson("设备类型错误！");
                            }
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
                    commandText.Append(" where id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@name", json.id));
                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@gateway", json.arm.gateway));
                    parameters.Add(new MySqlParameter("@mark", json.arm.mark));
                    parameters.Add(new MySqlParameter("@mac", json.arm.mac));
                    parameters.Add(new MySqlParameter("@arm_version", json.arm.version));
                    parameters.Add(new MySqlParameter("@dsp_version", json.dsp.version));

                    MySqlHelper.ExecuteNonQuery(conn, commandText.ToString(), parameters.ToArray());


                    dictHashtable = GetDict(conn, dict);

                    var result = new { detail = json, dict = dictHashtable };

                    return SuccessJson(result);
                }
            }
            catch (Exception ex)
            {
                return ErrorJson(ex.Message);
            }


        }

    }
}
