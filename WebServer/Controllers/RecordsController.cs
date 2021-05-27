using Elite.WebServer.Base;
using Elite.WebServer.Utility;
using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Text;
using System.Web.Http;

namespace Elite.WebServer.Controllers
{
    public class RecordsController : BaseController
    {
        [HttpGet]
        [Power("admin,manage")]
        [Route("api/devices/{id}/records")]
        public IHttpActionResult Get(int id = 0,
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

                String columns = "rec.id, rec.create_time,rec.file_path,dev.ip,dev.name,rec.size ";

                StringBuilder commandText = new StringBuilder("select SQL_CALC_FOUND_ROWS " + columns +
                    " from dev_record  rec " +
                    " left join dev_device dev on rec.device_id = dev.id" +
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

                if (sort_column.Equals("ip"))
                {
                    commandText.Append(QueryOrder("dev." + sort_column, sort_direction));
                }
                else
                {
                    commandText.Append(QueryOrder("rec." + sort_column, sort_direction));
                }
                commandText.Append(QueryLimit(page_size, page));

                ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                total = TotalCount(conn);

                dictHashtable = GetDict(conn, dict);

            }

            int pageCount = PageCount(total, page_size);


            return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(total, page, page_size, pageCount, ds.Tables[0], dictHashtable));
        }

    }
}
