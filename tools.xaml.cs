using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;


namespace GOHShaderModdingSupportLauncherWPF
{
    public partial class Tools : Page
    {
        MainWindow main;

        MainWindow.ToolsVars vars;
        public Tools()
        {
            main = Application.Current.MainWindow as MainWindow;

            vars = main.toolsVars;

            InitializeComponent();

            
        }
        private void clearShaderCache_Click(object sender, RoutedEventArgs e)
        {
            main.ClearCacheWork();
            MessageBox.Show("Shader cache cleanup complete!", "Notice");
        }

        private void restore_Click(object sender, RoutedEventArgs e)
        {
            main.ExtractFile("GOHShaderModdingSupportLauncherWPF.pak.Ori.shader.pak", main.universalVars.resourceDir + @"\shader.pak", 358400);
        }

        private void openConfig_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", main.universalVars.configLoc);
        }

        private void precompile_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}