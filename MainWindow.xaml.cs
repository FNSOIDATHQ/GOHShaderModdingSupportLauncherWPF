using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using GOHShaderModdingSupportLauncherWPF.Properties;
using System.Linq;



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
            public bool NeedRestore, NeedClearCache, NeedRedisplay, NeedCompileWarning, NeedLockModList, NeedAutoLoad, NeedCheckShaderModify, AlwaysConfirm;

            public DirectoryInfo? workshopDir, localDir;
            //option.set name -> mod
            public Dictionary<string, Mod> modDic;
            public List<Mod> modLoaded;
            public bool hasMod;
            //default value of lastShaderHash is 0, means the game is using default shader
            //in this case shader modify check should return true to auto load caches
            public string lastCacheHash, lastShaderHash;
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
            public bool runAsAdmin;

            public bool AdminUsed;
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
            AppDiagnostics.Log("Loading main window XAML.");
            InitializeComponent();
            //Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Dark);

            mv = this.FindName("mainView") as Wpf.Ui.Controls.NavigationView;


            mv.Loaded += navToDefaultPage;

            universalVars = new UniversalVars();
            launcherVars = new LauncherVars();
            settingsVars = new SettingsVars();
            toolsVars = new ToolsVars();
            modManagerVars = new ModManagerVars();

            InitBasicData();
            ContentRendered += (_, _) => AppDiagnostics.Log("Main window rendered.");
        }

        private void InitBasicData()
        {
            universalVars.gameDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            universalVars.resourceDir = new DirectoryInfo(Directory.GetCurrentDirectory());

            HasGetGameRoot = false;
            HasGetProfileLoc = false;

            AppDiagnostics.Log("Loading settings.");
            LoadConfigFromFile();

            if (HasGetProfileLoc == false)
            {
                AppDiagnostics.Log("Finding game profile.");
                GetProfileLoc();
            }

            if (HasGetGameRoot == false)
            {
                AppDiagnostics.Log("Finding game installation.");
                GetGameRoot();
            }

            AppDiagnostics.Log("Scanning mods.");
            RefreshMods();

            SaveSettings();
            AppDiagnostics.Log("Startup data initialized.");
        }

        private void LoadConfigFromFile()
        {
            SetDefaultSettings();
            universalVars.configLoc = FileManager.SettingsPath;

            //temp fallback
            string legacyPath = Path.Combine(AppContext.BaseDirectory, "settings.conf");
            string source = File.Exists(universalVars.configLoc) ? universalVars.configLoc : legacyPath;
            if (!File.Exists(source)) return;

            string[] lines;
            try
            {
                lines = FileManager.ReadSettings(source);
            }
            catch (Exception ex) when (FileManager.IsFileError(ex) || ex is FormatException)
            {
                AppDiagnostics.Log($"Ignoring invalid/unreadable settings: {source}", ex);
                MessageBox.Show(i18n.Main_ErrorReadConfig, i18n.Universal_Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            launcherVars.lm = Enum.Parse<LauncherVars.LaunchMethod>(lines[0]);
            launcherVars.showAddModInfo = bool.Parse(lines[1]);
            launcherVars.runAsAdmin = launcherVars.AdminUsed = bool.Parse(lines[2]);
            universalVars.NeedRestore = bool.Parse(lines[3]);
            universalVars.NeedClearCache = bool.Parse(lines[4]);
            universalVars.NeedRedisplay = bool.Parse(lines[5]);
            universalVars.NeedCompileWarning = bool.Parse(lines[6]);
            universalVars.NeedLockModList = bool.Parse(lines[7]);
            universalVars.AlwaysConfirm = bool.Parse(lines[8]);
            universalVars.NeedAutoLoad = bool.Parse(lines[9]);
            universalVars.NeedCheckShaderModify = bool.Parse(lines[10]);
            universalVars.lastCacheHash = lines[11];
            universalVars.lastShaderHash = lines[12];

            if (lines.Length != 15 || universalVars.AlwaysConfirm) return;

            try { HasGetGameRoot = TrySetGameDirectory(lines[13]); }
            catch (Exception ex) when (FileManager.IsFileError(ex)) { AppDiagnostics.Log("Cached game path is unavailable.", ex); }

            try { HasGetProfileLoc = TrySetProfileDirectory(lines[14]); }
            catch (Exception ex) when (FileManager.IsFileError(ex)) { AppDiagnostics.Log("Cached profile path is unavailable.", ex); }
        }

        private void SetDefaultSettings()
        {
            launcherVars.lm = LauncherVars.LaunchMethod.FileReplace;
            launcherVars.showAddModInfo = true;
            launcherVars.runAsAdmin = true;
            launcherVars.AdminUsed = true;
            universalVars.NeedRedisplay = true;
            universalVars.NeedCompileWarning = true;
            universalVars.NeedCheckShaderModify = true;
            universalVars.lastCacheHash = "-1";
            universalVars.lastShaderHash = "0";
        }

        private bool TrySetGameDirectory(string path)
        {
            if (FileManager.IsGameDirectory(path) == false) return false;

            var game = new DirectoryInfo(path);
            var root = game.Parent!.Parent!;
            var local = new DirectoryInfo(Path.Combine(root.FullName, "mods"));
            var workshop = new DirectoryInfo(
                                    Path.GetFullPath(
                                                Path.Combine(root.FullName, "..", "..", "workshop", "content", "400750")
                                                )
                                            );

            Environment.CurrentDirectory = game.FullName;
            universalVars.gameDir = game;
            universalVars.resourceDir = new DirectoryInfo(Path.Combine(root.FullName, "resource"));
            universalVars.localDir = local;
            universalVars.workshopDir = workshop;

            return true;
        }

        private bool TrySetProfileDirectory(string path)
        {
            string? options = FileManager.FindOptionsFile(path);
            if (options == null) return false;

            universalVars.profileLoc = path;
            universalVars.cacheLoc = Path.Combine(path, "shader_cache");
            universalVars.optionLoc = options;

            return true;
        }

        private void GetProfileLoc()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "digitalmindsoft", "gates of hell"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "my games", "gates of hell")
            };

            foreach (string candidate in candidates)
            {
                try
                {
                    if (TrySetProfileDirectory(candidate))
                    {
                        HasGetProfileLoc = true;
                        return;
                    }
                }
                catch (Exception ex) when (FileManager.IsFileError(ex))
                {
                    AppDiagnostics.Log($"Unable to inspect profile: {candidate}", ex);
                }
            }
            throw new InvalidOperationException($"{i18n.Main_NoProfile}\n{i18n.Main_RunGameOnce}\n\n{string.Join("\n", candidates)}");
        }

        private void GetGameRoot()
        {
            var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Shortcuts can start in an unrelated working directory
            foreach (var start in new[] {
                AppContext.BaseDirectory
                , Directory.GetCurrentDirectory()
                })
            {
                for (DirectoryInfo? dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                {
                    if (dir.Name.Equals("steamapps", StringComparison.OrdinalIgnoreCase))
                    {
                        libraries.Add(dir.FullName);
                    }
                }
            }

            var steamPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var hive in new[] {
                RegistryHive.CurrentUser
                , RegistryHive.LocalMachine
            })
            {
                foreach (var view in new[] {
                    RegistryView.Registry64
                    , RegistryView.Registry32
                })
                {
                    try
                    {
                        using var registry = RegistryKey.OpenBaseKey(hive, view);
                        using var steam = registry.OpenSubKey(@"SOFTWARE\Valve\Steam");
                        var location = steam?.GetValue(hive == RegistryHive.CurrentUser ? "SteamPath" : "InstallPath") as string;

                        if (string.IsNullOrWhiteSpace(location) == false) steamPaths.Add(location);
                    }
                    catch (Exception ex) when (FileManager.IsFileError(ex))
                    {
                        AppDiagnostics.Log("Unable to read a Steam registry location.", ex);
                    }
                }
            }

            foreach (string steamPath in steamPaths)
            {
                string steamApps = Path.Combine(steamPath, "steamapps");
                libraries.Add(steamApps);
                try
                {
                    string vdf = Path.Combine(steamApps, "libraryfolders.vdf");
                    if (File.Exists(vdf))
                    {
                        foreach (string library in FileManager.ReadLibraryPaths(vdf))
                        {
                            libraries.Add(Path.Combine(library, "steamapps"));
                        }
                    }
                }
                catch (Exception ex) when (FileManager.IsFileError(ex))
                {
                    AppDiagnostics.Log("Unable to read Steam libraries; trying the known locations.", ex);
                }
            }

            foreach (string library in libraries)
            {
                try
                {
                    if (TrySetGameDirectory(Path.Combine(library, "common", "Call to Arms - Gates of Hell", "binaries", "x64")))
                    {
                        HasGetGameRoot = true;
                        break;
                    }
                }
                catch (Exception ex) when (FileManager.IsFileError(ex))
                {
                    AppDiagnostics.Log($"Unable to inspect Steam library: {library}", ex);
                }
            }

            if (HasGetGameRoot == false)
            {
                throw new InvalidOperationException(libraries.Count == 0 ? i18n.Main_NoSteam : i18n.Main_NoGame);
            }

            string message = $"{i18n.Main_FoundGame0}\n{universalVars.gameDir}\n\n{i18n.Main_FoundGame1}";
            if (MessageBox.Show(message, i18n.Main_MaunalCheck, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
            {
                // Do not cache a path that the user rejected.
                HasGetGameRoot = false;
                SaveSettings();
                AppDiagnostics.Log("User declined the detected game path.");
                Environment.Exit(0);
            }
            
            ClearCacheWork();
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
                    string line;
                    line = log.ReadLine();

                    if (line.IndexOf("compile error:") != -1)
                    {
                        string errorMsg = line + "\n\n";
                        bool hasError = false;
                        while (!string.IsNullOrEmpty(line = log.ReadLine()))
                        {
                            errorMsg += line + "\n\n";
                            if (line.Contains("error") == false)
                            {
                                hasError = true;
                            }
                        }

                        string errorMessage = $"{i18n.Main_ShaderCompileErrorMessage1}\n\n\n\n";
                        errorMessage += errorMsg;

                        if (hasError == true)
                        {
                            MessageBox.Show($"{i18n.Main_ShaderCompileErrorMessage0}\n\n" + errorMessage, i18n.Main_ShaderCompileErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show($"{i18n.Main_ShaderCompileErrorMessage2}\n\n" + errorMessage, i18n.Main_ShaderCompileErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                        }

                        break;
                    }
                }

                log.Close();
            }
        }

        public void RefreshMods(bool afterGaming = false)
        {
            universalVars.modDic.Clear();


            ReadModsFromWorkshop();
            ReadModsFromLocal();

            if (universalVars.NeedLockModList == true && afterGaming == true)
            {
                verifyLoadedMods();
            }
            else
            {
                universalVars.modLoaded.Clear();
                ReadLoadedMods();
            }
        }

        private string getModShowName(FileInfo[] modInfo)
        {
            string name = "";
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
                            if (nameS == -1 || nameL <= 0)
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
            catch (Exception e)
            {
                MessageBox.Show($"{i18n.Main_ErrorReadModInfo}\n" + e, i18n.Universal_Error, MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    using ZipArchive curPak = ZipFile.Open(obj.FullName, ZipArchiveMode.Read);

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
            if (universalVars.workshopDir?.Exists != true) return;

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

                if (universalVars.modDic.TryAdd(folderName, single) == false)
                {
                    AppDiagnostics.Log($"Duplicate mod identifier ignored: {folderName} ({dir.FullName})");
                }
            }
        }

        private void ReadModsFromLocal()
        {
            if (universalVars.localDir?.Exists != true) return;

            foreach (var dir in universalVars.localDir.GetDirectories())
            {
                FileInfo[] modInfo = dir.GetFiles("mod.info", SearchOption.TopDirectoryOnly);
                string folderName = dir.Name.ToLowerInvariant();

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

                if (universalVars.modDic.TryAdd(folderName, single) == false)
                {
                    AppDiagnostics.Log($"Duplicate mod identifier ignored: {folderName} ({dir.FullName})");
                }
            }
        }

        public void ReadLoadedMods()
        {
            using var options = File.OpenText(universalVars.optionLoc);
            foreach (string modName in FileManager.ReadLoadedModNames(options))
            {
                if (universalVars.modDic.TryGetValue(modName, out var mod) && mod.hasLoad == false)
                {
                    mod.hasLoad = true;
                    universalVars.modLoaded.Add(mod);
                }
            }
        }

        public void verifyLoadedMods()
        {
            string modified = "";
            using (StreamReader opt = File.OpenText(universalVars.optionLoc))
            {
                //push to mod list start point
                while (opt.ReadLine() is var line)
                {
                    //no mod section
                    if (opt.EndOfStream == true)
                    {
                        modified += "\t{mods\r\n";
                        break;
                    }
                    modified += line + "\r\n";
                    if (line.Contains("{mods") == true)
                    {
                        break;
                    }

                }

                opt.Close();
            }

            List<Mod> purned = new List<Mod>();
            List<Mod> newList = universalVars.modDic.Values.ToList();
            foreach (Mod mod in universalVars.modLoaded)
            {
                int newIndex = newList.FindIndex(m => m.folderName == mod.folderName);
                if (newIndex != -1)
                {
                    modified += "\t\t\"" + mod.folderName + ":0\"\r\n";
                    purned.Add(newList[newIndex]);
                    universalVars.modDic[mod.folderName].hasLoad = true;
                }
                else
                {

                }
            }
            universalVars.modLoaded = purned;

            modified += "\t}\r\n}\r\n";

            File.WriteAllText(universalVars.optionLoc, modified);
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
            if (
                AppDiagnostics.IsFatal
                || string.IsNullOrWhiteSpace(universalVars.configLoc)
                ) return;

            try
            {
                var lines = new List<string>{
                    launcherVars.lm.ToString()
                    , launcherVars.showAddModInfo.ToString()
                    , launcherVars.runAsAdmin.ToString()
                    , universalVars.NeedRestore.ToString()
                    , universalVars.NeedClearCache.ToString()
                    , universalVars.NeedRedisplay.ToString()
                    , universalVars.NeedCompileWarning.ToString()
                    , universalVars.NeedLockModList.ToString()
                    , universalVars.AlwaysConfirm.ToString()
                    , universalVars.NeedAutoLoad.ToString()
                    , universalVars.NeedCheckShaderModify.ToString()
                    , FileManager.NormalizeCacheHash(universalVars.lastCacheHash)
                    , FileManager.NormalizeShaderHash(universalVars.lastShaderHash)
                };
                if (HasGetGameRoot && HasGetProfileLoc)
                {
                    lines.Add(universalVars.gameDir!.FullName);
                    lines.Add(universalVars.profileLoc);
                }
                FileManager.WriteSettings(universalVars.configLoc, lines);
            }
            catch (Exception ex) when (FileManager.IsFileError(ex))
            {
                AppDiagnostics.Log("Unable to save launcher settings.", ex);
                MessageBox.Show($"{universalVars.configLoc}\n\n{ex.Message}", i18n.Universal_Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
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