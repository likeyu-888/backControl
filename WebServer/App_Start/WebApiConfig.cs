using Elite.WebServer.Base;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;

namespace Elite.WebServer
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API 配置和服务

            //允许跨域
            //config.EnableCors();
            config.MessageHandlers.Add(new CorsHandler());
            // Web API 路由
            config.MapHttpAttributeRoutes();


            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            //移除xml返回格式数据
            //GlobalConfiguration.Configuration.Formatters.XmlFormatter.SupportedMediaTypes.Clear();

            var jsonFormatter = new JsonMediaTypeFormatter();
            config.Services.Replace(typeof(IContentNegotiator), new JsonContentNegotiator(jsonFormatter));


            //配置返回的时间类型数据格式  
            GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.Converters.Add(
                new Newtonsoft.Json.Converters.IsoDateTimeConverter
                {
                    DateTimeFormat = "yyyy'-'MM'-'dd' 'HH':'mm':'ss"
                }
            );


            //config.Filters.Add(new WebApiExceptionFilterAttribute());

            config.Services.Replace(typeof(IExceptionHandler), new ErrorHandler());

        }
    }
}
