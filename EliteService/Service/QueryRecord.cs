using Elite.WebServer.Services;
using EliteService.DTO;
using EliteService.Utility;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Text;

namespace EliteService.Service
{
    class QueryRecord
    {
        public static JsonMsg Get(int id = 0,
          string create_time = "",
          int page = 1,
          int page_size = 10,
          string sort_column = "id",
          string sort_direction = "DESC"
          )
        {
            try
            {
                DataSet ds = null;
                int total = 0;

                using (MySqlConnection conn = new MySqlConnection(Helper.GetConstr()))
                {
                    conn.Open();

                    String columns = "rec.id,rec.device_id, rec.create_time,rec.size ";

                    StringBuilder commandText = new StringBuilder("select SQL_CALC_FOUND_ROWS " + columns +
                        " from dev_record  rec " +
                        " where  1=1 ");
                    List<MySqlParameter> parameters = new List<MySqlParameter>();

                    if (id != 0)
                    {
                        commandText.Append(" and rec.device_id=@device_id");
                        parameters.Add(new MySqlParameter("@device_id", id));
                    }

                    if (!string.IsNullOrEmpty(create_time))
                    {
                        string[] times = create_time.Split(',');
                        if (DataValidate.IsDate(times[0]))
                        {
                            commandText.Append(" and (rec.create_time >=@create_time_begin) ");
                            parameters.Add(new MySqlParameter("@create_time_begin", times[0]));
                        }
                        if ((times.Length == 2) && (DataValidate.IsDate(times[1])))
                        {
                            commandText.Append(" and rec.create_time<@create_time_end");
                            parameters.Add(new MySqlParameter("@create_time_end", times[1]));
                        }
                    }

                    commandText.Append(SqlHelper.QueryOrder("rec." + sort_column, sort_direction));
                    commandText.Append(SqlHelper.QueryLimit(page_size, page));

                    ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                    total = SqlHelper.TotalCount(conn);
                }

                int pageCount = SqlHelper.PageCount(total, page_size);

                JsonMsg<PagedData<DataTable>> obj = ReturnMsg.GetJsonMsg(total, page, page_size, pageCount, ds.Tables[0], null);
                JsonMsg result = new JsonMsg
                {
                    code = obj.code,
                    message = obj.message,
                    data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj))
                };
                return result;
            }
            catch
            {
                JsonMsg result = new JsonMsg
                {
                    code = 500,
                    message = "异常错误",
                    data = new byte[0] { }
                };
                return result;
            }
        }

        public static JsonMsg DownRecord(int id)
        {
            DataSet ds = null;
            try
            {

                using (MySqlConnection conn = new MySqlConnection(Helper.GetConstr()))
                {
                    conn.Open();
                    String columns = "rec.id, rec.device_id, rec.create_time,rec.size, rec.file_path ";

                    StringBuilder commandText = new StringBuilder("select SQL_CALC_FOUND_ROWS " + columns +
                        " from dev_record  rec " +
                        " where  id=@id ");
                    List<MySqlParameter> parameters = new List<MySqlParameter>();
                    parameters.Add(new MySqlParameter("@id", id));

                    ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                    if (ds.Tables[0].Rows.Count <= 0)
                    {
                        JsonMsg result = new JsonMsg
                        {
                            code = 500,
                            message = "文件不存在"
                        };
                        return result;
                    }
                    DataRow record = ds.Tables[0].Rows[0];

                    string filePath = record["file_path"].ToString();

                    int recordId = Convert.ToInt32(record["id"]);

                    string apiName = "api/devices/records/" + SyncActions.GetSchoolId().ToString() + "/" + record["device_id"].ToString() + "/" + recordId.ToString();

                    string physicalPath = AppDomain.CurrentDomain.BaseDirectory + filePath;

                    IRestResponse response = SyncActions.Request(apiName, Method.POST, new { }, physicalPath);

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return new JsonMsg { code = 500, message = "文件上传失败" + response.Content };
                    }

                    return new JsonMsg { code = 200, message = "操作成功" };
                }
            }
            catch (Exception ex)
            {
                return new JsonMsg { code = 500, message = ex.Message };
            }
        }
    }
}