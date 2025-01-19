using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;


namespace GOHShaderModdingSupportLauncherWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool HasGetGameRoot;
        private DirectoryInfo gameDir, resourceDir;

        private bool NeedRestore, NeedClearCache,NeedRedisplay,AlwaysConfirm;
        private string configLoc;
        private string cacheLoc;

        private CheckBox C_restore, C_clearCache, C_gameExit,C_pathConfirm;
        public MainWindow()
        {
            InitializeComponent();


            gameDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            resourceDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            HasGetGameRoot = false;

            C_restore = this.FindName("restore") as CheckBox;
            C_clearCache = this.FindName("clearCache") as CheckBox;
            C_gameExit = this.FindName("gameExit") as CheckBox;
            C_pathConfirm = this.FindName("pathConfirm") as CheckBox;

            configLoc = Path.GetTempPath() + @"GOHSMSLauncher\settings.conf";
            if (File.Exists(configLoc) == true)
            {
                int index = 0;
                foreach (string line in File.ReadAllLines(configLoc))
                {
                    if (index == 0)
                    {
                        NeedRestore = bool.Parse(line);
                        
                        C_restore.IsChecked = NeedRestore;
                    }
                    else if (index == 1)
                    {
                        NeedClearCache = bool.Parse(line);
                        
                        C_clearCache.IsChecked = NeedClearCache;
                    }
                    else if (index == 2)
                    {
                        NeedRedisplay = bool.Parse(line);
                        
                        C_gameExit.IsChecked = NeedRedisplay;
                    }
                    else if (index == 3)
                    {
                        AlwaysConfirm = bool.Parse(line);
                        
                        C_pathConfirm.IsChecked = AlwaysConfirm;
                    }
                    else if (index == 4)
                    {
                        //get cached game location
                        //if AlwaysConfirm=true this cache won't be use in later functions
                        if (Directory.Exists(line) == true&&AlwaysConfirm!=true)
                        {
                            gameDir = new DirectoryInfo(line);
                            HasGetGameRoot = true;

                            //avoid catch by steam
                            Environment.CurrentDirectory = gameDir.FullName;
                        }
                    }
                    else if (index == 5)
                    {
                        //get cached resource location
                        //if AlwaysConfirm=true this cache won't be use in later functions
                        if (Directory.Exists(line) == true && AlwaysConfirm != true)
                        {
                            resourceDir = new DirectoryInfo(line);
                        }
                    }
                    else
                    {
                        break;
                    }
                    index++;

#if DEBUG
                    Trace.WriteLine(line);
                    Trace.WriteLine(index);
#endif
                }

                //the config file is not complete or break,fall back to default value
                //no cache is not important
                if (index!=4&&index != 6)
                {
                    NeedRestore = true;
                    C_restore.IsChecked = NeedRestore;
                    NeedClearCache = true;
                    C_clearCache.IsChecked = NeedClearCache;
                    NeedRedisplay = false;
                    C_gameExit.IsChecked = NeedRedisplay;
                    AlwaysConfirm = false;
                    C_pathConfirm.IsChecked = AlwaysConfirm;
                    HasGetGameRoot = false;
                }
            }
            else
            {
                NeedRestore = true;
                NeedClearCache = true;
                NeedRedisplay = false;
                AlwaysConfirm = false;
            }

            cacheLoc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\my games\\gates of hell\\shader_cache";
        }

        //reference https://juejin.cn/post/6989143365862293534
        private void ExtractFile(String resource, String path,int batch)
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

        //we assume the program is running in somewhere in SteamLibrary\steamapps
        //if not we will get game from registry
        private void GetGameRoot()
        {

            string appLoc = Directory.GetCurrentDirectory();
            int index = appLoc.IndexOf("steamapps");

            if (index == -1)
            {
                //try get game folder from reg
                var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
                string steamLoc;
                if (reg != null)
                {
                    steamLoc = reg.GetValue(@"InstallPath").ToString()+ @"\steamapps\libraryfolders.vdf";
                }
                else
                {
                    MessageBox.Show("Can not find steam app on your computer!", "WARNING");
                    throw new InvalidOperationException("Can not find steam app on your computer!");
                }

                //load libraryfolders.vdf to get all steam library location
                //I am to lazy to further search games from .acf files, so just try get every game in common folder
                foreach(string line in File.ReadAllLines(steamLoc))
                {
                    //search folder when get path
                    if (line.Contains("path") == true)
                    {
                        string gameLib = line.Split('"')[3];
                        string gameLoc = gameLib+@"\steamapps\common\Call to Arms - Gates of Hell\binaries\x64";
                        if (Directory.Exists(gameLoc) == true)
                        {
                            gameDir = new DirectoryInfo(gameLoc);
                            resourceDir= new DirectoryInfo(gameLib += @"\steamapps\common\Call to Arms - Gates of Hell\resource");
                            HasGetGameRoot = true;
                            break;
                        }
                        
                    }
                }

                if (HasGetGameRoot == false)
                {
                    MessageBox.Show("Can not find GOH game on your computer!","WARNING");
                    throw new InvalidOperationException("Can not find GOH game on your computer!");
                }
            }
            else
            {
                index += 9;
                string gameLoc = appLoc.Substring(0, index);
                gameDir = new DirectoryInfo(gameLoc);
                resourceDir = gameDir.GetDirectories("common/Call to Arms - Gates of Hell/resource")[0];
                gameDir = gameDir.GetDirectories("common/Call to Arms - Gates of Hell/binaries/x64")[0];
                
                HasGetGameRoot = true;
            }


            //avoid catch by steam
            Environment.CurrentDirectory = gameDir.FullName;

            //manual check
            //reference https://www.c-sharpcorner.com/UploadFile/mahesh/understanding-message-box-in-windows-forms-using-C-Sharp/
            string message = "A GOH game has been found in \n"+ gameDir+"\n\nIf this is NOT the correct location, Please click NO and report detailed information to developers.";
            string title = "Manual Check";
            MessageBoxButton buttons = MessageBoxButton.YesNo;
            MessageBoxResult result = MessageBox.Show(message, title, buttons);
            if (result == MessageBoxResult.No)
            {
                SaveSettings();
                Environment.Exit(0);
            }
            else
            {
                // do nothing
            }

#if DEBUG
            Trace.WriteLine(gameDir);
            MessageBox.Show(gameDir.FullName);
#endif
        }

        private void ReplaceFile()
        {
            var dstShaderPak = resourceDir + @"\shader.pak.bak";
            var modifiedPak= resourceDir + @"\shader.pak.modified";
            if (File.Exists(dstShaderPak) == false)
            {
                File.Move(resourceDir + @"\shader.pak", dstShaderPak);
                //rename shader folder to avoid load cache
                Directory.Move(resourceDir + @"\shader", resourceDir + @"\shaderBak");

                if (File.Exists(modifiedPak) == true)
                {
                    File.Move(modifiedPak, resourceDir + @"\shader.pak");
                }
                else
                {
                    //extract pak from exe
                    //file is 326kb, so we use 350kb as a batch to extract everything once
                    ExtractFile("GOHShaderModdingSupportLauncherWPF.shader.pak", resourceDir + @"\shader.pak", 358400);


                }

            }
            //if bak file exists, means the patch has been added
            else
            {
                //do nothing
            }
        }

        private void RestoreFile(bool force=false)
        {
            if (NeedRestore == true||force==true)
            {
                var dstShaderPak = resourceDir + @"\shader.pak.bak";

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
                        File.Move(resourceDir + @"\shader.pak", resourceDir + @"\shader.pak.modified");
                    }
                    

                    //retore
                    File.Move(dstShaderPak, resourceDir + @"\shader.pak", force);
                    Directory.Move(resourceDir + @"\shaderBak", resourceDir + @"\shader");

                }
            }
            else
            {
                //do nothing
            }

        }

        private void ClearCache(bool force=false)
        {
            if (NeedClearCache == true || force == true)
            {
                if (Directory.Exists(cacheLoc) == true)
                {
                    Directory.Delete(cacheLoc, true);
                }
            }
            else
            {

            }
        }

        private void SaveSettings()
        {
            Directory.CreateDirectory(Path.GetTempPath() + @"GOHSMSLauncher");

            using (StreamWriter sw = File.CreateText(configLoc))
            {

                sw.WriteLine(NeedRestore);
                sw.WriteLine(NeedClearCache);
                sw.WriteLine(NeedRedisplay);
                sw.WriteLine(AlwaysConfirm);
                if (HasGetGameRoot == true)
                {
                    sw.WriteLine(gameDir.FullName);
                    sw.WriteLine(resourceDir.FullName);
                }
                
            }
        }

        private void OnGameExit()
        {
            if (NeedRedisplay == true)
            {
                this.Show();
            }
            else
            {
                SaveSettings();
                Environment.Exit(0);
            }
        }

        private void GameFR_Click(object sender, RoutedEventArgs e)
        {
            if (HasGetGameRoot == false)
            {
                GetGameRoot();
            }

            ReplaceFile();

            Process game = Process.Start(gameDir.GetFiles("call_to_arms.exe")[0].ToString());

            this.Hide();
            game.WaitForExit();

            RestoreFile();
            ClearCache();

            OnGameExit();
            
        }

        private void EditorFR_Click(object sender, RoutedEventArgs e)
        {
            if (HasGetGameRoot == false)
            {
                GetGameRoot();
            }

            ReplaceFile();

            Process editor = Process.Start(gameDir.GetFiles("call_to_arms_ed.exe")[0].ToString());

            this.Hide();
            editor.WaitForExit();

            RestoreFile();
            ClearCache();

            OnGameExit();
        }

        private void GameDX101_Click(object sender, RoutedEventArgs e)
        {
            if (HasGetGameRoot == false)
            {
                GetGameRoot();
            }

            Process game = Process.Start(gameDir.GetFiles("call_to_arms.exe")[0].ToString(), "-dx 10.1 -showmodinfo");

            this.Hide();
            game.WaitForExit();

            ClearCache();

            OnGameExit();
        }

        private void EditorDX101_Click(object sender, RoutedEventArgs e)
        {
            if (HasGetGameRoot == false)
            {
                GetGameRoot();
            }

            Process editor = Process.Start(gameDir.GetFiles("call_to_arms_ed.exe")[0].ToString(), "-dx 10.1 -showmodinfo");

            this.Hide();
            editor.WaitForExit();

            ClearCache();

            OnGameExit();
        }

        //this will make a auto fix
        private void Safe_Click(object sender, RoutedEventArgs e)
        {
            if (HasGetGameRoot == false)
            {
                GetGameRoot();
            }

            //force fix
            ClearCache(true);
            RestoreFile(true);

            //start game with no mods
            Process game = Process.Start(gameDir.GetFiles("call_to_arms.exe")[0].ToString(), "-no_mods");

            this.Hide();
            game.WaitForExit();

            OnGameExit();
        }

        private void OnGameExitStateChanged(object sender, RoutedEventArgs e)
        {
            CheckBox c = (CheckBox)sender;
            NeedRedisplay = c.IsChecked.Value;
        }

        private void OnPathConfirmStateChanged(object sender, RoutedEventArgs e)
        {
            CheckBox c = (CheckBox)sender;
            AlwaysConfirm = c.IsChecked.Value;
        }

        private void OnClearCacheStateChanged(object sender, RoutedEventArgs e)
        {
            CheckBox c = (CheckBox)sender;
            NeedClearCache = c.IsChecked.Value;
#if DEBUG
            Trace.WriteLine(c.IsChecked.Value + "  " + NeedClearCache);
#endif
        }

        private void OnRestoreStateChanged(object sender, RoutedEventArgs e)
        {
            CheckBox c = (CheckBox)sender;
            NeedRestore = c.IsChecked.Value;
#if DEBUG
            Trace.WriteLine(c.IsChecked.Value+"  "+NeedRestore);
#endif
        }

        //when manually close by user, only store the settings
        private void OnClosed(object sender, EventArgs e)
        {

            SaveSettings();
        }

    }
}