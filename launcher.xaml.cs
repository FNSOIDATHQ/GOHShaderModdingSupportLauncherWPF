using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Printing;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;


namespace GOHShaderModdingSupportLauncherWPF
{
    public partial class Launcher : Page
    {
        private MainWindow main;

        private MainWindow.LauncherVars vars;

        public Launcher()
        {
            main = Application.Current.MainWindow as MainWindow;

            vars = main.launcherVars;

            InitializeComponent();



            launchMethod.SelectedIndex = (int)vars.lm;

            addModInfo.IsChecked = vars.showAddModInfo;

        }

        public static void ReplaceFile(DirectoryInfo resourceDir)
        {
            FileInfo targetPak = new FileInfo(resourceDir + @"\shader.pak");
            if (targetPak.Length > 7 * 1048576)
            {
                //extract pak from exe
                //file is 326kb, so we use 350kb as a batch to extract everything once
                MainWindow.ExtractFile("GOHShaderModdingSupportLauncherWPF.pak.noCache.shader.pak", resourceDir + @"\shader.pak", 358400);

            }
            //if shader.pak <7mb, means the patch has been added
            else
            {
                //do nothing
            }
        }

        //open bump options, a fix for my own shader mod
        public static void ForceChangeSettings(string optionLoc)
        {
            string opt;
            using (StreamReader sr = File.OpenText(optionLoc))
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
            //Trace.Write(opt);
#endif

            using (StreamWriter sw = File.CreateText(optionLoc))
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
                MainWindow.ExtractFile("GOHShaderModdingSupportLauncherWPF.pak.Ori.shader.lzma", main.universalVars.resourceDir + @"\shader.lzma", 358400);
                main.DecompressFileLZMA(main.universalVars.resourceDir + @"\shader.lzma", main.universalVars.resourceDir + @"\shader.pak");
                File.Delete(main.universalVars.resourceDir + @"\shader.lzma");
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

        private void AutoLoadCache()
        {
            if (main.universalVars.NeedAutoLoad == true)
            {
                int len = main.universalVars.modLoaded.Count;
                if (len > 0)
                {
                    for (int i = len - 1; i >= 0; i--)
                    {
                        if (main.universalVars.modLoaded[i].hasShader == true)
                        {
                            string cachePath = main.universalVars.modLoaded[i].path + "/resource/shader/shader_cache/dx11.0";
                            string hashFile = main.universalVars.modLoaded[i].path + "/resource/shader/shader_cache/dx11.0/hash";

                            if (Directory.Exists(cachePath) == true)
                            {
                                if (File.Exists(hashFile) == true)
                                {
                                    string cacheHash = File.ReadAllText(hashFile);
                                    if (cacheHash == main.universalVars.lastCacheHash)
                                    {
                                        //do nothing because cache has been loaded
                                    }
                                    else
                                    {
                                        main.universalVars.lastCacheHash = cacheHash;
                                        main.ClearCacheWork();
                                        Directory.CreateDirectory(main.universalVars.cacheLoc + "/dx11.0/");
                                        foreach (var file in new DirectoryInfo(cachePath).GetFiles())
                                        {
                                            if (file.Name != "hash")
                                            {
                                                file.CopyTo(main.universalVars.cacheLoc + "/dx11.0/" + file.Name);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    main.universalVars.lastCacheHash = "-1";
                                    main.ClearCacheWork();
                                    Directory.CreateDirectory(main.universalVars.cacheLoc + "/dx11.0/");
                                    foreach (var file in new DirectoryInfo(cachePath).GetFiles())
                                    {
                                        if (file.Name != "hash")
                                        {
                                            file.CopyTo(main.universalVars.cacheLoc + "/dx11.0/" + file.Name);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //no cache
                            }

                            //only load cache of last shader mod
                            break;
                        }
                        else
                        {
                            //not a shader mod
                        }
                    }
                }
                else
                {
                    //no mod loaded
                }
            }
            else
            {

            }
        }

        private bool CheckShaderModify()
        {
            if (main.universalVars.NeedCheckShaderModify == true)
            {
                int len = main.universalVars.modLoaded.Count;
                if (len > 0)
                {
                    HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes("SahderCheck"));
                    for (int i = len - 1; i >= 0; i--)
                    {
                        if (main.universalVars.modLoaded[i].hasShader == true)
                        {
                            //no need to check because mod with shader must have resource folder
                            DirectoryInfo resouce = new DirectoryInfo(main.universalVars.modLoaded[i].path + "/resource");
                            

                            //check if shader is in paks
                            foreach (var obj in resouce.GetFiles("*.pak", SearchOption.TopDirectoryOnly))
                            {
                                ZipArchive curPak = ZipFile.Open(obj.FullName, ZipArchiveMode.Read);

                                foreach (var file in curPak.Entries)
                                {
                                    if (file.FullName.Contains("shader/dx10/") == true)
                                    {
                                        byte[] fn = Encoding.UTF8.GetBytes(file.FullName.ToString());
                                        byte[] fl = Encoding.UTF8.GetBytes(file.Length.ToString());
                                        byte[] fw = Encoding.UTF8.GetBytes(file.LastWriteTime.ToString());
                                        hmac.TransformBlock(fn, 0, fn.Length, null, 0);
                                        hmac.TransformBlock(fl, 0, fl.Length, null, 0);
                                        hmac.TransformBlock(fw, 0, fw.Length, null, 0);
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

                                    byte[] fn = Encoding.UTF8.GetBytes(file.FullName.ToString());
                                    byte[] fl = Encoding.UTF8.GetBytes(file.Length.ToString());
                                    byte[] fw = Encoding.UTF8.GetBytes(file.LastWriteTime.ToString());
                                    hmac.TransformBlock(fn, 0, fn.Length, null, 0);
                                    hmac.TransformBlock(fl, 0, fl.Length, null, 0);
                                    hmac.TransformBlock(fw, 0, fw.Length, null, 0);
                                }
                            }

                            


                        }
                        else
                        {
                            //not a shader mod
                        }
                    }

                    //save hash
                    hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    byte[] hash = hmac.Hash;
                    string hashS = Convert.ToBase64String(hash);
                    //default shader -> shader mod
                    if (main.universalVars.lastShaderHash == "0")
                    {
                        main.ClearCacheWork();
                        main.universalVars.lastShaderHash = hashS;
                        return false;
                    }
                    //shader mod -> shader mod
                    else
                    {
                        //no changes
                        if (hashS == main.universalVars.lastShaderHash)
                        {
                            return false;
                        }
                        else
                        {
#if DEBUG
                            Trace.WriteLine("shader modified!");
#endif
                            main.ClearCacheWork();
                            main.universalVars.lastShaderHash = hashS;
                            return true;
                        }
                    }

                }
                else
                {
                    //no mod loaded
                    main.universalVars.lastShaderHash = "0";
                    return false;
                }
            }
            else
            {
                //do not check
                return false;
            }
        }

        private void OnGameExit()
        {
            if (main.universalVars.NeedCompileWarning == true)
            {
                main.CheckCompileWarning();
            }
            if (main.universalVars.NeedRedisplay == true)
            {
                main.RefreshMods();
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
            bool hasModified = CheckShaderModify();
            if (hasModified == false)
            {
                AutoLoadCache();
            }


            if (vars.lm == MainWindow.LauncherVars.LaunchMethod.FileReplace)
            {
                ReplaceFile(main.universalVars.resourceDir);
            }
            ForceChangeSettings(main.universalVars.optionLoc);

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