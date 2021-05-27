using Elite.WebServer.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
/// <summary>
/// 自定义此特性用于接口的身份验证
/// </summary>
/// 
namespace Elite.WebServer.Base
{
    public class RequestAuthorizeAttribute : AuthorizeAttribute
    {
        private int status = 403;

        //重写基类的验证方式，加入我们自定义的Ticket验证
        public override void OnAuthorization(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            status = 403;

            var attributes = actionContext.ActionDescriptor.GetCustomAttributes<AllowAnonymousAttribute>().OfType<AllowAnonymousAttribute>();
            bool isAnonymous = attributes.Any(a => a is AllowAnonymousAttribute);
            if (isAnonymous)
            { //匿名访问
                base.OnAuthorization(actionContext);
                return;
            }

            IEnumerable<string> tokens;
            actionContext.Request.Headers.TryGetValues("x-auth-token", out tokens);
            string token = "";

            //未带token,禁止访问
            if ((tokens != null) && (tokens.Count() > 0))
            {
                token = tokens.FirstOrDefault();
            }
            else
            {
                token = ((HttpContextBase)actionContext.Request.Properties["MS_HttpContext"]).Request.QueryString.Get("token");
                if (string.IsNullOrEmpty(token))
                {
                    this.status = 401;
                    HandleUnauthorizedRequest(actionContext);
                    return;
                }
            }

            string actionPower = "";
            var powerAttributes = actionContext.ActionDescriptor.GetCustomAttributes<PowerAttribute>().OfType<PowerAttribute>();
            var authAttribute = powerAttributes.FirstOrDefault(ee => ee is PowerAttribute);

            if (authAttribute != null)
            {
                var power = authAttribute as PowerAttribute;
                actionPower += power.RoleString;
            }

            if (!RedisHelper.Exists(token))
            {
                this.status = 401;
                HandleUnauthorizedRequest(actionContext);
            }
            else
            {


                if (ValidateTicket(token, actionPower)) //权限通过
                {
                    base.IsAuthorized(actionContext);
                }
                else //权限不通过
                {
                    HandleUnauthorizedRequest(actionContext);
                }
            }
        }


        protected void HandleUnauthorizedRequest(System.Web.Http.Controllers.HttpActionContext context, string error)
        {
            base.HandleUnauthorizedRequest(context);
            var response = context.Response ?? new HttpResponseMessage();
            response.StatusCode = (status == 401 ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden);


            string jsonStr = "{\"code\": " + status.ToString() + "," +
                "\"message\":\"" + error + "\"" +
                "}";

            response.Content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
        }
        protected override void HandleUnauthorizedRequest(System.Web.Http.Controllers.HttpActionContext context)
        {
            HandleUnauthorizedRequest(context, "非法请求，未授权访问");
        }


        private bool UserNeedLoginAgain(UserInfo userInfo)
        {
            string key = Helper.md5("user_login_again_" + userInfo.id.ToString());
            if (RedisHelper.Exists(key))
            {
                RedisHelper.Remove(key);
                return true;
            }
            return false;
        }

        private bool ValidateTicket(string token, string actionPower)
        {
            if (string.IsNullOrEmpty(actionPower)) return true;

            string[] needPowerArr = actionPower.Split(','); //多个为或的关系

            if (!RedisHelper.Exists(token))
            {
                this.status = 401;
                return false;
            }


            string userInfoStr = RedisHelper.Get(token).ToString();
            if (string.IsNullOrEmpty(userInfoStr))
            {
                this.status = 401;
                return false;
            }
            UserInfo userInfo = JsonConvert.DeserializeObject<UserInfo>(userInfoStr);
            if (userInfo == null)
            {
                this.status = 401;
                return false;
            }

            if (UserNeedLoginAgain(userInfo))
            {
                this.status = 401;
                return false;
            }

            string[] access = userInfo.access;

            foreach (string power in needPowerArr)
            {
                if (access.Contains(power)) return true;
            }

            return false;
        }
    }
}