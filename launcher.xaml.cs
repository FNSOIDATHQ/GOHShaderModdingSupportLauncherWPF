using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Microsoft.Win32;


namespace GOHShaderModdingSupportLauncherWPF
{
    public partial class Launcher : Page
    {
        MainWindow main;

        MainWindow.LauncherVars vars;

        public Launcher()
        {
            InitializeComponent();

            main = Application.Current.MainWindow as MainWindow;

            vars = main.launcherVars;
            
            launchMethod.SelectedIndex = (int)vars.lm;

            addModInfo.IsChecked = vars.showAddModInfo;

        }

        private void ReplaceFile()
        {
            var dstShaderPak = vars.resourceDir + @"\shader.pak.bak";
            var modifiedPak = vars.resourceDir + @"\shader.pak.modified";
            if (File.Exists(dstShaderPak) == false)
            {
                File.Move(vars.resourceDir + @"\shader.pak", dstShaderPak);
                //rename shader folder to avoid load cache
                Directory.Move(vars.resourceDir + @"\shader", vars.resourceDir + @"\shaderBak");

                if (File.Exists(modifiedPak) == true)
                {
                    File.Move(modifiedPak, vars.resourceDir + @"\shader.pak");
                }
                else
                {
                    //extract pak from exe
                    //file is 326kb, so we use 350kb as a batch to extract everything once
                    ExtractFile("GOHShaderModdingSupportLauncherWPF.shader.pak", vars.resourceDir + @"\shader.pak", 358400);


                }

            }
            //if bak file exists, means the patch has been added
            else
            {
                //do nothing
            }
        }

        //reference https://juejin.cn/post/6989143365862293534
        private void ExtractFile(String resource, String path, int batch)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            BufferedStream input = new BufferedStream(assembly.GetManifestResourceStream(resource));
            FileStream output = new FileStream(path, FileMode.Create);

            byte[] data = new byte[batch];
            int lengthEachRead;
            while ((lengthEachRead = input.Read(data, 0, data.Length)) > 0)
            {
                output.Write(data, 0, lengthEachRead);
            }
            output.Flush();
            output.Close();
        }

        //open bump options, a fix for my own shader mod
        private void ForceChangeSettings()
        {
            string opt;
            using (StreamReader sr = File.OpenText(vars.optionLoc))
            {
                opt = sr.ReadToEnd();

                sr.Close();
            }

            int presetLoc = opt.IndexOf("{preset");
            int presetEndLoc = opt.IndexOf("}\r\n\t\t{hdr");
            int bumpLoc = opt.IndexOf("{bumpType");
            int bumpEndLoc = opt.IndexOf("}\r\n\t\t{specular");


            opt = opt.Remove(bumpLoc, bumpEndLoc - bumpLoc + 1);
            opt = opt.Insert(bumpLoc, "{bumpType parallax}");
            opt = opt.Remove(presetLoc, presetEndLoc - presetLoc + 1);
            opt = opt.Insert(presetLoc, "{preset custom}");

#if DEBUG
            Trace.WriteLine(presetEndLoc);
            Trace.Write(opt);
#endif

            using (StreamWriter sw = File.CreateText(vars.optionLoc))
            {

                sw.Write(opt);
                sw.Close();
            }
        }

        private void RestoreFile(bool force = false)
        {
            if (vars.NeedRestore == true || force == true)
            {
                var dstShaderPak = vars.resourceDir + @"\shader.pak.bak";

                //if bak file not exists,we have nothing to do
                if (File.Exists(dstShaderPak) == false)
                {
                    //do nothing
                }
                else
                {
                    //backup modified pak for optimization
                    if (force == false)
                    {
                        File.Move(vars.resourceDir + @"\shader.pak", vars.resourceDir + @"\shader.pak.modified");
                    }


                    //retore
                    File.Move(dstShaderPak, vars.resourceDir + @"\shader.pak", force);
                    Directory.Move(vars.resourceDir + @"\shaderBak", vars.resourceDir + @"\shader");

                }
            }
            else
            {
                //do nothing
            }

        }

        private void ClearCache(bool force = false)
        {
            if (vars.NeedClearCache == true || force == true)
            {
                main.ClearCacheWork();
            }
            else
            {

            }
        }

        private void OnGameExit()
        {
            if (vars.NeedRedisplay == true)
            {
                main.Show();
            }
            else
            {
                main.SaveSettings();
                Environment.Exit(0);
            }
        }

        private void Game_Click(object sender, RoutedEventArgs e)
        {
            if (vars.lm == MainWindow.LauncherVars.LaunchMethod.FileReplace)
            {
                ReplaceFile();
            }
            ForceChangeSettings();

            string args = "";
            if (vars.lm == MainWindow.LauncherVars.LaunchMethod.DX101)
            {
                args = "-dx 10.1";
            }
            if (vars.showAddModInfo == true)
            {
                args += " -showmodinfo";
            }

            Process game = Process.Start(vars.gameDir.GetFiles("call_to_arms.exe")[0].ToString(), args);

            main.Hide();
            game.WaitForExit();

            if (vars.lm == MainWindow.LauncherVars.LaunchMethod.FileReplace)
            {
                RestoreFile();
            }
            ClearCache();

            OnGameExit();

        }

        private void Editor_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AutoFix_Click(object sender, RoutedEventArgs e)
        {

        }

        //this will make a auto fix
        private void Safe_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LaunchMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void AddModInfo_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}