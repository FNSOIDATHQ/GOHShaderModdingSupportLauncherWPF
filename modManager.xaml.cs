using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GOHShaderModdingSupportLauncherWPF.Properties;


namespace GOHShaderModdingSupportLauncherWPF
{
    public partial class ModManager : Page
    {
        private MainWindow main;

        private MainWindow.ModManagerVars vars;

        private bool hasInit=false;

        //user can not select one row in two data grids
        private struct SelectedRow
        {
            public string dataGrid;
            public List<MainWindow.Mod> mods;

            public SelectedRow(string dg, List<MainWindow.Mod> m){
                dataGrid = dg;
                mods = m;
            }
        }
        private SelectedRow selectedRow;

        public ModManager()
        {
            main = Application.Current.MainWindow as MainWindow;

            vars = main.modManagerVars;

            InitializeComponent();

            main.RefreshMods();
            UpdateUI();

            hasInit = true;
        }

        private void UpdateUI()
        {
            vars.hasMod = true;
            loadedMods.Items.Clear();
            unloadedMods.Items.Clear();
            foreach(var mod in main.universalVars.modDic.Values)
            {
                if (mod.hasLoad == true)
                {
                    //do nothing, because we must maintain order of loaded mods
                }
                else
                {
                    unloadedMods.Items.Add(mod);
                }
            }

            foreach(var mod in main.universalVars.modLoaded)
            {
                loadedMods.Items.Add(mod);
            }

            if (loadedMods.Items.Count != 0)
            {
                loadedMods.SelectedIndex = 0;
                selectedRow = new SelectedRow("loadedMods",loadedMods.SelectedItems.Cast<MainWindow.Mod>().ToList());

            }
            else if (unloadedMods.Items.Count!=0)
            {
                unloadedMods.SelectedIndex = 0;
                selectedRow = new SelectedRow("unloadedMods", unloadedMods.SelectedItems.Cast<MainWindow.Mod>().ToList());
            }
            else
            {
                //no mod!
                selectedRow = new SelectedRow();
                vars.hasMod = false;
            }
            
        }

        private void UpdateOptionFIle()
        {
            string modified = "";
            using (StreamReader opt = File.OpenText(main.universalVars.optionLoc))
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
                    modified += line+"\r\n";
                    if (line.Contains("{mods") == true)
                    {
                        break;
                    }

                }

                opt.Close();
            }

            foreach(MainWindow.Mod mod in loadedMods.Items)
            {
                modified += "\t\t\""+mod.folderName+ ":0\"\r\n";
            }

            modified += "\t}\r\n}\r\n";

            File.WriteAllText(main.universalVars.optionLoc, modified);
        }

        private void DataGridAddRange(DataGrid dg,List<MainWindow.Mod> mods)
        {
            foreach(var mod in mods)
            {
                dg.Items.Add(mod);
            }
        }

        private void DataGridRemoveRange(DataGrid dg, List<MainWindow.Mod> mods)
        {
            foreach (var mod in mods)
            {
                dg.Items.Remove(mod);
            }
        }

        private void loadMod_Click(object sender, RoutedEventArgs e)
        {
            if (selectedRow.dataGrid == "loadedMods")
            {
                //do nothing
            }
            else
            {
                foreach (var mod in selectedRow.mods)
                {
                    mod.hasLoad = true;
                    main.universalVars.modLoaded.Add(mod);
                }

                DataGridAddRange(loadedMods,selectedRow.mods);
                DataGridRemoveRange(unloadedMods, selectedRow.mods);
            }

            UpdateOptionFIle();
        }

        private void unloadMod_Click(object sender, RoutedEventArgs e)
        {
            if (selectedRow.dataGrid == "loadedMods")
            {
                foreach (var mod in selectedRow.mods)
                {
                    mod.hasLoad = false;
#if DEBUG
                    //Trace.WriteLine("modloaded[0]==selectedRow.mods[0] ? " + object.ReferenceEquals(mod, main.universalVars.modLoaded[0]));
#endif
                    main.universalVars.modLoaded.Remove(mod);
                }

                DataGridAddRange(unloadedMods, selectedRow.mods);
                DataGridRemoveRange(loadedMods, selectedRow.mods);
            }
            else
            {
                //do nothing
            }
            UpdateOptionFIle();
        }

        private void openModFolder_Click(object sender, RoutedEventArgs e)
        {
            if (vars.hasMod == true)
            {
                Process.Start("explorer.exe", selectedRow.mods[0].path);
            }
            
        }

        private void loadShaderCache_Click(object sender, RoutedEventArgs e)
        {

            string cachePath = selectedRow.mods[0].path + "/resource/shader/shader_cache/dx11.0";
            string hashFile = selectedRow.mods[0].path + "/resource/shader/shader_cache/dx11.0/hash";

            if (Directory.Exists(cachePath) == true)
            {
                if (File.Exists(hashFile) == true)
                {
                    string cacheHash = File.ReadAllText(hashFile);
                    if (cacheHash == main.universalVars.lastCacheHash)
                    {
                        //do nothing because cache has been loaded
                        MessageBox.Show(i18n.M_CacheHasBeenLoaded, i18n.Universal_Notice, MessageBoxButton.OK, MessageBoxImage.Information);
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
                        MessageBox.Show(i18n.M_LoadCacheSuccessful, i18n.Universal_Notice, MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show(i18n.M_NoCacheToLoad, i18n.Universal_Notice, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void collectShaderCache_Click(object sender, RoutedEventArgs e)
        {
            if (selectedRow.mods[0].type == "Local")
            {
                string modCachePath = selectedRow.mods[0].path + "/resource/shader/shader_cache/dx11.0";
                string gameCachePath = main.universalVars.cacheLoc + "/dx11.0";

                HMACSHA512 hmac = new HMACSHA512(Encoding.UTF8.GetBytes("CacheCheck"));

                
                if (Directory.Exists(gameCachePath)==true)
                {
                    List<FileInfo> files = new DirectoryInfo(gameCachePath).GetFiles("*", SearchOption.TopDirectoryOnly).OrderBy(p => p.FullName).ToList();
                    if (files.Count > 0)
                    {
                        if (Directory.Exists(modCachePath) == true)
                        {
                            Directory.Delete(modCachePath, true);
                        }
                        Directory.CreateDirectory(modCachePath);

                        for (int i = 0; i < files.Count && (files[i] is var file); i++)
                        {
                            byte[] curF = File.ReadAllBytes(file.FullName);
                            File.WriteAllBytes(modCachePath + "/" + file.Name, curF);
                            if (i == files.Count - 1)
                            {
                                hmac.TransformFinalBlock(curF, 0, curF.Length);
                            }
                            else
                            {
                                hmac.TransformBlock(curF, 0, curF.Length, curF, 0);
                            }
                        }
                        

                        byte[] hash = hmac.Hash;

                        File.WriteAllText(modCachePath + "/hash", Convert.ToBase64String(hash));
                        MessageBox.Show(i18n.M_CollectCacheSuccessful, i18n.Universal_Notice, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(i18n.M_NoCacheToCollect, i18n.Universal_Notice, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show(i18n.M_NoCacheToCollect, i18n.Universal_Notice, MessageBoxButton.OK, MessageBoxImage.Information);
                }

            }
            else
            {
                MessageBox.Show(i18n.M_TryCollectWorkshopMod, i18n.Universal_Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            
        }

        private bool isSyncingSelection = false;

        private void unloadedMods_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isSyncingSelection) return;
            try
            {
                isSyncingSelection = true;
                selectedRow = new SelectedRow("unloadedMods", unloadedMods.SelectedItems.Cast<MainWindow.Mod>().ToList());
                loadedMods.SelectedIndex = -1;
            }
            finally
            {
                isSyncingSelection = false;
            }
        }

        private void loadedMods_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isSyncingSelection) return;
            try
            {
                isSyncingSelection = true;
                selectedRow = new SelectedRow("loadedMods", loadedMods.SelectedItems.Cast<MainWindow.Mod>().ToList());
                unloadedMods.SelectedIndex = -1;
            }
            finally
            {
                isSyncingSelection = false;
            }
        }

        private void refresh_Click(object sender, RoutedEventArgs e)
        {
            main.RefreshMods();
            UpdateUI();
        }

        private Point startPoint;
        private MainWindow.Mod draggedItem;

        //mouse down
        private void loadedMods_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);

            draggedItem = loadedMods.SelectedItem as MainWindow.Mod;
        }

        //start drag
        private void loadedMods_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && draggedItem != null)
            {
                var pos = e.GetPosition(null);
                if (Math.Abs(pos.X - startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(pos.Y - startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    DragDrop.DoDragDrop(loadedMods, draggedItem, DragDropEffects.Move);
                }
            }
        }

        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }

        private object GetDataGridRowItemAt(Point position)
        {
            var hitTest = VisualTreeHelper.HitTest(loadedMods, position);
            if (hitTest == null) return null;

            var row = FindVisualParent<DataGridRow>(hitTest.VisualHit);
            return row?.Item;
        }

        private void loadedMods_Drop(object sender, DragEventArgs e)
        {
            if (draggedItem == null) return;

            var target = GetDataGridRowItemAt(e.GetPosition(loadedMods));
            if (target == null || target == draggedItem) return;

            int newIndex = loadedMods.Items.IndexOf((MainWindow.Mod)target);

            loadedMods.Items.Remove(draggedItem);
            loadedMods.Items.Insert(newIndex, draggedItem);

            UpdateOptionFIle();

            draggedItem = null;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (hasInit == true)
            {
#if DEBUG
                Trace.WriteLine("mod manager page loaded!");
#endif

                //this should work when game exit and mod data instances have been refreshed
                UpdateUI();

            }
        }
    }
}