using System.Net;

namespace EliteService.DTO
{
    public class School
    {

        public int id { get; set; }
        public int version { get; set; }
        public byte[] password { get; set; }
        public EndPoint endPoint { get; set; }

        /// <summary>
        /// 删除标志，使用时先重置为1，数据库存在时置为0，最后仍为1的表示不存在，需删除
        /// </summary>
        public int deleteTag { get; set; }



        public School()
        {
            this.version = 0;
            this.password = new byte[] { };
            this.deleteTag = 1;
            this.endPoint = new IPEndPoint(IPAddress.Any, 0);
        }
    }
}
