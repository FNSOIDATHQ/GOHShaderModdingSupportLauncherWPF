using System;
using System.Runtime.CompilerServices;
using System.Windows.Interop;
using System.Windows.Media;

namespace GOHShaderModdingSupportLauncherWPF
{
    internal static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            AppDiagnostics.Initialize();
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                AppDiagnostics.ReportFatal("Unhandled exception", e.ExceptionObject);
            };

            try
            {
                return RunApplication(args);
            }
            catch (Exception ex)
            {
                AppDiagnostics.ReportFatal("Application startup / run", ex);
                return 1;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int RunApplication(string[] args)
        {
            if (Array.Exists(args, arg => arg.Equals("--software-rendering", StringComparison.OrdinalIgnoreCase)))
            {
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
                AppDiagnostics.Log("Software rendering enabled.");
            }

            AppDiagnostics.Log("Creating Application.");
            var app = new App();
            app.InitializeComponent();
            AppDiagnostics.Log("Application resources loaded.");
            return app.Run();
        }
    }
}
