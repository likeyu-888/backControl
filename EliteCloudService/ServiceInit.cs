using EliteService.Control;
using EliteService.Utility;

namespace EliteService
{
    public class ServiceInit
    {
        //1查询数据库得到设备列表
        //2定时器，每个间隔去查询所有设备的状态,并存入数据库以及缓存,同时传到云平台,连续三次查询失败，则视为掉线。
        //3运行一个线程等候客户端的连接
        //

        public static void Begin()
        {

            RedisHelper.SetCon(Helper.GetRedisConstr());

            GlobalData.Initial();

            AutoUpdate autoUpdate = new AutoUpdate();
            autoUpdate.BeginTask();

            CloudServer cloudServer = new CloudServer();
            cloudServer.StartServer();

            //RtpMonitor rtpMonitor = new RtpMonitor();      2020-10-12   lky
            //rtpMonitor.StartServer();
        }
    }
}
