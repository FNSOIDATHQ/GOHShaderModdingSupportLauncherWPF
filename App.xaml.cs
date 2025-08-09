using System.Threading;
using System;
using System.Windows;
using System.Globalization;

namespace GOHShaderModdingSupportLauncherWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            //Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            base.OnStartup(e);
        }
    }



}
