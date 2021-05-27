using Elite.WebServer.Base;
using RestSharp;
using System;
using System.Web.Http;

namespace Elite.WebServer.Controllers
{
    public class TestsController : BaseController
    {
        protected string BaseUri = "http://192.168.0.175:8080";

        [HttpGet]
        [AllowAnonymous]
        [Route("api/tests")]
        public IHttpActionResult Get()
        {
            try
            {
                //发送Get和Post请求
                RestClient client = new RestClient("http://t2173i8728.iask.in");//指定请求的url
                RestRequest request = new RestRequest("api/devices", Method.POST);//指定请求的方式，如果Post则改成Method.POST
                request.AddHeader("x-auth-token", "891ba48f093e668acfe54f8ae6524c83");
                request.AddJsonBody(new
                {
                    name = "设备1234",
                    ip = "192.158.1.1"
                });
                //req.AddBody(body); /*如发送post请求，则用req.AddBody()指定body内容*/
                //发送请求得到请求的内容
                //如果有header可以使用下面方法添加
                //request.AddHeader("header", "value");
                IRestResponse response = client.Execute(request);
                //上传一个文件
                //request.AddFile("file", path);
                var content = response.Content; // 未处理的content是string
                return SuccessJson(new { result = content });
            }
            catch (Exception ex)
            {
                return SuccessJson(new { error = ex.Message });
            }

        }
    }
}