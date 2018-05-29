using System;
using System.Windows;

namespace PianoRoll
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length >= 1)
            {
                new MainWindow(e.Args[0]).Show();
                return;
            }

            var clickOnceArgs = AppDomain.CurrentDomain.SetupInformation.ActivationArguments;
            if (clickOnceArgs?.ActivationData != null && clickOnceArgs.ActivationData.Length >= 1)
            {
                new MainWindow(clickOnceArgs.ActivationData[0]).Show();
                return;
            }

            new MainWindow().Show();
        }
    }
}
