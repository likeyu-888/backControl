using Elite.WebServer.Base;
using Elite.WebServer.Services;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace Elite.WebServer.Controllers
{
    public class DownloadsController : BaseController
    {

        [HttpGet]
        [Power("admin,manage")]
        [Route("api/downloads/{id}")]
        public HttpResponseMessage Get(int id, string token = "")
        {
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入下载文件Id"));
            }


            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();

                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_record where id=@id", parameters.ToArray());
                if (DsEmpty(ds)) throw new HttpResponseException(Error("文件不存在"));
                DataRow row = ds.Tables[0].Rows[0];

                object obj = MySqlHelper.ExecuteScalar(conn, "select value from sys_config_item where item_key='service_root'");
                if (obj is null) throw new HttpResponseException(Error("服务器目录参数不存在"));
                string serviceRoot = obj.ToString();

                int action_id = 131;

                string filePath = row["file_path"].ToString();
                string physicalPath = serviceRoot + filePath;

                if (!File.Exists(physicalPath))
                {
                    throw new HttpResponseException(Error("文件不存在"));
                }

                long logId = ActionLog.AddLog(conn, action_id, (int)row["device_id"], 0, userInfo.username, userInfo.id, filePath);

                try
                {
                    string filename = System.IO.Path.GetFileName(physicalPath);

                    var stream = new FileStream(physicalPath, FileMode.Open);
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = new StreamContent(stream);
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = filename
                    };

                    ActionLog.Finished(conn, logId);
                    return response;
                }
                catch (Exception ex)
                {
                    try
                    {
                        return new HttpResponseMessage(HttpStatusCode.NoContent);
                    }
                    catch (Exception en)
                    {
                        return new HttpResponseMessage(HttpStatusCode.NoContent);
                    }

                }
            }
        }
    }
}
