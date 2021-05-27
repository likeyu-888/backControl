using System.Threading;

namespace EliteService.Control
{
    public class AutoUpdate
    {

        private readonly int heartBeatInterval = 5; //心跳检测时间间隔。

        public void BeginTask()
        {

            Thread thread = new Thread(new ThreadStart(() =>
            {
                DealAutoUpdate();
            }))
            {
                IsBackground = true
            };

            thread.Start();
        }

        /// <summary>
        /// 自动更新系统设置
        /// </summary>
        private void DealAutoUpdate()
        {
            while (true)
            {
                GlobalData.Initial();
                Thread.Sleep(this.heartBeatInterval * 1000);
            }
        }

    }


}
