using Elite.WebServer.Base;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Http;

namespace Elite.WebServer.Controllers
{
    public class UploadsController : BaseController
    {
        [HttpPut]
        [HttpPost]
        [Power("204")]
        [Route("api/uploads")]
        public IHttpActionResult Post()
        {
            GetRequest();

            HttpRequest request = HttpContext.Current.Request;
            HttpFileCollection fileCollection = request.Files;

            if (fileCollection.Count <= 0)
            {
                throw new HttpResponseException(Error("请上传文件"));
            }

            HttpPostedFile httpPostedFile = fileCollection[0];

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>();

                try
                {
                    string path = System.Web.Hosting.HostingEnvironment.MapPath(@"/uploads");
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    path = System.Web.Hosting.HostingEnvironment.MapPath(@"/uploads/images");
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    string extension = Path.GetExtension(httpPostedFile.FileName);

                    string fileName = "";
                    int index = httpPostedFile.FileName.IndexOf('.');
                    if (index > 0)
                    {
                        fileName = httpPostedFile.FileName.Substring(0, index) + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;
                    }
                    else
                    {
                        fileName = httpPostedFile.FileName + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;
                    }

                    path = path + "/" + fileName;

                    httpPostedFile.SaveAs(path);

                    string imageUrl = "/uploads/images/" + fileName;

                    return SuccessJson(new { name = fileName, path = imageUrl });
                }
                catch (Exception ex)
                {
                    return ErrorJson(ex.Message);
                }
            }
        }

        [HttpDelete]
        [Power("204")]
        [Route("api/uploads")]
        public IHttpActionResult Delete(JObject obj)
        {
            GetRequest(obj);

            try
            {
                string file = GetString("file");
                string path = System.Web.Hosting.HostingEnvironment.MapPath(@"/uploads/images/" + file);

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                return SuccessJson();
            }
            catch (Exception ex)
            {
                return ErrorJson(ex.Message);
            }

        }
    }
}
