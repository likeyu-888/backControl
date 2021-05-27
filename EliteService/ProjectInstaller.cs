using System;
using System.Collections;
using System.ComponentModel;

namespace EliteService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
        //protected override void OnAfterInstall(IDictionary savedState)
        //{
        //    try
        //    {
        //        base.OnAfterInstall(savedState);
        //        System.Management.ManagementObject myService = new System.Management.ManagementObject(
        //            string.Format("Win32_Service.Name='{0}'", this.serviceInstaller1.ServiceName));
        //        System.Management.ManagementBaseObject changeMethod = myService.GetMethodParameters("Change");
        //        changeMethod["DesktopInteract"] = true;
        //        System.Management.ManagementBaseObject OutParam = myService.InvokeMethod("Change", changeMethod, null);
        //    }
        //    catch (Exception ex)
        //    {
        //    }
        //}
    }
}
