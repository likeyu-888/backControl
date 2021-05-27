namespace Elite.WebServer.Base
{
    public class UserInfo
    {
        public int id { get; set; }
        public string username { get; set; }
        public string company { get; set; }
        public string contact_name { get; set; }
        public string mobile { get; set; }
        public string wechat { get; set; }
        public string email { get; set; }
        public string last_login_ip { get; set; }
        public int status { get; set; }
        public int role_id { get; set; }
        public int school_id { get; set; }
        public string[] access { get; set; }
        public string schools { get; set; }

    }
}
