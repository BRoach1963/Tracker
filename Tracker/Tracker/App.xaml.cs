using System.Windows;
using Tracker.Logging;

namespace Tracker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    { 
        public App()
        {

        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            LoggingManager.Instance.Shutdown();
        }
    }

}
