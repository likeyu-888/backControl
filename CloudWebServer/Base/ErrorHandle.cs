using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Results;

public class ErrorHandler : ExceptionHandler
{
    public override void Handle(ExceptionHandlerContext context)
    {
        string jsonStr = "{\"code\": 500, \"message\":\"" + context.Exception.Message + "\"}";


        HttpResponseMessage message = new HttpResponseMessage();
        message.Content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
        message.StatusCode = HttpStatusCode.InternalServerError;

        context.Result = new ResponseMessageResult(
           message
        );
    }
}