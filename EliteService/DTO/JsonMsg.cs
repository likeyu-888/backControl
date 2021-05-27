using System;
using System.Collections;

namespace EliteService.DTO
{
    /// <summary>
    /// 定义返回json格式数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JsonMsg
    {
        public int code { get; set; }
        public string message { get; set; }
        public byte[] data { get; set; }
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

    [Serializable]
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
