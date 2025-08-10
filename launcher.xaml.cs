using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;


namespace GOHShaderModdingSupportLauncherWPF
{
    public partial class Launcher : Page
    {
        MainWindow main;

        MainWindow.LauncherVars vars;

        public Launcher()
        {
            main = Application.Current.MainWindow as MainWindow;

            vars = main.launcherVars;

            InitializeComponent();

            
            
            launchMethod.SelectedIndex = (int)vars.lm;

            addModInfo.IsChecked = vars.showAddModInfo;

        }

        private void ReplaceFile()
        {
            FileInfo targetPak = new FileInfo(main.universalVars.resourceDir + @"\shader.pak");
            if (targetPak.Length>7*1048576)
            {
                    //extract pak from exe
                    //file is 326kb, so we use 350kb as a batch to extract everything once
                    main.ExtractFile("GOHShaderModdingSupportLauncherWPF.pak.noCache.shader.pak", main.universalVars.resourceDir + @"\shader.pak", 358400);

            }
            //if shader.pak <7mb, means the patch has been added
            else
            {
                //do nothing
            }
        }

        //open bump options, a fix for my own shader mod
        private void ForceChangeSettings()
        {
            string opt;
            using (StreamReader sr = File.OpenText(main.universalVars.optionLoc))
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

            using (StreamWriter sw = File.CreateText(main.universalVars.optionLoc))
            {

                sw.Write(opt);
                sw.Close();
            }
        }

        private void RestoreFile(bool force = false)
        {
            if (main.universalVars.NeedRestore == true || force == true)
            {
                //retore
                main.ExtractFile("GOHShaderModdingSupportLauncherWPF.pak.Ori.shader.pak", main.universalVars.resourceDir + @"\shader.pak", 358400);
            }
            else
            {
                //do nothing
            }

        }

        private void ClearCache(bool force = false)
        {
            if (main.universalVars.NeedClearCache == true || force == true)
            {
                main.ClearCacheWork();
            }
            else
            {

            }
        }

        private void CheckCompileWarning()
        {
            using (StreamReader log = File.OpenText(main.universalVars.profileLoc + @"\log\game.log"))
            {
                while (log.EndOfStream != true)
                {
                    string line=log.ReadLine();

                    if(line.IndexOf("compile error:") != -1)
                    {
                        string errorMessage = "Get at least one shader compile error in last gaming! \nTry contact creator(s) of your shader mod to slove this problem.\n\n";
                        errorMessage += "First error message:\n\n";
                        errorMessage += line+'\n';
                        errorMessage+= log.ReadLine();
                        MessageBox.Show(errorMessage, "Shader Compile Error");
                        break;
                    }
                }

                log.Close();
            }
        }

        private void OnGameExit()
        {
            if (main.universalVars.NeedCompileWarning == true)
            {
                CheckCompileWarning();
            }
            if (main.universalVars.NeedRedisplay == true)
            {
                main.Show();
            }
            else
            {
                main.SaveSettings();
                Environment.Exit(0);
            }
        }

        private void RunGame(string processName)
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

            Process game = Process.Start(main.universalVars.gameDir.GetFiles(processName)[0].ToString(), args);

            main.Hide();
            game.WaitForExit();

            if (vars.lm == MainWindow.LauncherVars.LaunchMethod.FileReplace)
            {
                RestoreFile();
            }
            ClearCache();

            OnGameExit();
        }

        private void Game_Click(object sender, RoutedEventArgs e)
        {
            RunGame("call_to_arms.exe");
        }

        private void Editor_Click(object sender, RoutedEventArgs e)
        {
            RunGame("call_to_arms_ed.exe");
        }

        private void AutoFix_Click(object sender, RoutedEventArgs e)
        {
            //force fix
            ClearCache(true);
            RestoreFile(true);

            RunGame("call_to_arms.exe");
        }

        //this will make a auto fix
        private void Safe_Click(object sender, RoutedEventArgs e)
        {
            //force fix
            ClearCache(true);
            RestoreFile(true);

            //start game with no mods
            Process game = Process.Start(main.universalVars.gameDir.GetFiles("call_to_arms.exe")[0].ToString(), "-no_mods");

            main.Hide();
            game.WaitForExit();

            OnGameExit();
        }

        private void LaunchMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vars.lm = (MainWindow.LauncherVars.LaunchMethod)launchMethod.SelectedIndex;
        }

        private void AddModInfo_Click(object sender, RoutedEventArgs e)
        {
            vars.showAddModInfo = addModInfo.IsChecked.Value;
        }
    }
}