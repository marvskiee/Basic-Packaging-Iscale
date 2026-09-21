using ISCALE.MVVM.View;
using System.Windows;
using Microsoft.Data.Sqlite;
namespace ISCALE
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            SQLitePCL.Batteries_V2.Init();
            LoginView loginView = new LoginView();
            loginView.ShowDialog();
        }
    }

}
