using System.ServiceProcess;

namespace EliteService
{
    public partial class mEliteCloudService : ServiceBase
    {
        public mEliteCloudService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            ServiceInit.Begin();
        }

        protected override void OnStop()
        {
        }
    }
}
