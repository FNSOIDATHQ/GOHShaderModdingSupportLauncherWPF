using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using static GOHShaderModdingSupportLauncherWPF.MainWindow;


namespace GOHShaderModdingSupportLauncherWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        Wpf.Ui.Controls.NavigationView mv;

        private bool HasGetGameRoot, HasConfigFile;
        private DirectoryInfo gameDir, resourceDir;

        private bool NeedRestore, NeedClearCache, NeedRedisplay, AlwaysConfirm;
        private string configLoc;
        private string profileLoc, cacheLoc, optionLoc;

        //launcher vars
        public struct LauncherVars
        {
            public enum LaunchMethod
            {
                FileReplace,
                DX101
            }
            public LaunchMethod lm;
            public bool showAddModInfo;
            public DirectoryInfo gameDir, resourceDir;
            public string optionLoc;
            public bool NeedRestore, NeedClearCache, NeedRedisplay;
        }
        public LauncherVars launcherVars;


        public MainWindow()
        {
            InitializeComponent();
            //Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Dark);

            mv=this.FindName("mainView") as Wpf.Ui.Controls.NavigationView;

            
            mv.Loaded += navToDefaultPage;

            InitBasicData();
            TransferData();
        }
        ///*
        
        private void TransferData()
        {
            //init universal launcher vars
            launcherVars.gameDir = gameDir;
            launcherVars.resourceDir = resourceDir;
            launcherVars.optionLoc = optionLoc;
            launcherVars.NeedClearCache = NeedClearCache;
            launcherVars.NeedRedisplay = NeedRedisplay;
            launcherVars.NeedRestore = NeedRestore;
        }
        private void InitBasicData()
        {

            gameDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            resourceDir = new DirectoryInfo(Directory.GetCurrentDirectory());

            launcherVars = new LauncherVars();
            C_restore = this.FindName("restore") as CheckBox;
            C_clearCache = this.FindName("clearCache") as CheckBox;
            C_gameExit = this.FindName("gameExit") as CheckBox;
            C_pathConfirm = this.FindName("pathConfirm") as CheckBox;
            
            B_openConfig = this.FindName("openConfig") as Button;
            T_gamePath = this.FindName("gamePath") as TextBox;
            T_gameConfigPath = this.FindName("gameConfigPath") as TextBox;

            HasGetGameRoot = false;
            HasConfigFile = false;
            B_openConfig.IsEnabled = false;

            LoadConfigFromFile();

            GetProfileLoc();

            if (HasGetGameRoot == false)
            {
                GetGameRoot();
            }
        }

        private void LoadConfigFromFile()
        {
            configLoc = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"settings.conf";
            if (File.Exists(configLoc) == true)
            {
                int index = 0;
                try
                {
                    foreach (string line in File.ReadAllLines(configLoc))
                    {
                        switch (index)
                        {
                            case 0:
                                launcherVars.lm = (LauncherVars.LaunchMethod)Enum.Parse(typeof(LauncherVars.LaunchMethod), line);
                                break;
                            case 1:
                                launcherVars.showAddModInfo = bool.Parse(line);
                                break;
                            case 2:
                                NeedRestore = bool.Parse(line);

                                C_restore.IsChecked = NeedRestore;
                                break;
                            case 3:
                                NeedClearCache = bool.Parse(line);

                                C_clearCache.IsChecked = NeedClearCache;
                                break;
                            case 4:
                                NeedRedisplay = bool.Parse(line);

                                C_gameExit.IsChecked = NeedRedisplay;
                                break;
                            case 5:
                                AlwaysConfirm = bool.Parse(line);

                                C_pathConfirm.IsChecked = AlwaysConfirm;
                                break;
                            case 6:
                                //get cached game location
                                //if AlwaysConfirm=true this cache won't be use in later functions
                                if (Directory.Exists(line) == true && AlwaysConfirm != true)
                                {
                                    gameDir = new DirectoryInfo(line);
                                    T_gamePath.Text = gameDir.FullName;
                                    HasGetGameRoot = true;

                                    //avoid catch by steam
                                    Environment.CurrentDirectory = gameDir.FullName;
                                }
                                break;
                            case 7:
                                //get cached resource location
                                //if AlwaysConfirm=true this cache won't be use in later functions
                                if (Directory.Exists(line) == true && AlwaysConfirm != true)
                                {
                                    resourceDir = new DirectoryInfo(line);
                                }
                                break;
                            default:
                                break;
                        }
                        index++;

#if DEBUG
                        Trace.WriteLine(line);
                        Trace.WriteLine(index);
#endif
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Get error when reading config file! Will delete config file", "ERROR");
                    File.Delete(configLoc);
                }


                //the config file is not complete or break,fall back to default value
                //no cache is not important
                if (index != 6 && index != 8)
                {
                    launcherVars.lm = LauncherVars.LaunchMethod.FileReplace;
                    launcherVars.showAddModInfo = true;
                    NeedRestore = false;
                    C_restore.IsChecked = NeedRestore;
                    NeedClearCache = true;
                    C_clearCache.IsChecked = NeedClearCache;
                    NeedRedisplay = true;
                    C_gameExit.IsChecked = NeedRedisplay;
                    AlwaysConfirm = false;
                    C_pathConfirm.IsChecked = AlwaysConfirm;
                    HasGetGameRoot = false;
                }
                else
                {
                    HasConfigFile = true;
                    B_openConfig.IsEnabled = true;
                }
            }
            else
            {
                launcherVars.lm = LauncherVars.LaunchMethod.FileReplace;
                launcherVars.showAddModInfo = true;
                NeedRestore = false;
                NeedClearCache = true;
                NeedRedisplay = true;
                AlwaysConfirm = false;
            }
        }

        private void GetProfileLoc()
        {
            profileLoc = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\digitalmindsoft\\gates of hell";
            //fall back to backup path
            if (Directory.Exists(profileLoc) == false)
            {
                profileLoc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\my games\\gates of hell";
            }



            if (Directory.Exists(profileLoc) == false)
            {
                MessageBox.Show("Can not find goh user profile on your computer!", "WARNING");
                throw new InvalidOperationException("Can not find goh user profile on your computer!");
            }
            else
            {
                cacheLoc = profileLoc + "\\shader_cache";
                optionLoc = profileLoc + "\\profiles";
                optionLoc = Directory.GetDirectories(optionLoc)[0] + @"\options.set";
                T_gameConfigPath.Text = profileLoc;

#if DEBUG
                Trace.WriteLine(optionLoc);
#endif

                if (File.Exists(optionLoc) == false)
                {
                    MessageBox.Show("Run goh game at least once before using this program!", "WARNING");
                    throw new InvalidOperationException("Run goh game at least once before using this program!");
                }
            }
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
                    steamLoc = reg.GetValue(@"InstallPath").ToString() + @"\steamapps\libraryfolders.vdf";
                }
                else
                {
                    MessageBox.Show("Can not find steam app on your computer!", "WARNING");
                    throw new InvalidOperationException("Can not find steam app on your computer!");
                }

                //load libraryfolders.vdf to get all steam library location
                //I am to lazy to further search games from .acf files, so just try get every game in common folder
                foreach (string line in File.ReadAllLines(steamLoc))
                {
                    //search folder when get path
                    if (line.Contains("path") == true)
                    {
                        string gameLib = line.Split('"')[3];
                        string gameLoc = gameLib + @"\steamapps\common\Call to Arms - Gates of Hell\binaries\x64";
                        if (Directory.Exists(gameLoc) == true)
                        {
                            gameDir = new DirectoryInfo(gameLoc);
                            resourceDir = new DirectoryInfo(gameLib += @"\steamapps\common\Call to Arms - Gates of Hell\resource");
                            HasGetGameRoot = true;
                            break;
                        }

                    }
                }

                if (HasGetGameRoot == false)
                {
                    MessageBox.Show("Can not find GOH game on your computer!", "WARNING");
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

            T_gamePath.Text = gameDir.FullName;
            //avoid catch by steam
            Environment.CurrentDirectory = gameDir.FullName;

            //manual check
            //reference https://www.c-sharpcorner.com/UploadFile/mahesh/understanding-message-box-in-windows-forms-using-C-Sharp/
            string message = "A GOH game has been found in \n" + gameDir + "\n\nIf this is NOT the correct location, Please click NO and report detailed information to developers.";
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
                // preprocess
                //GetGameRoot() always happen when first find goh game, we clear cache to delete any shader that compile from different files
                ClearCacheWork();
            }

#if DEBUG
            Trace.WriteLine(gameDir);
            //MessageBox.Show(gameDir.FullName);
#endif
        }

        public void ClearCacheWork()
        {

            if (Directory.Exists(cacheLoc) == true)
            {
                Directory.Delete(cacheLoc, true);
            }

        }

        public void SaveSettings()
        {
            //Directory.CreateDirectory(Path.GetTempPath() + @"GOHSMSLauncher");

            using (StreamWriter sw = File.CreateText(configLoc))
            {
                sw.WriteLine(launcherVars.lm.ToString());
                sw.WriteLine(launcherVars.showAddModInfo.ToString());
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
        //*/

        private void navToDefaultPage(object? sender, EventArgs e)
        {
            mv.Navigate(typeof(Launcher));
        }

        //when manually close by user, only store the settings
        private void OnClosed(object sender, EventArgs e)
        {
            
            SaveSettings();
        }

    }
}