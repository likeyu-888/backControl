namespace Elite.WebServer.Base
{
    public class PagedJson
    {
        public static PagedJsonMsg<T> GetPagedJson<T>(int code, string msg, T data = default(T))
        {
            PagedJsonMsg<T> _jsonMsg = new PagedJsonMsg<T>();
            _jsonMsg.code = code;
            _jsonMsg.msg = msg;
            if (data != null)
            {
                _jsonMsg.data = data;
            }

            return _jsonMsg;
        }
    }

    /// <summary>
    /// 定义统计返回json格式数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PagedJsonMsg<T>
    {
        public int code { get; set; }
        public string msg { get; set; }
        public T data { get; set; }
    }
}