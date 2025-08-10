using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;


namespace GOHShaderModdingSupportLauncherWPF
{
    public partial class ModManager : Page
    {
        private MainWindow main;

        private MainWindow.ModManagerVars vars;

        private class Mod
        {
            public string name { get; set; }
            public string type { get; set; }
            public bool hasShader { get; set; }
            public string path;
            public string folderName;
            public bool hasLoad;

        public Mod(string n,string t,string p,string fn,bool hS)
            {
                name = n;
                type = t;
                path = p;
                folderName = fn;
                hasShader = hS;
                hasLoad = false;
            }
        };

        //option.set name -> mod
        private Dictionary<string,Mod> modDic;
        private List<Mod> modLoaded;

        //user can not select one row in two data grids
        private struct SelectedRow
        {
            public string dataGrid;
            public List<Mod> mods;

            public SelectedRow(string dg, List<Mod> m){
                dataGrid = dg;
                mods = m;
            }
        }
        private SelectedRow selectedRow;
        private bool hasMod;

        public ModManager()
        {
            main = Application.Current.MainWindow as MainWindow;

            vars = main.modManagerVars;

            InitializeComponent();

            modDic = new Dictionary<string, Mod>();
            modLoaded = new List<Mod>();

            RefreshWork();
        }

        private void RefreshWork()
        {
            modDic.Clear();
            modLoaded.Clear();

            ReadModsFromWorkshop();
            ReadModsFromLocal();

            ReadLoadedMods();

            UpdateUI();
        }

        private string getModShowName(FileInfo[] modInfo)
        {
            string name;
            using (StreamReader info = modInfo[0].OpenText())
            {
                string nameLine;
                //push to name line
                while ((nameLine = info.ReadLine()).Contains("{name") == false) ;

                int commentIndex = nameLine.IndexOf(";");
                nameLine = nameLine.Substring(0, commentIndex == -1 ? nameLine.Length : commentIndex);

                int nameS = nameLine.IndexOf('"') + 1;
                name = nameLine.Substring(nameS, nameLine.LastIndexOf('"') - nameS);
#if DEBUG
                Trace.WriteLine("mod show name= " + name);
#endif

                info.Close();
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
                foreach(var obj in resouce.GetFiles("*.pak",SearchOption.TopDirectoryOnly))
                {
#if DEBUG
                    Trace.WriteLine("get a pak= "+obj.Name);
#endif
                    ZipArchive curPak = ZipFile.Open(obj.FullName, ZipArchiveMode.Read);

                    foreach(var file in curPak.Entries)
                    {
                        if (file.FullName.Contains("shader/") == true)
                        {
                            if (file.Length > 0)
                            {
                                return true;
                            }
                        }
                    }

                }

                string shaderFolder = resouce.FullName + "/shader";
                //check if shader is in dir
                if (Directory.Exists(shaderFolder)==true)
                {
                    DirectoryInfo shader = new DirectoryInfo(shaderFolder);
                    foreach (FileInfo file in shader.GetFiles("*",SearchOption.AllDirectories))
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
            foreach(var dir in vars.workshopDir.GetDirectories())
            {
                FileInfo[] modInfo = dir.GetFiles("mod.info",SearchOption.TopDirectoryOnly);
                string folderName = "mod_" + dir.Name;

                if (modInfo.Length==0)
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


                Mod single = new Mod(name, "Workshop", dir.FullName, folderName, hasShader);

                modDic.Add(folderName, single);
            }
        }

        private void ReadModsFromLocal()
        {
            foreach (var dir in vars.localDir.GetDirectories())
            {
                FileInfo[] modInfo = dir.GetFiles("mod.info", SearchOption.TopDirectoryOnly);
                string folderName = dir.Name.ToLower();

                if (modInfo.Length == 0)
                {
                    //not a mod
                    continue;
                }
                else if (folderName.Contains(' ')==true)
                {
                    //is a mod, but not valid
                    continue;
                }
                string name = getModShowName(modInfo);

                bool hasShader = checkShader(dir);

                

                Mod single = new Mod(name, "Local", dir.FullName, folderName, hasShader);

                modDic.Add(folderName, single);
            }
        }

        private void ReadLoadedMods()
        {
            using (StreamReader opt = File.OpenText(main.universalVars.optionLoc))
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

                while ((opt.ReadLine() is var line)&& line.Contains("}") == false)
                {
#if DEBUG
                    Trace.WriteLine(line);
#endif
                    int nameS = line.IndexOf("\"")+1;
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
                    modDic[modName].hasLoad = true;
                    modLoaded.Add(modDic[modName]);

                }

                opt.Close();
            }
        }
        private void UpdateUI()
        {
            hasMod = true;
            loadedMods.Items.Clear();
            unloadedMods.Items.Clear();
            foreach(var mod in modDic.Values)
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

            foreach(var mod in modLoaded)
            {
                loadedMods.Items.Add(mod);
            }

            if (loadedMods.Items.Count != 0)
            {
                loadedMods.SelectedIndex = 0;
                selectedRow = new SelectedRow("loadedMods",loadedMods.SelectedItems.Cast<Mod>().ToList());

            }
            else if (unloadedMods.Items.Count!=0)
            {
                unloadedMods.SelectedIndex = 0;
                selectedRow = new SelectedRow("unloadedMods", unloadedMods.SelectedItems.Cast<Mod>().ToList());
            }
            else
            {
                //no mod!
                selectedRow = new SelectedRow();
                hasMod = false;
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

            foreach(Mod mod in loadedMods.Items)
            {
                modified += "\t\t\""+mod.folderName+ ":0\"\r\n";
            }

            modified += "\t}\r\n}\r\n";

            File.WriteAllText(main.universalVars.optionLoc, modified);
        }

        private void DataGridAddRange(DataGrid dg,List<Mod> mods)
        {
            foreach(var mod in mods)
            {
                dg.Items.Add(mod);
            }
        }

        private void DataGridRemoveRange(DataGrid dg, List<Mod> mods)
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
                DataGridAddRange(loadedMods,selectedRow.mods);
                DataGridRemoveRange(unloadedMods, selectedRow.mods);

                foreach(var mod in selectedRow.mods)
                {
                    mod.hasLoad = true;
                    modLoaded.Add(mod);
                }
            }

            UpdateOptionFIle();
        }

        private void unloadMod_Click(object sender, RoutedEventArgs e)
        {
            if (selectedRow.dataGrid == "loadedMods")
            {
                DataGridAddRange(unloadedMods, selectedRow.mods);
                DataGridRemoveRange(loadedMods, selectedRow.mods);

                foreach (var mod in selectedRow.mods)
                {
                    mod.hasLoad = false;
                    modLoaded.Remove(mod);
                }
            }
            else
            {
                //do nothing
            }
            UpdateOptionFIle();
        }

        private void openModFolder_Click(object sender, RoutedEventArgs e)
        {
            if (hasMod == true)
            {
                Process.Start("explorer.exe", selectedRow.mods[0].path);
            }
            
        }

        private void loadShaderCache_Click(object sender, RoutedEventArgs e)
        {
            main.ClearCacheWork();

        }

        private bool isSyncingSelection = false;

        private void unloadedMods_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isSyncingSelection) return;
            try
            {
                isSyncingSelection = true;
                selectedRow = new SelectedRow("unloadedMods", unloadedMods.SelectedItems.Cast<Mod>().ToList());
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
                selectedRow = new SelectedRow("loadedMods", loadedMods.SelectedItems.Cast<Mod>().ToList());
                unloadedMods.SelectedIndex = -1;
            }
            finally
            {
                isSyncingSelection = false;
            }
        }

        private void refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshWork();
        }

        private Point startPoint;
        private Mod draggedItem;

        //mouse down
        private void loadedMods_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);

            draggedItem = loadedMods.SelectedItem as Mod;
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

            int newIndex = loadedMods.Items.IndexOf((Mod)target);

            loadedMods.Items.Remove(draggedItem);
            loadedMods.Items.Insert(newIndex, draggedItem);

            UpdateOptionFIle();

            draggedItem = null;
        }
    }
}