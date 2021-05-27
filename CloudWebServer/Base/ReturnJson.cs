using System.Collections;

namespace Elite.WebServer.Base
{
    public class ReturnJson
    {
        public static JsonMsg GetJsonMsg(int code, string message)
        {
            JsonMsg jsonMsg = new JsonMsg();
            jsonMsg.code = code;
            jsonMsg.message = message;


            return jsonMsg;
        }
        public static JsonMsg<T> GetJsonMsg<T>(int code, string message, T data)
        {
            JsonMsg<T> jsonMsg = new JsonMsg<T>();
            jsonMsg.code = code;
            jsonMsg.message = message;
            jsonMsg.data = data;

            return jsonMsg;
        }

        public static JsonMsg<T> GetJsonMsg<T>(T data)
        {
            return GetJsonMsg(200, "操作成功", data);
        }

        public static JsonMsg<PagedData<T>> GetJsonMsg<T>(int total, int page, int page_size, int page_count, T data, Hashtable hashTable = null)
        {

            PagedData<T> pagedData = new PagedData<T>();
            pagedData.total = total;
            pagedData.page = page;
            pagedData.page_size = page_size;
            pagedData.page_count = page_count;
            pagedData.list = data;
            if (hashTable != null)
            {
                pagedData.dict = hashTable;
            }

            JsonMsg<PagedData<T>> jsonMsg = new JsonMsg<PagedData<T>>();
            jsonMsg.code = 200;
            jsonMsg.message = "操作成功";
            jsonMsg.data = pagedData;

            return jsonMsg;
        }
    }

    /// <summary>
    /// 定义返回json格式数据
    /// </summary>
    public class JsonMsg
    {
        public int code { get; set; }
        public string message { get; set; }
    }

    /// <summary>
    /// 定义返回json格式数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JsonMsg<T>
    {
        public int code { get; set; }
        public string message { get; set; }
        public T data { get; set; }
    }

    public class PagedData<T>
    {
        public int total { get; set; }
        public int page { get; set; }
        public int page_size { get; set; }
        public int page_count { get; set; }
        public T list { get; set; }
        public Hashtable dict { get; set; }
    }
}