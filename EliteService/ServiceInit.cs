using EliteService.Audio;
using EliteService.Control;
using EliteService.Utility;
using System;
using System.IO;
using System.Text;

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
            try
            {
                LogHelper.GetInstance.Write("Begin", "系统开始启动");

                RedisHelper.SetCon(Helper.GetRedisConstr());
                //try
                //{
                //    string root = AppDomain.CurrentDomain.BaseDirectory  +"records"+"\\";
                //    if (!Directory.Exists(root)) Directory.CreateDirectory(root);
                //    string fullFileName = root + "1234.txt";
                //    FileStream fileStream = new FileStream(fullFileName, FileMode.Create, FileAccess.Write);
                //    string str = "123";
                //    byte[] tmp = Encoding.Default.GetBytes(str);
                //    fileStream.Write(tmp, 0, tmp.Length);
                //    fileStream.Close();
                //    LogHelper.GetInstance.Write("路径", fullFileName);
                //}
                //catch(Exception ex)
                //{
                //    LogHelper.GetInstance.Write("异常", ex.Message);
                //}

                GlobalData.Initial();

                AutoSave autoSave = new AutoSave();
                autoSave.BeginTask();

                ControlServer controlServer = new ControlServer();
                controlServer.StartServer();

                if (GlobalData.SaveWaveInterval > 0)
                {
                    MonitorServer monitorServer = new MonitorServer();
                    monitorServer.StartServer();
                }

                CloudClient cloudClient = new CloudClient();
                cloudClient.BeginTask();

                MemoryClear memoryClear = new MemoryClear();
                memoryClear.BeginTask();
            }
            catch { }

        }

        public static void End()
        {
            if (GlobalData.mServerSocket.Connected)
            {
                GlobalData.mServerSocket.Close();
            }
            GlobalData.mServerSocket.Dispose();

        }
    }
}
