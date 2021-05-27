using Elite.WebServer.Base;
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
    public class HistoriesController : BaseController
    {
        [HttpGet]
        [Power("141")]
        [Route("api/devices/{schoolId}/{id}/histories")]
        public IHttpActionResult Get(int id = 0,
            string create_time = "",
            string listen_efficiency = "",
            string snr = "",
            int page = 1,
            int page_size = 10,
            string sort_column = "id",
            string sort_direction = "DESC",
            string dict = ""
            )
        {
            int school_id = GetSchoolId();

            try
            {
                if (school_id == 0)
                {
                    school_id = userInfo.school_id;
                }
                if (school_id == 0)
                {
                    return ErrorJson("请输入学校Id");
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

                    dictHashtable = GetDict(conn, dict, school_id);

                    string tableName = "dev_history_" + school_id.ToString() + "_" + id.ToString();

                    bool exists = MysqlHelper.Exists(conn, "SELECT count(*) FROM information_schema.tables WHERE table_name ='" + tableName + "'");

                    if (!exists)
                    {
                        return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(0, page, page_size, 0, new string[] { }, dictHashtable));
                    }


                    String columns = "his.id, his.create_time,his.snr,his.listen_efficiency,his.attendence_difficulty,his.anbient_noice ";

                    StringBuilder commandText = new StringBuilder("select SQL_CALC_FOUND_ROWS " + columns +
                        "from  " + tableName + " his " +
                        "where  1=1 ");
                    List<MySqlParameter> parameters = new List<MySqlParameter>();

                    if (!string.IsNullOrEmpty(create_time))
                    {
                        string[] times = create_time.Split(',');
                        if (DataValidate.IsDate(times[0]))
                        {
                            commandText.Append(" and (his.create_time >=@create_time_begin) ");
                            parameters.Add(new MySqlParameter("@create_time_begin", times[0]));
                        }
                        if ((times.Length == 2) && (DataValidate.IsDate(times[1])))
                        {
                            commandText.Append(" and his.create_time<@create_time_end");
                            parameters.Add(new MySqlParameter("@create_time_end", times[1]));
                        }
                    }

                    if (!string.IsNullOrEmpty(snr))
                    {
                        string[] snrs = snr.Split(',');
                        if (!string.IsNullOrEmpty(snrs[0]))
                        {
                            commandText.Append(" and (his.snr >=@snr_begin) ");
                            parameters.Add(new MySqlParameter("@snr_begin", snrs[0]));
                        }
                        if ((snrs.Length == 2) && (!string.IsNullOrEmpty(snrs[0])))
                        {
                            commandText.Append(" and his.snr<=@snr_end");
                            parameters.Add(new MySqlParameter("@snr_end", snrs[1]));
                        }
                    }

                    if (!string.IsNullOrEmpty(listen_efficiency))
                    {
                        string[] items = listen_efficiency.Split(',');
                        if (!string.IsNullOrEmpty(items[0]))
                        {
                            commandText.Append(" and (his.listen_efficiency >=@effi_begin) ");
                            parameters.Add(new MySqlParameter("@effi_begin", items[0]));
                        }
                        if ((items.Length == 2) && (!string.IsNullOrEmpty(items[0])))
                        {
                            commandText.Append(" and his.listen_efficiency<=@effi_end");
                            parameters.Add(new MySqlParameter("@effi_end", items[1]));
                        }
                    }

                    commandText.Append(QueryOrder("his." + sort_column, sort_direction));
                    commandText.Append(QueryLimit(page_size, page));

                    ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                    total = TotalCount(conn);



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
        [Power("142")]
        [Route("api/devices/{schoolId}/{id}/histories")]
        public IHttpActionResult Post(JObject obj)
        {
            GetRequest(obj);

            int id = GetId();

            int school_id = GetSchoolId();
            int device_id = id;

            int listen_efficiency = GetInt("listen_efficiency");
            int attendence_difficulty = GetInt("attendence_difficulty");
            decimal snr = GetDecimal("snr");
            decimal anbient_noice = GetDecimal("anbient_noice");
            int status = GetInt("status");

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

                try
                {
                    string tableName = "dev_history_" + school_id.ToString() + "_" + device_id.ToString();

                    bool exists = MysqlHelper.Exists(conn, "SELECT count(*) FROM information_schema.tables WHERE table_name ='" + tableName + "'");

                    if (!exists)
                    {
                        string sql = "CREATE TABLE `dev_history_" + school_id.ToString() + "_" + device_id.ToString() + "` (" +
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


                        MySqlHelper.ExecuteNonQuery(conn, sql);
                    }

                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("insert into " + tableName + " set ");
                    commandText.Append("school_id=@school_id,");
                    commandText.Append("device_id=@device_id,");
                    commandText.Append("snr=@snr,");
                    commandText.Append("listen_efficiency=@listen_efficiency,");
                    commandText.Append("attendence_difficulty=@attendence_difficulty,");
                    commandText.Append("anbient_noice=@anbient_noice");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@school_id", school_id));
                    parameters.Add(new MySqlParameter("@device_id", device_id));
                    parameters.Add(new MySqlParameter("@snr", snr));
                    parameters.Add(new MySqlParameter("@listen_efficiency", listen_efficiency));
                    parameters.Add(new MySqlParameter("@attendence_difficulty", attendence_difficulty));
                    parameters.Add(new MySqlParameter("@anbient_noice", anbient_noice));

                    MySqlHelper.ExecuteNonQuery(conn, commandText.ToString(), parameters.ToArray());
                    commandText.Clear();
                    commandText.Append("update dev_device set ");
                    commandText.Append("status=@status,");
                    commandText.Append("snr=@snr,");
                    commandText.Append("listen_efficiency=@listen_efficiency,");
                    commandText.Append("attendence_difficulty=@attendence_difficulty,");
                    commandText.Append("anbient_noice=@anbient_noice ");
                    commandText.Append("where id=@id and school_id=@school_id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@school_id", school_id));
                    parameters.Add(new MySqlParameter("@id", device_id));
                    parameters.Add(new MySqlParameter("@status", status));
                    parameters.Add(new MySqlParameter("@snr", snr));
                    parameters.Add(new MySqlParameter("@listen_efficiency", listen_efficiency));
                    parameters.Add(new MySqlParameter("@attendence_difficulty", attendence_difficulty));
                    parameters.Add(new MySqlParameter("@anbient_noice", anbient_noice));

                    MySqlHelper.ExecuteNonQuery(conn, commandText.ToString(), parameters.ToArray());
                    return SuccessJson();
                }
                catch (Exception ex)
                {
                    return ErrorJson(ex.Message);
                }

            }
        }

    }
}
