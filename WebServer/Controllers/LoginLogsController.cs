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
    public class LoginLogsController : BaseController
    {
        [HttpGet]
        [Power("admin")]
        [Route("api/loginlogs")]
        public IHttpActionResult Get(
            int user_id = 0,
            string username = "",
            string create_time = "",
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

                String columns = "id, create_time, username, status, (case status " +
                    " when 0 then \"失败\" " +
                    " when 1 then \"成功\" " +
                    " end) as status_text" +
                    ", ip, remark, action_id,(case action_id " +
                    " when 0 then \"登录\" " +
                    " when 1 then \"注销\" " +
                    " end) as action_text";

                StringBuilder commandText = new StringBuilder("select SQL_CALC_FOUND_ROWS " + columns +
                    "  from log_login  where 1=1 ");
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                if (user_id != 0)
                {
                    commandText.Append(" and user_id = @user_id");
                    parameters.Add(new MySqlParameter("@user_id", user_id));
                }
                if (!string.IsNullOrEmpty(username))
                {
                    commandText.Append(" and username = @username");
                    parameters.Add(new MySqlParameter("@username", username));
                }

                if (!string.IsNullOrEmpty(create_time))
                {
                    string[] times = create_time.Split(',');
                    if (DataValidate.IsDate(times[0]))
                    {
                        commandText.Append(" and (create_time >=@create_time_begin) ");
                        parameters.Add(new MySqlParameter("@create_time_begin", times[0]));
                    }
                    if ((times.Length == 2) && (DataValidate.IsDate(times[1])))
                    {
                        commandText.Append(" and create_time<@create_time_end");
                        parameters.Add(new MySqlParameter("@create_time_end", times[1]));
                    }
                }


                commandText.Append(QueryOrder(sort_column, sort_direction));
                commandText.Append(QueryLimit(page_size, page));

                ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                total = TotalCount(conn);

                dictHashtable = GetDict(conn, dict);

            }

            int pageCount = PageCount(total, page_size);


            return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(total, page, page_size, pageCount, ds.Tables[0], dictHashtable));
        }


        [HttpDelete]
        [Power("admin")]
        [Route("api/loginlogs")]
        public IHttpActionResult Delete()
        {
            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                int action_id = 801;
                int device_id = 0;
                string remark = "";
                long logId = ActionLog.AddLog(conn, action_id, device_id, 0, userInfo.username, userInfo.id, remark);


                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("delete from log_login");

                    List<MySqlParameter> parameters = new List<MySqlParameter>();
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
        [Power("admin")]
        [Route("api/loginlogs/batch")]
        public IHttpActionResult Delete(JObject obj)
        {
            GetRequest(obj);

            string ids = GetString("ids");
            if (string.IsNullOrEmpty(ids))
            {
                throw new HttpResponseException(Error("请选择需要删除的日志Id"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                int action_id = 802;
                int device_id = 0;
                string remark = "ids:" + ids;
                long logId = ActionLog.AddLog(conn, action_id, device_id, 0, userInfo.username, userInfo.id, remark);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("delete from log_login where 1=2  ");

                    List<MySqlParameter> parameters = new List<MySqlParameter>();
                    string[] idArr = ids.Trim().Split(',');

                    if (idArr.Length > 0)
                    {
                        int i = 0;
                        foreach (string recordId in idArr)
                        {
                            i++;
                            commandText.Append(" or id=@id_" + i.ToString());
                            parameters.Add(new MySqlParameter("@id_" + i.ToString(), recordId));
                        }
                    }
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
