using Elite.WebServer.Base;
using Elite.WebServer.Services;
using Elite.WebServer.Utility;
using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Http;

namespace Elite.WebServer.Controllers
{
    public class DocumentsController : BaseController
    {
        [HttpGet]
        [Power("161")]
        [Route("api/documents")]
        public IHttpActionResult Get(
            string keyword = "",
            int document_type = 0,
            string update_time = "",
            int product_type = 0,
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

                String columns = "doc.id, doc.name, doc.document_type,doc.create_time,doc.update_time, doc.product_type,doc.file_extension,doc.size,doc.down_count";

                StringBuilder commandText = new StringBuilder("select SQL_CALC_FOUND_ROWS " + columns +
                    "  from svc_document doc where 1=1 ");
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                if (document_type != 0)
                {
                    commandText.Append(" and (doc.document_type=@document_type)");
                    parameters.Add(new MySqlParameter("@document_type", document_type));
                }

                if (product_type != 0)
                {
                    commandText.Append(" and (doc.product_type=@product_type)");
                    parameters.Add(new MySqlParameter("@product_type", product_type));
                }

                if (!string.IsNullOrEmpty(update_time))
                {
                    string[] times = update_time.Split(',');
                    if (DataValidate.IsDate(times[0]))
                    {
                        commandText.Append(" and (doc.update_time >=@create_time_begin) ");
                        parameters.Add(new MySqlParameter("@create_time_begin", times[0]));
                    }
                    if ((times.Length == 2) && (DataValidate.IsDate(times[1])))
                    {
                        commandText.Append(" and doc.update_time<@create_time_end");
                        parameters.Add(new MySqlParameter("@create_time_end", times[1]));
                    }
                }

                if (!string.IsNullOrEmpty(keyword))
                {
                    commandText.Append(" and ((doc.id =@keyword) or (doc.name like CONCAT('%',@keyword,'%')) )");
                    parameters.Add(new MySqlParameter("@keyword", keyword));
                }

                commandText.Append(QueryOrder("doc." + sort_column, sort_direction));
                commandText.Append(QueryLimit(page_size, page));

                ds = MySqlHelper.ExecuteDataset(conn, commandText.ToString(), parameters.ToArray());
                total = TotalCount(conn);

                dictHashtable = GetDict(conn, dict);

            }

            int pageCount = PageCount(total, page_size);


            return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(total, page, page_size, pageCount, ds.Tables[0], dictHashtable));
        }

        [HttpPost]
        [Power("162")]
        [Route("api/documents")]
        public IHttpActionResult Post()
        {
            GetRequest();

            string name = GetString("name");
            int documentType = GetInt("document_type", 0);
            int productType = GetInt("product_type", 0);

            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入产品名称"));
            }
            if (documentType == 0)
            {
                throw new HttpResponseException(Error("请选择文档类型"));
            }
            if (productType == 0)
            {
                throw new HttpResponseException(Error("请选择产品型号"));
            }

            HttpRequest request = HttpContext.Current.Request;
            HttpFileCollection fileCollection = request.Files;

            if (fileCollection.Count <= 0)
            {
                throw new HttpResponseException(Error("请上传文档"));
            }

            HttpPostedFile httpPostedFile = fileCollection[0];


            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@name", name),
                    new MySqlParameter("@product_type", productType)
                };

                bool exists = MysqlHelper.Exists(conn, "select count(*) from svc_document where name=@name and product_type=@product_type", parameters.ToArray());
                if (exists) return ErrorJson("文档名已存在");




                int action_id = 162;

                string remark2 = "文档名：" + name;

                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();


                try
                {
                    string path = System.Web.Hosting.HostingEnvironment.MapPath(@"/documents");
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
                    string virPath = "/documents/" + fileName;

                    httpPostedFile.SaveAs(path);



                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("insert into svc_document set ");
                    commandText.Append("name=@name,");
                    commandText.Append("document_type=@document_type,");
                    commandText.Append("product_type=@product_type,");
                    commandText.Append("file_extension='").Append(extension.Substring(1).ToUpper()).Append("',");
                    commandText.Append("size=").Append(httpPostedFile.ContentLength.ToString()).Append(",");
                    commandText.Append("file_path='").Append(virPath).Append("'");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@document_type", documentType));
                    parameters.Add(new MySqlParameter("@product_type", productType));

                    long groupId = MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray(), true);
                    if (groupId <= 0)
                    {
                        transaction.Rollback();
                        return ErrorJson("产品资料添加失败");
                    }
                    transaction.Commit();
                    ActionLog.Finished(conn, logId);
                    return SuccessJson(new { id = groupId });
                }
                catch (Exception ex)
                {
                    try
                    {
                        return ErrorJson(ex.Message);
                    }
                    catch (Exception en)
                    {
                        return ErrorJson(en.Message);
                    }

                }
            }
        }



        [HttpPost]
        [HttpPut]
        [Power("163")]
        [Route("api/documents/update/{id=0}")]
        public IHttpActionResult Put()
        {
            GetRequest();

            int id = GetId();
            string name = GetString("name");
            int documentType = GetInt("document_type", 0);
            int productType = GetInt("product_type", 0);

            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入文档Id"));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new HttpResponseException(Error("请输入产品名称"));
            }
            if (documentType == 0)
            {
                throw new HttpResponseException(Error("请选择文档类型"));
            }
            if (productType == 0)
            {
                throw new HttpResponseException(Error("请选择产品型号"));
            }

            HttpRequest request = HttpContext.Current.Request;
            HttpFileCollection fileCollection = request.Files;

            if (fileCollection.Count <= 0)
            {
                //throw new HttpResponseException(Error("请上传文档"));
            }

            string extension = "";
            string virPath = "";

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();

                List<MySqlParameter> parameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@id", id)
                };

                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from svc_document where  id=@id", parameters.ToArray());
                if (ds.Tables[0].Rows.Count <= 0) return ErrorJson("文档不存在");
                DataRow document = ds.Tables[0].Rows[0];

                parameters.Clear();
                parameters.Add(new MySqlParameter("@name", name));
                parameters.Add(new MySqlParameter("@product_type", productType));
                parameters.Add(new MySqlParameter("@id", id));

                bool exists = MysqlHelper.Exists(conn, "select count(*) from svc_document where name=@name and product_type=@product_type and id!=@id", parameters.ToArray());
                if (exists) return ErrorJson("文档名已存在");


                int action_id = 163;

                string remark2 = "文档名：" + name;

                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark2);

                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {


                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("update svc_document set ");
                    commandText.Append("name=@name,");

                    if (fileCollection.Count > 0)
                    {


                        string filePath = document["file_path"].ToString();

                        string physicalPath = System.Web.Hosting.HostingEnvironment.MapPath(filePath);

                        if (File.Exists(physicalPath))
                        {
                            File.Delete(physicalPath);
                        }


                        string path = System.Web.Hosting.HostingEnvironment.MapPath(@"/documents");
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                        HttpPostedFile httpPostedFile = fileCollection[0];

                        extension = Path.GetExtension(httpPostedFile.FileName);

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
                        virPath = "/documents/" + fileName;

                        httpPostedFile.SaveAs(path);

                        commandText.Append("file_extension='").Append(extension.Substring(1).ToUpper()).Append("',");
                        commandText.Append("size=").Append(httpPostedFile.ContentLength.ToString()).Append(",");
                        commandText.Append("file_path='").Append(virPath).Append("',");
                    }

                    commandText.Append("document_type=@document_type,");
                    commandText.Append("product_type=@product_type ");

                    commandText.Append(" where id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@name", name));
                    parameters.Add(new MySqlParameter("@document_type", documentType));
                    parameters.Add(new MySqlParameter("@product_type", productType));
                    parameters.Add(new MySqlParameter("@id", id));

                    MysqlHelper.ExecuteNonQuery(transaction, CommandType.Text, commandText.ToString(), parameters.ToArray(), true);

                    transaction.Commit();
                    ActionLog.Finished(conn, logId);

                    return SuccessJson();
                }
                catch (Exception ex)
                {
                    try
                    {
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
        [Power("164")]
        [Route("api/documents/{id=0}")]
        public IHttpActionResult Delete(int id)
        {
            if (id == 0)
            {
                throw new HttpResponseException(Error("请输入文档Id"));
            }

            using (conn = new MySqlConnection(Constr()))
            {
                conn.Open();


                List<MySqlParameter> parameters = new List<MySqlParameter>();


                parameters.Clear();
                parameters.Add(new MySqlParameter("@id", id));
                DataSet ds = MySqlHelper.ExecuteDataset(conn, "select * from svc_document where id=@id", parameters.ToArray());
                if (DsEmpty(ds)) return ErrorJson("文档不存在");
                DataRow row = ds.Tables[0].Rows[0];

                int action_id = 164;
                string remark = "文档名称:" + row["name"].ToString();
                long logId = ActionLog.AddLog(conn, action_id, 0, 0, 0, userInfo.username, userInfo.id, remark);


                MySqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    string filePath = row["file_path"].ToString();

                    string physicalPath = System.Web.Hosting.HostingEnvironment.MapPath(filePath);

                    if (File.Exists(physicalPath))
                    {
                        File.Delete(physicalPath);
                    }


                    StringBuilder commandText = new StringBuilder();
                    commandText.Append("delete from svc_document where id=@id");

                    parameters.Clear();
                    parameters.Add(new MySqlParameter("@id", id));

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
