using System.Runtime.InteropServices;
using System.Windows;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.ViewModels;
using Tracker.ViewModels.DialogViewModels;
using Tracker.Views.Dialogs;

namespace Tracker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appId);

        public static void SetAppUserModelId(string appId)
        {
            SetCurrentProcessExplicitAppUserModelID(appId);
        }



        public App()
        {
            //TODO: Currently 7-day license key - replace with Community license key when available
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NCaF1cWWhAYVJ2WmFZfVpgcl9GYlZVQmYuP1ZhSXxXdkxjWn9YcHZRQGFYWEM=");
            RegisterAppForToastNotifications();
        }
 

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            LoggingManager.Instance.Shutdown();
            TrackerDataManager.Instance.Shutdown();
        }

        private void RegisterAppForToastNotifications()
        {
            SetAppUserModelId("tracker.diveccosoftware.trackerapp");
        }
 
        private void OnAppStartup(object sender, StartupEventArgs e)
        {
            var loginVm = new LoginDialogViewModel(null)
            {
                UserName = Environment.UserName
            };

            var loginWindow = new LoginDialog()
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            var callback = new Action(() =>
            {
   
                if (loginVm.DialogResult.Cancelled)
                {
                    loginVm.Dispose();
                    loginWindow.Close();
                    return;
                }



                TrackerDbManager.Instance.Initialize();

                TrackerDbManager.Instance.CheckUserAsync();

                TrackerDataManager.Instance.Initialize();

                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    DialogManager.Instance.LaunchDialogByType(DialogType.MainWindow, false, () => { });
                    loginVm.Dispose();
                    loginWindow.Close();
                });
            });

            loginVm.Callback = callback;

            loginWindow.DataContext = loginVm;

            loginWindow.Show();


        }
    }

}
