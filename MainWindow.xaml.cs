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

        private string configLoc;

        //universal vars
        public class UniversalVars
        {
            public DirectoryInfo gameDir, resourceDir;
            public string profileLoc, cacheLoc, optionLoc;
            public bool NeedRestore,NeedClearCache, NeedRedisplay, AlwaysConfirm;
            public UniversalVars()
            {
                gameDir = new DirectoryInfo("");
                resourceDir = new DirectoryInfo("");
                profileLoc = "";
                cacheLoc = "";
                optionLoc = "";
            }
        }
        public UniversalVars universalVars;
        //launcher vars
        public class LauncherVars
        {
            public enum LaunchMethod
            {
                FileReplace,
                DX101
            }
            public LaunchMethod lm;
            public bool showAddModInfo;

            public LauncherVars()
            {
            }
        }
        public LauncherVars launcherVars;

        //settings vars
        public class SettingsVars
        {

        }
        public SettingsVars settingsVars;

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
            //init launcher vars
            
        }
        private void InitBasicData()
        {

            universalVars.gameDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            universalVars.resourceDir = new DirectoryInfo(Directory.GetCurrentDirectory());

            universalVars = new UniversalVars();
            launcherVars = new LauncherVars();
            settingsVars = new SettingsVars();

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
                                universalVars.NeedRestore = bool.Parse(line);
                                break;
                            case 3:
                                universalVars.NeedClearCache = bool.Parse(line);
                                break;
                            case 4:
                                universalVars.NeedRedisplay = bool.Parse(line);
                                break;
                            case 5:
                                universalVars.AlwaysConfirm = bool.Parse(line);
                                break;
                            case 6:
                                //get cached game location
                                //if AlwaysConfirm=true this cache won't be use in later functions
                                if (Directory.Exists(line) == true && universalVars.AlwaysConfirm != true)
                                {
                                    universalVars.gameDir = new DirectoryInfo(line);
                                    HasGetGameRoot = true;

                                    //avoid catch by steam
                                    Environment.CurrentDirectory = universalVars.gameDir.FullName;
                                }
                                break;
                            case 7:
                                //get cached resource location
                                //if AlwaysConfirm=true this cache won't be use in later functions
                                if (Directory.Exists(line) == true && universalVars.AlwaysConfirm != true)
                                {
                                    universalVars.resourceDir = new DirectoryInfo(line);
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
                    universalVars.NeedRestore = false;
                    universalVars.NeedClearCache = true;
                    universalVars.NeedRedisplay = true;
                    universalVars.AlwaysConfirm = false;
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
                universalVars.NeedRestore = false;
                universalVars.NeedClearCache = true;
                universalVars.NeedRedisplay = true;
                universalVars.AlwaysConfirm = false;
            }
        }

        private void GetProfileLoc()
        {
            universalVars.profileLoc = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\digitalmindsoft\\gates of hell";
            //fall back to backup path
            if (Directory.Exists(universalVars.profileLoc) == false)
            {
                universalVars.profileLoc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\my games\\gates of hell";
            }



            if (Directory.Exists(universalVars.profileLoc) == false)
            {
                MessageBox.Show("Can not find goh user profile on your computer!", "WARNING");
                throw new InvalidOperationException("Can not find goh user profile on your computer!");
            }
            else
            {
                universalVars.cacheLoc = universalVars.profileLoc + "\\shader_cache";
                universalVars.optionLoc = universalVars.profileLoc + "\\profiles";
                universalVars.optionLoc = Directory.GetDirectories(universalVars.optionLoc)[0] + @"\options.set";

#if DEBUG
                Trace.WriteLine(universalVars.optionLoc);
#endif

                if (File.Exists(universalVars.optionLoc) == false)
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
                            universalVars.gameDir = new DirectoryInfo(gameLoc);
                            universalVars.resourceDir = new DirectoryInfo(gameLib += @"\steamapps\common\Call to Arms - Gates of Hell\resource");
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
                universalVars.gameDir = new DirectoryInfo(gameLoc);
                universalVars.resourceDir = universalVars.gameDir.GetDirectories("common/Call to Arms - Gates of Hell/resource")[0];
                universalVars.gameDir = universalVars.gameDir.GetDirectories("common/Call to Arms - Gates of Hell/binaries/x64")[0];

                HasGetGameRoot = true;
            }

            //avoid catch by steam
            Environment.CurrentDirectory = universalVars.gameDir.FullName;

            //manual check
            //reference https://www.c-sharpcorner.com/UploadFile/mahesh/understanding-message-box-in-windows-forms-using-C-Sharp/
            string message = "A GOH game has been found in \n" + universalVars.gameDir + "\n\nIf this is NOT the correct location, Please click NO and report detailed information to developers.";
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
            Trace.WriteLine(universalVars.gameDir);
            //MessageBox.Show(gameDir.FullName);
#endif
        }

        public void ClearCacheWork()
        {

            if (Directory.Exists(universalVars.cacheLoc) == true)
            {
                Directory.Delete(universalVars.cacheLoc, true);
            }

        }

        public void SaveSettings()
        {
            //Directory.CreateDirectory(Path.GetTempPath() + @"GOHSMSLauncher");

            using (StreamWriter sw = File.CreateText(configLoc))
            {
                sw.WriteLine(launcherVars.lm.ToString());
                sw.WriteLine(launcherVars.showAddModInfo.ToString());
                sw.WriteLine(universalVars.NeedRestore);
                sw.WriteLine(universalVars.NeedClearCache);
                sw.WriteLine(universalVars.NeedRedisplay);
                sw.WriteLine(universalVars.AlwaysConfirm);
                if (HasGetGameRoot == true)
                {
                    sw.WriteLine(universalVars.gameDir.FullName);
                    sw.WriteLine(universalVars.resourceDir.FullName);
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