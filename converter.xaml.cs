using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace GOHShaderModdingSupportLauncherWPF
{
    public partial class Converter : Page
    {
        private string targetPath;

        private Wpf.Ui.Controls.TextBox TB_ConvertPath;
        public Converter()
        {
            InitializeComponent();

            TB_ConvertPath = this.FindName("convertPath") as Wpf.Ui.Controls.TextBox;

        }

        private void convertPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            targetPath = TB_ConvertPath.Text;
        }

        private void viewPath_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            bool? result=dialog.ShowDialog();

            if (result == true)
            {
                targetPath = dialog.FolderName;
                TB_ConvertPath.Text = targetPath;
            }
        }

        private void enableEnvMaps_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}