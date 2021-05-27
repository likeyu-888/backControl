using Elite.WebServer.Services;
using Elite.WebServer.Utility;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Results;

namespace Elite.WebServer.Base
{
    [RequestAuthorize]
    [EnableCors(origins: "*", headers: "*", methods: "*")]

    public class BaseController : ApiController
    {

        protected UserInfo userInfo;

        protected MySqlConnection conn;

        private NameValueCollection requestForm;

        private NameValueCollection requestQuery;

        private JObject jObject;

        protected byte[] tokenHex;

        //重写基类的验证方式，加入我们自定义的Ticket验证
        public BaseController()
        {

            string token = HttpContext.Current.Request.Headers.Get("x-auth-token");

            if (string.IsNullOrEmpty(token))
            {
                token = HttpContext.Current.Request.QueryString.Get("token");
            }

            RedisHelper.SetCon(GetRedisConstr());

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    int tokenExpireMinutes = 20;
                    if (RedisHelper.Exists("token_expire_seconds"))
                    {
                        tokenExpireMinutes = Convert.ToInt32(RedisHelper.Get("token_expire_seconds")) / 60;
                    }


                    this.tokenHex = Helper.StrToHexByte(token);
                    object obj = RedisHelper.Get(token);
                    if (obj == null) return;
                    RedisHelper.Set(token, obj, tokenExpireMinutes);

                    userInfo = JsonConvert.DeserializeObject<UserInfo>(obj.ToString());
                }
                catch
                {
                };
            }
        }

        public void GetRequest(JObject obj)
        {
            HttpContextBase context = (HttpContextBase)Request.Properties["MS_HttpContext"];//获取传统context   
            requestForm = context.Request.Form;
            requestQuery = context.Request.QueryString;
            jObject = obj;
        }

        public void GetRequest()
        {
            HttpContextBase context = (HttpContextBase)Request.Properties["MS_HttpContext"];//获取传统context   
            requestForm = context.Request.Form;
            requestQuery = context.Request.QueryString;
        }

        public int GetId(int defaultVal = 0)
        {
            if (Request.GetRouteData().Values.ContainsKey("id"))
            {
                return Convert.ToInt32(Request.GetRouteData().Values["id"].ToString());
            }
            return defaultVal;
        }

        public int GetSchoolId(int defaultVal = 0)
        {
            if (Request.GetRouteData().Values.ContainsKey("schoolId"))
            {
                return Convert.ToInt32(Request.GetRouteData().Values["schoolId"].ToString());
            }
            return defaultVal;
        }

        public int GetUriParamsInt(string key, int defaultVal = 0)
        {
            if (Request.GetRouteData().Values.ContainsKey(key))
            {
                return Convert.ToInt32(Request.GetRouteData().Values[key].ToString());
            }
            return defaultVal;
        }

        public string GetUriParamsStr(string key, string defaultVal = "")
        {
            if (Request.GetRouteData().Values.ContainsKey(key))
            {
                return Request.GetRouteData().Values[key].ToString();
            }
            return defaultVal;
        }

        public int GetId(string idName, int defaultVal = 0)
        {
            if (Request.GetRouteData().Values.ContainsKey(idName))
            {
                return Convert.ToInt32(Request.GetRouteData().Values[idName].ToString());
            }
            return defaultVal;
        }

        public string GetString(string param, string defaultVal = "")
        {
            if (jObject != null)
            {
                if (jObject.Property(param) != null)
                {
                    return jObject[param].ToString().Trim();
                }
            }
            if (requestForm.AllKeys.Contains(param)) return requestForm[param].ToString().Trim();
            if (requestQuery.AllKeys.Contains(param)) return requestQuery[param].ToString().Trim();
            return defaultVal;
        }

        public int GetInt(string param, int defaultVal = 0)
        {
            if (jObject != null)
            {
                if ((jObject.Property(param) != null) && (DataValidate.IsInteger(jObject[param].ToString())))
                {
                    return Convert.ToInt32(jObject[param]);
                }
            }
            if (requestForm.AllKeys.Contains(param) && DataValidate.IsInteger(requestForm[param].ToString())) return Convert.ToInt32(requestForm[param]);
            if (requestQuery.AllKeys.Contains(param) && DataValidate.IsInteger(requestQuery[param].ToString())) return Convert.ToInt32(requestQuery[param]);
            return defaultVal;
        }


        public decimal GetDecimal(string param, decimal defaultVal = 0)
        {
            if (jObject != null)
            {
                if (jObject.Property(param) != null)
                {
                    return Convert.ToDecimal(jObject[param]);
                }
            }
            if (requestForm.AllKeys.Contains(param)) return Convert.ToDecimal(requestForm[param]);
            if (requestQuery.AllKeys.Contains(param)) return Convert.ToDecimal(requestQuery[param]);
            return defaultVal;
        }

        protected bool DsEmpty(DataSet ds)
        {
            bool flag = false;
            if ((ds == null) || (ds.Tables.Count == 0) || (ds.Tables.Count == 1 && ds.Tables[0].Rows.Count == 0))
            {
                flag = true;
            }
            return flag;
        }

        public String Constr()
        {
            return ConfigurationManager.AppSettings["constr"];
        }

        public string GetRedisConstr()
        {
            return ConfigurationManager.AppSettings["redisConstr"].ToString();
        }

        protected int PageCount(int total, int pageSize)
        {
            return (int)Math.Ceiling(Convert.ToDouble(total) / pageSize);
        }

        protected int TotalCount(MySqlConnection conn)
        {
            return Convert.ToInt32(MySqlHelper.ExecuteScalar(conn, "select FOUND_ROWS()"));
        }

        protected String QueryLimit(int page_size, int page)
        {
            return " limit " + (page_size * (page - 1)).ToString() + "," + page_size.ToString();
        }

        protected String QueryOrder(string sort_column, string sort_direction)
        {
            if (string.IsNullOrEmpty(sort_column)) return string.Empty;
            if (string.IsNullOrEmpty(sort_direction)) return string.Empty;
            if (sort_direction.ToLower().Equals("normal")) return string.Empty;
            if ((sort_column.IndexOf('.') > 0) && ((sort_column.IndexOf('.') + 1) >= sort_column.Length)) return string.Empty;

            return " order by " + sort_column + " " + sort_direction;
        }

        //校验用户名密码（正式环境中应该是数据库校验）
        private bool ValidateTicket(string encryptTicket)
        {
            return true;
        }

        protected HttpResponseMessage Error(string error)
        {

            string jsonStr = "{\"code\": 500, \"message\":\"" + error + "\"}";

            HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            message.Content = new StringContent(jsonStr, Encoding.UTF8, "application/json");

            return message;
        }

        protected JsonMsg CheckResult(byte[] returns)
        {
            if (!Helper.CheckCRC16(returns, Convert.ToUInt16(returns.Length)))
            {
                return new JsonMsg { code = 500, message = "服务器未启动或数据传输错误" };
            }

            if ((returns[0] != 0xff) || (returns[1] != 0xee)) return new JsonMsg { code = 500, message = "获取结果异常" };

            int len = BitConverter.ToUInt16(returns, 2);
            string result = Encoding.UTF8.GetString(returns, 4, len - 6);
            JsonMsg<byte[]> obj = JsonConvert.DeserializeObject(result) as JsonMsg<byte[]>;
            return new JsonMsg { code = obj.code, message = obj.message };
        }


        public static JsonMsg<byte[]> CheckResultWithData(byte[] returns)
        {
            if ((returns.Length == 1) && (returns[0] == 100))
            {
                return new JsonMsg<byte[]> { code = 501, message = "服务器未启动或数据传输错误" };
            }

            if (!Helper.CheckCRC16(returns, Convert.ToUInt16(returns.Length)))
            {
                return new JsonMsg<byte[]> { code = 501, message = "服务器未启动或数据传输错误" };
            }

            if ((returns[0] != 0xff) || (returns[1] != 0xee)) return new JsonMsg<byte[]> { code = 500, message = "获取结果异常" };
            int len = BitConverter.ToUInt16(returns, 2);

            string result = Encoding.UTF8.GetString(returns, 4, len - 6);
            try
            {
                dynamic obj = JsonConvert.DeserializeObject(result);

                if (obj.code != 200) return new JsonMsg<byte[]> { code = 500, message = obj.message };
                string aaa = obj.data.ToString();
                byte[] bbb = Convert.FromBase64String(aaa);
                return new JsonMsg<byte[]> { code = 200, message = "操作成功", data = bbb };
            }
            catch (Exception ex)
            {
                return new JsonMsg<byte[]> { code = 500, message = "数据转换失败:" + ex.Message };
            }
        }

        protected NegotiatedContentResult<JsonMsg> ErrorJson(string error = "操作失败")
        {
            return Content(HttpStatusCode.InternalServerError, ReturnJson.GetJsonMsg(500, error));
        }

        protected NegotiatedContentResult<JsonMsg> ErrorJson(int code, string error = "操作失败")
        {
            return Content((HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), code.ToString()), ReturnJson.GetJsonMsg(code, error));
        }
        protected NegotiatedContentResult<JsonMsg> SuccessJson(string success = "操作成功")
        {
            return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(200, success));
        }

        protected NegotiatedContentResult<JsonMsg<T>> SuccessJson<T>(T data, string success = "操作成功")
        {
            return Content(HttpStatusCode.OK, ReturnJson.GetJsonMsg(200, success, data));
        }

        protected Hashtable GetDict(MySqlConnection conn, string dict, int schoolId = 0)
        {
            return Dictionary.GetDict(conn, dict, schoolId);
        }

        protected System.Collections.Generic.Dictionary<string, object> DataRowToDict(DataRow row)
        {
            System.Collections.Generic.Dictionary<string, object> result = new System.Collections.Generic.Dictionary<string, object>();
            if (row != null)
            {
                foreach (DataColumn dataColumn in row.Table.Columns)
                {
                    result.Add(dataColumn.ColumnName, row[dataColumn]);
                }
            }
            return result;
        }

        /// <summary>
        /// 判断用户是否包含某权限
        /// </summary>
        /// <param name="power"></param>
        /// <returns></returns>
        protected bool HasPower(string power)
        {
            if (this.userInfo is null) return false;
            if (this.userInfo.access.Contains(power)) return true;
            return false;
        }

    }
}
