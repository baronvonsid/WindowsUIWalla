using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using log4net;
using log4net.Config;

namespace ManageWalla
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(App));

        [System.STAThreadAttribute()]
        public static void Main()
        {
            SplashScreen splashScreen = new SplashScreen("resources/icons/splashScreen.png");
            splashScreen.Show(true);
            


            ManageWalla.App app = new ManageWalla.App();
            app.StartupUri = new Uri("MainTwo.xaml", System.UriKind.Relative);
            app.InitializeComponent();
            //splashScreen.Close(TimeSpan.FromSeconds(5));
            System.Threading.Thread.Sleep(2000);
            app.Run();

        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0}", "App.App_DispatcherUnhandledException()"); }
            logger.Error(e.Exception);
            e.Handled = true;
        }

    }
}
