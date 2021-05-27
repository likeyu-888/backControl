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
}
