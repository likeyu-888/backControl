using System.ServiceProcess;

namespace EliteService
{
    public partial class mEliteService : ServiceBase
    {
        public mEliteService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            ServiceInit.Begin();
        }

        protected override void OnStop()
        {
            ServiceInit.End();
        }
    }
}
