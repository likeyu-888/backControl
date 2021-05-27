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
        [Power("161")]
        [Route("api/downloads/documents/{id}")]
        public HttpResponseMessage GetFile(int id, string token = "")
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
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from svc_document where id=@id", parameters.ToArray());
                if (DsEmpty(ds)) throw new HttpResponseException(Error("文件不存在"));
                DataRow row = ds.Tables[0].Rows[0];

                int action_id = 161;

                string filePath = row["file_path"].ToString();
                string physicalPath = System.Web.Hosting.HostingEnvironment.MapPath(filePath);

                if (!File.Exists(physicalPath))
                {
                    throw new HttpResponseException(Error("文件不存在"));
                }

                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, filePath);

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
                    MySqlHelper.ExecuteNonQuery(conn, "update svc_document set down_count  = down_count + 1 where id=" + id.ToString(), null);
                    ActionLog.Finished(conn, logId);
                    return response;
                }
                catch
                {
                    try
                    {
                        return new HttpResponseMessage(HttpStatusCode.NoContent);
                    }
                    catch
                    {
                        return new HttpResponseMessage(HttpStatusCode.NoContent);
                    }

                }
            }
        }

        [HttpGet]
        [Power("131")]
        [Route("api/downloads/records/{schoolId}/{recordId}")]
        public HttpResponseMessage GetRecords(int schoolId, int recordId, string token = "")
        {
            if (schoolId == 0)
            {
                throw new HttpResponseException(Error("请输入学校Id"));
            }
            if (recordId == 0)
            {
                throw new HttpResponseException(Error("请输入下载文件Id"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();

                parameters.Clear();
                parameters.Add(new MySqlParameter("@school_id", schoolId));
                parameters.Add(new MySqlParameter("@id", recordId));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from dev_record where school_id=@school_id and id=@id", parameters.ToArray());
                if (DsEmpty(ds)) throw new HttpResponseException(Error("文件不存在"));
                DataRow row = ds.Tables[0].Rows[0];

                int action_id = 131;

                string filePath = row["file_path"].ToString();
                string physicalPath = System.Web.Hosting.HostingEnvironment.MapPath(filePath);

                if (!File.Exists(physicalPath))
                {
                    throw new HttpResponseException(Error("文件不存在"));
                }

                long logId = ActionLog.AddLog(conn, action_id, schoolId, Convert.ToInt32(row["device_id"]), 0, userInfo.username, userInfo.id, filePath);

                try
                {
                    string filename = Path.GetFileName(physicalPath);

                    var stream = new FileStream(physicalPath, FileMode.Open);
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = new StreamContent(stream);
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = filename
                    };
                    MySqlHelper.ExecuteNonQuery(conn, "update dev_record set down_count  = down_count + 1 where school_id=" + schoolId.ToString() + " and id=" + recordId.ToString(), null);
                    ActionLog.Finished(conn, logId);
                    return response;
                }
                catch
                {
                    try
                    {
                        return new HttpResponseMessage(HttpStatusCode.NoContent);
                    }
                    catch
                    {
                        return new HttpResponseMessage(HttpStatusCode.NoContent);
                    }

                }
            }
        }
    }
}
