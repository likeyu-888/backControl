using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

public class WebApiExceptionFilterAttribute : ExceptionFilterAttribute
{
    //重写基类的异常处理方法
    public override void OnException(HttpActionExecutedContext actionExecutedContext)
    {

        if (actionExecutedContext.Exception is NotImplementedException)
        {
            actionExecutedContext.Response = new HttpResponseMessage(HttpStatusCode.NotImplemented);
        }
        else if (actionExecutedContext.Exception is TimeoutException)
        {
            actionExecutedContext.Response = new HttpResponseMessage(HttpStatusCode.RequestTimeout);
        }
        else
        {
            var oResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            oResponse.Content = actionExecutedContext.Response.Content;
            oResponse.ReasonPhrase = actionExecutedContext.Response.ReasonPhrase;
            actionExecutedContext.Response = oResponse;
        }

        base.OnException(actionExecutedContext);
    }
}