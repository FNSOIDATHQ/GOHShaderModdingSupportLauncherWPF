using System.Threading.Tasks;
using System.Windows;

namespace GOHShaderModdingSupportLauncherWPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");
            AppDiagnostics.Log("Starting main window.");
            base.OnStartup(e);
        }
        public App()
        {
            DispatcherUnhandledException += (_, e) =>
            {
                e.Handled = true;
                AppDiagnostics.ReportFatal("UI dispatcher", e.Exception);
                Shutdown(1);
            };

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                // This event runs on the finalizer thread
                // do not call Application.Shutdown here
                AppDiagnostics.Log("Unobserved background task exception", e.Exception);
                e.SetObserved();
            };

            Exit += (_, e) =>
            {
                AppDiagnostics.Log($"Application exit: {e.ApplicationExitCode}");
            };
        }
    }
}
