using System;
using System.Globalization;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GOHShaderModdingSupportLauncherWPF.Properties;

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

        public App()
        {
            //catch all unhandled exception
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ShowErrorReport("", e.Exception);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            ShowErrorReport("", ex);
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            ShowErrorReport("", e.Exception);
            e.SetObserved();
        }

        private Exception GetRootException(Exception ex)
        {
            while (ex.InnerException != null)
                ex = ex.InnerException;
            return ex;
        }

        //strinig type will not be use for now
        private void ShowErrorReport(string type, Exception ex)
        {
            var root = GetRootException(ex);

            var sb = new StringBuilder();
            //sb.AppendLine($"{i18n.APP_Type}{type}");
            sb.AppendLine($"{i18n.APP_Time}{DateTime.Now}");
            sb.AppendLine($"{i18n.APP_ExceptionType}{ex?.GetType().FullName}");
            sb.AppendLine($"{i18n.APP_ExceptionMessage}{ex?.Message}\n");
            sb.AppendLine($"{i18n.APP_RootType}{root?.GetType().FullName}");
            sb.AppendLine($"{i18n.APP_RootMessage}{root?.Message}");
            sb.AppendLine($"{i18n.APP_Stack}\n{root?.StackTrace}");

            // 显示给用户
            MessageBox.Show(sb.ToString(), i18n.Universal_Error, MessageBoxButton.OK, MessageBoxImage.Error);

            Current.Shutdown();
        }
    }



}
