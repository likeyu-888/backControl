using Elite.WebServer.Utility;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Configuration;
using System.Net;

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
            if (RedisHelper.Exists("cloud_server_url")) return RedisHelper.Get<string>("cloud_server_url");

            string cloudServUrl = ConfigurationManager.AppSettings["cloudServUrl"];
            RedisHelper.Set("cloud_server_url", cloudServUrl,5);

            return cloudServUrl;
        }

        /// <summary>
        /// 云平台地址
        /// </summary>
        /// <returns></returns>
        public static int GetSchoolId()
        {
            if (RedisHelper.Exists("school_id")) return RedisHelper.Get<int>("school_id");

            string constr = Constr();
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

        public static String Constr()
        {
            return ConfigurationManager.AppSettings["constr"];
        }

        public static bool SendToCloudEnable()
        {
            if (!RedisHelper.Exists("school_id")) return false;
            if (RedisHelper.Get<int>("school_id") == 0) return false;
            if (!RedisHelper.Exists("reg_password")) return false;
            if (string.IsNullOrEmpty(RedisHelper.Get<string>("reg_password"))) return false;
            return true;
        }

        public static void LoginToCloud()
        {
            if (RedisHelper.Exists("cloud_server_token")) return;

            string apiName = "api/cloud/tokens";
            IRestResponse response = SyncActions.Request(apiName, RestSharp.Method.POST, new
            {
                school_id = SyncActions.GetSchoolId(),
                password = RedisHelper.Get<string>("reg_password")
            });

            if (response.StatusCode != HttpStatusCode.OK)
            {
                LogHelper.GetInstance.Write("login to cloud server error：", response.Content);
                return;
            }

            JObject obj = JsonConvert.DeserializeObject(response.Content) as JObject;
            RedisHelper.Set("cloud_server_token", obj["data"]["token"].ToString());

        }

        public static IRestResponse Request(
            string api_name,
            Method method,
            dynamic sendJson,
            string filePath = ""
            )
        {
            IRestResponse response = RequestAction(api_name, method, sendJson, filePath);
            if ((response.StatusCode == System.Net.HttpStatusCode.Unauthorized) || (response.StatusCode == System.Net.HttpStatusCode.Forbidden))
            {
                if (RedisHelper.Exists("cloud_server_token")) RedisHelper.Remove("cloud_server_token");
                LoginToCloud();
                response = RequestAction(api_name, method, sendJson, filePath);
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
                if (RedisHelper.Exists("is_debug") && (RedisHelper.Get("is_debug").ToString().Equals("1")))
                {
                    LogHelper.GetInstance.Write("request cloud server api_name:", api_name);
                }

                RestClient client = new RestClient(GetCloudServerUrl());//指定请求的url
                RestRequest request = new RestRequest(api_name, method);//指定请求的方式，如果Post则改成Method.POST

                request.AddHeader("x-auth-token", RedisHelper.Get<string>("cloud_server_token"));
                request.AddJsonBody(sendJson);
                if (!string.IsNullOrEmpty(filePath))
                {
                    request.AddFile("file", filePath);
                }
                IRestResponse response = client.Execute(request);
                if (RedisHelper.Exists("is_debug") && (RedisHelper.Get("is_debug").ToString().Equals("1")))
                {
                    LogHelper.GetInstance.Write("request cloud server success:", response.Content);
                }

                return response; // 未处理的content是string
            }
            catch (Exception ex)
            {
                LogHelper.GetInstance.Write("request cloud server error:", ex.Message);
                return null;
            }

        }


    }
}
