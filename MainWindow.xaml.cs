using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using GOHShaderModdingSupportLauncherWPF.Properties;



namespace GOHShaderModdingSupportLauncherWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        Wpf.Ui.Controls.NavigationView mv;

        private bool HasGetGameRoot, HasGetProfileLoc;

        public class Mod
        {
            public string name { get; set; }
            public string type { get; set; }
            public bool hasShader { get; set; }
            public string path;
            public string folderName;
            public bool hasLoad;

            public Mod(string n, string t, string p, string fn, bool hS)
            {
                name = n;
                type = t;
                path = p;
                folderName = fn;
                hasShader = hS;
                hasLoad = false;
            }
        };

        //universal vars
        public class UniversalVars
        {
            public DirectoryInfo? gameDir, resourceDir;
            public string profileLoc, cacheLoc, optionLoc;
            public string configLoc;
            public bool NeedRestore,NeedClearCache, NeedRedisplay, NeedCompileWarning,NeedAutoLoad, NeedCheckShaderModify, AlwaysConfirm;

            public DirectoryInfo? workshopDir, localDir;
            //option.set name -> mod
            public Dictionary<string, Mod> modDic;
            public List<Mod> modLoaded;
            public bool hasMod;
            //default value of lastShaderHash is 0, means the game is using default shader
            //in this case shader modify check should return true to auto load caches
            public string lastCacheHash,lastShaderHash;
            public UniversalVars()
            {
                profileLoc = "";
                cacheLoc = "";
                optionLoc = "";
                configLoc = "";
                modLoaded = new List<Mod>();
                modDic = new Dictionary<string, Mod>();
                lastCacheHash = "";
                lastShaderHash = "";
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
        }
        public LauncherVars launcherVars;

        //settings vars
        public class SettingsVars
        {

        }
        public SettingsVars settingsVars;

        //settings vars
        public class ToolsVars
        {

        }
        public ToolsVars toolsVars;

        //mod manager vars
        public class ModManagerVars
        {
            public bool hasMod;
        }
        public ModManagerVars modManagerVars;

        public MainWindow()
        {
            InitializeComponent();
            //Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Dark);

            mv=this.FindName("mainView") as Wpf.Ui.Controls.NavigationView;

            
            mv.Loaded += navToDefaultPage;

            universalVars = new UniversalVars();
            launcherVars = new LauncherVars();
            settingsVars = new SettingsVars();
            toolsVars = new ToolsVars();
            modManagerVars = new ModManagerVars();

            InitBasicData();
        }
        
        private void InitBasicData()
        {
            


            universalVars.gameDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            universalVars.resourceDir = new DirectoryInfo(Directory.GetCurrentDirectory());

            HasGetGameRoot = false;
            HasGetProfileLoc = false;

            LoadConfigFromFile();

            if (HasGetProfileLoc == false)
            {
                GetProfileLoc();
            }

            if (HasGetGameRoot == false)
            {
                GetGameRoot();
            }

            RefreshMods();

            SaveSettings();
        }

        private void LoadConfigFromFile()
        {
            universalVars.configLoc = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"settings.conf";
            if (File.Exists(universalVars.configLoc) == true)
            {
                int index = 0;
                try
                {
                    foreach (string line in File.ReadAllLines(universalVars.configLoc))
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
                                universalVars.NeedCompileWarning = bool.Parse(line);
                                break;
                            case 6:
                                universalVars.AlwaysConfirm = bool.Parse(line);
                                break;
                            case 7:
                                universalVars.NeedAutoLoad = bool.Parse(line);
                                break;
                            case 8:
                                universalVars.NeedCheckShaderModify = bool.Parse(line);
                                break;
                            case 9:
                                universalVars.lastCacheHash = line;
                                break;
                            case 10:
                                universalVars.lastShaderHash = line;
                                break;
                            case 11:
                                //get cached game location
                                //if AlwaysConfirm=true this cache won't be use in later functions
                                if (Directory.Exists(line) == true && universalVars.AlwaysConfirm != true)
                                {
                                    universalVars.gameDir = new DirectoryInfo(line);
                                    //avoid catch by steam
                                    Environment.CurrentDirectory = universalVars.gameDir.FullName;

                                    DirectoryInfo root = universalVars.gameDir.Parent.Parent;
                                    universalVars.resourceDir = root.GetDirectories("resource")[0];
                                    universalVars.localDir= root.GetDirectories("mods")[0];
                                    universalVars.workshopDir = root.GetDirectories("..\\..\\workshop\\content\\400750")[0];
#if DEBUG
                                    Trace.WriteLine("workshop dir= "+ universalVars.workshopDir.FullName);
#endif

                                    HasGetGameRoot = true;
                                }
                                break;
                            case 12:
                                //get cached resource location
                                //if AlwaysConfirm=true this cache won't be use in later functions
                                if (Directory.Exists(line) == true && universalVars.AlwaysConfirm != true)
                                {
                                    universalVars.profileLoc = line;
                                    universalVars.cacheLoc = universalVars.profileLoc + "\\shader_cache";
                                    universalVars.optionLoc = universalVars.profileLoc + "\\profiles";
                                    universalVars.optionLoc = Directory.GetDirectories(universalVars.optionLoc)[0] + @"\options.set";

                                    HasGetProfileLoc = true;
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
                    MessageBox.Show(i18n.Main_ErrorReadConfig, i18n.Universal_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    File.Delete(universalVars.configLoc);
                }


                //the config file is not complete or break,fall back to default value
                //no cache for path is not important
                if (index != 11 && index != 13)
                {
                    launcherVars.lm = LauncherVars.LaunchMethod.FileReplace;
                    launcherVars.showAddModInfo = true;
                    universalVars.NeedRestore = false;
                    universalVars.NeedClearCache = false;
                    universalVars.NeedRedisplay = true;
                    universalVars.NeedCompileWarning = true;
                    universalVars.NeedAutoLoad = false;
                    universalVars.NeedCheckShaderModify = true;
                    universalVars.AlwaysConfirm = false;
                    universalVars.lastCacheHash = "-1";
                    universalVars.lastShaderHash = "0";
                    HasGetGameRoot = false;
                    HasGetProfileLoc = false;
                }
            }
            else
            {
                launcherVars.lm = LauncherVars.LaunchMethod.FileReplace;
                launcherVars.showAddModInfo = true;
                universalVars.NeedRestore = false;
                universalVars.NeedClearCache = false;
                universalVars.NeedRedisplay = true;
                universalVars.NeedCompileWarning = true;
                universalVars.NeedAutoLoad = false;
                universalVars.NeedCheckShaderModify = true;
                universalVars.AlwaysConfirm = false;
                universalVars.lastCacheHash = "-1";
                universalVars.lastShaderHash = "0";
                HasGetProfileLoc = false;
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
                    MessageBox.Show(i18n.Main_NoSteam, i18n.Universal_Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    throw new InvalidOperationException(i18n.Main_NoSteam);
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
#if DEBUG
                            Trace.WriteLine("find game location= " + gameLib);
#endif
                            universalVars.gameDir = new DirectoryInfo(gameLoc);
                            universalVars.resourceDir = new DirectoryInfo(gameLib + @"\steamapps\common\Call to Arms - Gates of Hell\resource");
                            universalVars.localDir = new DirectoryInfo(gameLib + @"\steamapps\common\Call to Arms - Gates of Hell\mods");
                            universalVars.workshopDir = new DirectoryInfo(gameLib + @"\steamapps\workshop\content\400750");

                            HasGetGameRoot = true;
                            break;
                        }

                    }
                }

                if (HasGetGameRoot == false)
                {
                    MessageBox.Show(i18n.Main_NoGame, i18n.Universal_Warning);
                    throw new InvalidOperationException(i18n.Main_NoGame);
                }
            }
            else
            {
                index += 9;
                string gameLoc = appLoc.Substring(0, index);
#if DEBUG
                Trace.WriteLine("find game location= "+gameLoc);
#endif

                universalVars.gameDir = new DirectoryInfo(gameLoc);
                universalVars.resourceDir = universalVars.gameDir.GetDirectories("common/Call to Arms - Gates of Hell/resource")[0];
                universalVars.localDir = universalVars.gameDir.GetDirectories("common/Call to Arms - Gates of Hell/mods")[0];
                universalVars.workshopDir = universalVars.gameDir.GetDirectories(@"workshop\content\400750")[0];
                universalVars.gameDir = universalVars.gameDir.GetDirectories("common/Call to Arms - Gates of Hell/binaries/x64")[0];

                HasGetGameRoot = true;
            }

            //avoid catch by steam
            Environment.CurrentDirectory = universalVars.gameDir.FullName;

            //manual check
            //reference https://www.c-sharpcorner.com/UploadFile/mahesh/understanding-message-box-in-windows-forms-using-C-Sharp/
            string message = $"{i18n.Main_FoundGame0} \n" + universalVars.gameDir + $"\n\n{i18n.Main_FoundGame1}";
            string title = i18n.Main_MaunalCheck;
            MessageBoxResult result = MessageBox.Show(message, title, MessageBoxButton.YesNo,MessageBoxImage.Information);
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
                MessageBox.Show(i18n.Main_NoProfile, i18n.Universal_Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
                throw new InvalidOperationException(i18n.Main_NoProfile);
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
                    MessageBox.Show(i18n.Main_RunGameOnce, i18n.Universal_Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    throw new InvalidOperationException(i18n.Main_RunGameOnce);
                }
            }
        }

        public void ClearCacheWork()
        {

            if (Directory.Exists(universalVars.cacheLoc) == true)
            {
                Directory.Delete(universalVars.cacheLoc, true);
            }

        }

        public void CheckCompileWarning()
        {
            using (StreamReader log = File.OpenText(universalVars.profileLoc + @"\log\game.log"))
            {
                while (log.EndOfStream != true)
                {
                    string line = log.ReadLine();

                    if (line.IndexOf("compile error:") != -1)
                    {
                        string errorMessage = $"{i18n.Main_ShaderCompileErrorMessage0}\n\n";
                        errorMessage += $"{i18n.Main_ShaderCompileErrorMessage1}\n\n";
                        errorMessage += line + '\n';
                        errorMessage += log.ReadLine();
                        MessageBox.Show(errorMessage, i18n.Main_ShaderCompileErrorTitle,MessageBoxButton.OK,MessageBoxImage.Error);
                        break;
                    }
                }

                log.Close();
            }
        }

        public void RefreshMods()
        {
            universalVars.modDic.Clear();
            universalVars.modLoaded.Clear();

            ReadModsFromWorkshop();
            ReadModsFromLocal();

            ReadLoadedMods();
        }

        private string getModShowName(FileInfo[] modInfo)
        {
            string name="";
            try
            {
                using (StreamReader info = modInfo[0].OpenText())
                {
                    string nameLine;
                    //push to name line
                    while (info.EndOfStream == false)
                    {
                        while (((nameLine = info.ReadLine()).Contains("name", StringComparison.OrdinalIgnoreCase) == false || nameLine.Contains("{", StringComparison.Ordinal) == false) && info.EndOfStream == false) ;

                        int commentIndex = nameLine.IndexOf(";");
                        nameLine = nameLine.Substring(0, commentIndex == -1 ? nameLine.Length : commentIndex);
#if DEBUG
                        Trace.WriteLine("get nameline= " + nameLine);
#endif
                        if (nameLine.Length > 0)
                        {
                            int nameS = nameLine.IndexOf('"') + 1;
                            int nameL = nameLine.LastIndexOf('"') - nameS;
                            if(nameS==-1|| nameL <= 0)
                            {
                                continue;
                            }
                            name = nameLine.Substring(nameS, nameL);
#if DEBUG
                            Trace.WriteLine("mod show name= " + name);
#endif
                            break;
                        }

                    }


                    info.Close();
                }

                if (name == "")
                {
                    name = i18n.Main_ModErrorName;
                }
            }
            catch(Exception e)
            {
                MessageBox.Show($"{i18n.Main_ErrorReadModInfo}\n"+e, i18n.Universal_Error, MessageBoxButton.OK, MessageBoxImage.Warning);
                name = i18n.Main_ModErrorName;
            }

            return name;
        }

        private bool checkShader(DirectoryInfo root)
        {
            DirectoryInfo[] searchResult = root.GetDirectories("resource", SearchOption.TopDirectoryOnly);
            if (searchResult.Length != 0)
            {
                DirectoryInfo resouce = searchResult[0];

                //check if shader is in paks
                foreach (var obj in resouce.GetFiles("*.pak", SearchOption.TopDirectoryOnly))
                {
#if DEBUG
                    Trace.WriteLine("get a pak= " + obj.Name);
#endif
                    ZipArchive curPak = ZipFile.Open(obj.FullName, ZipArchiveMode.Read);

                    foreach (var file in curPak.Entries)
                    {
                        if (file.FullName.Contains("shader/dx10/") == true)
                        {
                            if (file.Length > 0)
                            {
#if DEBUG
                                Trace.WriteLine("get a shader in pak= " + file.FullName);
#endif
                                return true;
                            }
                        }
                    }

                }

                string shaderFolder = resouce.FullName + "/shader/dx10";
                //check if shader is in dir
                if (Directory.Exists(shaderFolder) == true)
                {
                    DirectoryInfo shader = new DirectoryInfo(shaderFolder);
                    foreach (FileInfo file in shader.GetFiles("*", SearchOption.AllDirectories))
                    {
#if DEBUG
                        Trace.WriteLine("get a shader file= " + file.Name);
#endif
                        if (file.Length > 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void ReadModsFromWorkshop()
        {
            foreach (var dir in universalVars.workshopDir.GetDirectories())
            {
                FileInfo[] modInfo = dir.GetFiles("mod.info", SearchOption.TopDirectoryOnly);
                string folderName = "mod_" + dir.Name;

                if (modInfo.Length == 0)
                {
                    //not a mod
                    continue;
                }
                else if (folderName.Contains(' ') == true)
                {
                    //is a mod, but not valid
                    continue;
                }
#if DEBUG
                Trace.WriteLine("mod dir name= " + dir.Name);
#endif
                string name = getModShowName(modInfo);

                bool hasShader = checkShader(dir);


                MainWindow.Mod single = new MainWindow.Mod(name, i18n.Main_ModWorkshop, dir.FullName, folderName, hasShader);

                universalVars.modDic.Add(folderName, single);
            }
        }

        private void ReadModsFromLocal()
        {
            foreach (var dir in universalVars.localDir.GetDirectories())
            {
                FileInfo[] modInfo = dir.GetFiles("mod.info", SearchOption.TopDirectoryOnly);
                string folderName = dir.Name.ToLower();

                if (modInfo.Length == 0)
                {
                    //not a mod
                    continue;
                }
                else if (folderName.Contains(' ') == true)
                {
                    //is a mod, but not valid
                    continue;
                }
                string name = getModShowName(modInfo);

                bool hasShader = checkShader(dir);



                MainWindow.Mod single = new MainWindow.Mod(name, i18n.Main_ModLocal, dir.FullName, folderName, hasShader);

                universalVars.modDic.Add(folderName, single);
            }
        }

        public void ReadLoadedMods()
        {
            using (StreamReader opt = File.OpenText(universalVars.optionLoc))
            {
                //push to mod list start point
                while (opt.ReadLine().Contains("{mods") == false)
                {
                    if (opt.EndOfStream == true)
                    {
                        //no mod loaded
                        return;
                    }
                }

                while ((opt.ReadLine() is var line) && line.Contains("}") == false)
                {
#if DEBUG
                    Trace.WriteLine(line);
#endif
                    int nameS = line.IndexOf("\"") + 1;
                    int nameE = line.IndexOf(":");
                    if (nameS == -1 || nameE == -1)
                    {
                        //not valid
                        continue;
                    }

                    string modName = line.Substring(nameS, nameE - nameS);
#if DEBUG
                    Trace.WriteLine(modName);
#endif
                    //mod in option.set can be invalid
                    if (universalVars.modDic.ContainsKey(modName) == true)
                    {
                        universalVars.modDic[modName].hasLoad = true;
                        universalVars.modLoaded.Add(universalVars.modDic[modName]);
                    }
                    

                }

                opt.Close();
            }
        }

        //reference https://juejin.cn/post/6989143365862293534
        public static void ExtractFile(String resource, String path, int batch)
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

        //reference https://stackoverflow.com/questions/7646328/how-to-use-the-7z-sdk-to-compress-and-decompress-a-file
        public void CompressFileLZMA(string inFile, string outFile)
        {
            SevenZip.Compression.LZMA.Encoder coder = new SevenZip.Compression.LZMA.Encoder();
            FileStream input = new FileStream(inFile, FileMode.Open);
            FileStream output = new FileStream(outFile, FileMode.Create);

            // Write the encoder properties
            coder.WriteCoderProperties(output);

            // Write the decompressed file size.
            output.Write(BitConverter.GetBytes(input.Length), 0, 8);

            // Encode the file.
            coder.Code(input, output, input.Length, -1, null);
            output.Flush();
            output.Close();
            input.Close();
        }
        
        public void DecompressFileLZMA(string inFile, string outFile)
        {
            SevenZip.Compression.LZMA.Decoder coder = new SevenZip.Compression.LZMA.Decoder();
            FileStream input = new FileStream(inFile, FileMode.Open);
            FileStream output = new FileStream(outFile, FileMode.Create);

            // Read the decoder properties
            byte[] properties = new byte[5];
            input.Read(properties, 0, 5);

            // Read in the decompress file size.
            byte[] fileLengthBytes = new byte[8];
            input.Read(fileLengthBytes, 0, 8);
            long fileLength = BitConverter.ToInt64(fileLengthBytes, 0);

            coder.SetDecoderProperties(properties);
            coder.Code(input, output, input.Length, fileLength, null);
            output.Flush();
            output.Close();
            input.Close();
        }

        public void SaveSettings()
        {
            //Directory.CreateDirectory(Path.GetTempPath() + @"GOHSMSLauncher");

            using (StreamWriter sw = File.CreateText(universalVars.configLoc))
            {
                sw.WriteLine(launcherVars.lm.ToString());
                sw.WriteLine(launcherVars.showAddModInfo.ToString());
                sw.WriteLine(universalVars.NeedRestore);
                sw.WriteLine(universalVars.NeedClearCache);
                sw.WriteLine(universalVars.NeedRedisplay);
                sw.WriteLine(universalVars.NeedCompileWarning);
                sw.WriteLine(universalVars.AlwaysConfirm);
                sw.WriteLine(universalVars.NeedAutoLoad);
                sw.WriteLine(universalVars.NeedCheckShaderModify);
                sw.WriteLine(universalVars.lastCacheHash);
                sw.WriteLine(universalVars.lastShaderHash);
                if (HasGetGameRoot == true)
                {
                    sw.WriteLine(universalVars.gameDir.FullName);
                    sw.WriteLine(universalVars.profileLoc);
                }

            }
        }

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