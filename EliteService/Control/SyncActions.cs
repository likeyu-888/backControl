using EliteService;
using EliteService.Control;
using EliteService.Utility;
using MySql.Data.MySqlClient;
using RestSharp;
using System;
using System.Configuration;

namespace Elite.WebServer.Services
{
    public class SyncActions
    {

        /// <summary>
        /// 云平台地址
        /// </summary>
        /// <returns></returns>
        public static string GetCloudServerUrl()
        {
            try
            {
                if (RedisHelper.Exists("cloud_server_url"))
                {
                    return RedisHelper.Get<string>("cloud_server_url");
                }
                else
                {
                    string cloudServUrl = ConfigurationManager.AppSettings["cloudServUrl"].ToString();
                    RedisHelper.Set("cloud_server_url", cloudServUrl, 5);

                    return cloudServUrl;
                }
                
            }
            catch
            {
                return "";
            }

        }

        /// <summary>
        /// 云平台地址
        /// </summary>
        /// <returns></returns>
        public static int GetSchoolId()
        {
            try
            {
                if (RedisHelper.Exists("school_id")) return RedisHelper.Get<int>("school_id");

                string constr = Helper.GetConstr();
                using (MySqlConnection conn = new MySqlConnection(constr))
                {
                    conn.Open();
                    object value = MySqlHelper.ExecuteScalar(conn, "select value from sys_config_item where item_key='school_id'", null);
                    if (value != null)
                    {
                        RedisHelper.Set("school_id", Convert.ToInt32(value));
                        return Convert.ToInt32(value);
                    }
                    return 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        public static bool SendToCloudEnable()
        {
            try
            {
                if (!RedisHelper.Exists("school_id")) return false;
                if (RedisHelper.Get<int>("school_id") == 0) return false;
                if (!RedisHelper.Exists("reg_password")) return false;
                if (string.IsNullOrEmpty(RedisHelper.Get<string>("reg_password"))) return false;
                return true;
            }
            catch
            {
                return true;
            }
        }

        public static IRestResponse Request(
            string api_name,
            Method method,
            dynamic sendJson,
            string filePath = ""
            )
        {

            Console.WriteLine("api_name=" + api_name);

            IRestResponse response = RequestAction(api_name, method, sendJson, filePath);

            if ((response.StatusCode == System.Net.HttpStatusCode.Unauthorized) || (response.StatusCode == System.Net.HttpStatusCode.Forbidden))
            {
                try
                {
                    if (RedisHelper.Exists("cloud_server_token")) RedisHelper.Remove("cloud_server_token");
                    if ((!api_name.Equals("api/cloud/tokens")) || (method != Method.POST))
                    {
                        CloudClient.LoginToCloud();
                        response = RequestAction(api_name, method, sendJson, filePath);
                    }
                }
                catch
                {
                }
            }
            if (GlobalData.IsDebug)
            {
                //if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    LogHelper.GetInstance.Write("request cloud server statuscode:", response.StatusCode.ToString());
                    LogHelper.GetInstance.Write("request cloud server api:", api_name);
                    LogHelper.GetInstance.Write("request cloud server method:", method.ToString());
                    LogHelper.GetInstance.Write("request cloud server error:", response.Content);
                }
            }
            return response;
        }

        public static IRestResponse RequestAction(
            string api_name,
            Method method,
            dynamic sendJson,
            string filePath = ""
            )
        {
            try
            {
                RestClient client = new RestClient(GetCloudServerUrl());//指定请求的url
                RestRequest request = new RestRequest(api_name, method);//指定请求的方式，如果Post则改成Method.POST

                request.AddHeader("x-auth-token", RedisHelper.Get<string>("cloud_server_token"));
                request.AddJsonBody(sendJson);
                if (!string.IsNullOrEmpty(filePath))
                {
                    request.AddFile("file", filePath);
                }

                IRestResponse response = client.Execute(request);
                return response; // 未处理的content是string
            }
            catch (Exception ex)
            {
                LogHelper.GetInstance.Write("request cloud server error:", ex.Message);
                return null;
            }

        }

        public static string RequestSync(
            string api_name,
            Method method,
            dynamic sendJson,
            string filePath = ""
            )
        {
            try
            {
                RestClient client = new RestClient(GetCloudServerUrl());//指定请求的url
                RestRequest request = new RestRequest(api_name, method);//指定请求的方式，如果Post则改成Method.POST

                request.AddHeader("x-auth-token", RedisHelper.Get<string>("cloud_server_token"));
                request.AddJsonBody(sendJson);
                if (!string.IsNullOrEmpty(filePath))
                {
                    request.AddFile("file", filePath);
                }

                client.ExecuteAsync(request, response =>
                {
                });

                return "OK";
            }
            catch (Exception ex)
            {
                LogHelper.GetInstance.Write("request cloud server error:", ex.Message);
                return ex.Message;
            }

        }

        public static IRestResponse Download(
            string api_name
            )
        {
            IRestResponse response = DownloadAction(api_name);
            if ((response.StatusCode == System.Net.HttpStatusCode.Unauthorized) || (response.StatusCode == System.Net.HttpStatusCode.Forbidden))
            {
                try
                {
                    if (RedisHelper.Exists("cloud_server_token")) RedisHelper.Remove("cloud_server_token");
                }
                catch { }
                CloudClient.LoginToCloud();
                response = DownloadAction(api_name);
            }
            return response;

        }

        public static IRestResponse DownloadAction(
            string api_name
            )
        {
            try
            {
                RestClient client = new RestClient(GetCloudServerUrl());//指定请求的url
                RestRequest request = new RestRequest(api_name, Method.GET);

                request.AddHeader("x-auth-token", RedisHelper.Get<string>("cloud_server_token"));

                IRestResponse response = client.Execute(request);
                return response;
            }
            catch (Exception ex)
            {
                LogHelper.GetInstance.Write("download error:", ex.Message);
                return null;
            }

        }

    }
}
