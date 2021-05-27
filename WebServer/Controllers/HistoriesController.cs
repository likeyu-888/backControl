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
    public class HistoriesController : BaseController
    {
        [HttpGet]
        [Route("api/devices/{id}/histories")]
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
            DataSet ds = null;
            Hashtable dictHashtable;
            int total = 0;

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                String columns = "his.id, his.create_time,his.snr,his.listen_efficiency,his.attendence_difficulty,his.anbient_noice ";

                StringBuilder commandText = new StringBuilder("select SQL_CALC_FOUND_ROWS " + columns +
                    "from dev_history_" + id.ToString() + " his " +
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

                dictHashtable = GetDict(conn, dict);

            }

            int pageCount = PageCount(total, page_size);


            return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(total, page, page_size, pageCount, ds.Tables[0], dictHashtable));
        }

    }
}
