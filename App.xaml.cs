using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

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
            ShowErrorReport("UI线程异常", e.Exception);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            ShowErrorReport("非UI线程异常", ex);
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            ShowErrorReport("Task异常", e.Exception);
            e.SetObserved();
        }

        private Exception GetRootException(Exception ex)
        {
            while (ex.InnerException != null)
                ex = ex.InnerException;
            return ex;
        }

        private void ShowErrorReport(string type, Exception ex)
        {
            var root = GetRootException(ex);

            var sb = new StringBuilder();
            sb.AppendLine($"错误类型：{type}");
            sb.AppendLine($"时间：{DateTime.Now}");
            sb.AppendLine($"捕获异常类型：{ex?.GetType().FullName}");
            sb.AppendLine($"捕获异常消息：{ex?.Message}");
            sb.AppendLine($"根异常类型：{root?.GetType().FullName}");
            sb.AppendLine($"根异常消息：{root?.Message}");
            sb.AppendLine($"堆栈：{root?.StackTrace}");

            // 显示给用户
            MessageBox.Show(sb.ToString(), "发生错误", MessageBoxButton.OK, MessageBoxImage.Error);

            Current.Shutdown();
        }
    }



}
