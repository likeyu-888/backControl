using System;
using System.ServiceProcess;

namespace EliteService
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        static void Main()
        {
            //ServiceInit.Begin();
            //Console.ReadLine();
            //return;
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new mEliteService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
