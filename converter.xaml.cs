using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace GOHShaderModdingSupportLauncherWPF
{
    public partial class Converter : Page
    {
        private string targetPath;

        private Wpf.Ui.Controls.TextBox TB_ConvertPath;
        public Converter()
        {
            InitializeComponent();

            TB_ConvertPath = this.FindName("convertPath") as Wpf.Ui.Controls.TextBox;

        }

        private void convertPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            targetPath = TB_ConvertPath.Text;
        }

        private void viewPath_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            bool? result=dialog.ShowDialog();

            if (result == true)
            {
                targetPath = dialog.FolderName;
                TB_ConvertPath.Text = targetPath;
            }
        }

        private bool EnableEnvInMTLFile(ref string mtl)
        {
            mtl.Replace("{material simple", "{material bump");

            if(mtl.Contains("{material bump") == true)
            {
                //a special vanilla goh material,or have enabled environment map
                if(mtl.Contains("{height")|| mtl.Contains("{lightmap"))
                {
                    return false;
                }

                int lastBracket = mtl.LastIndexOf('}');
                mtl=mtl.Insert(lastBracket, "\t{height \"$/envmap/env\"}\r\n\t{lightmap \"$/dummyTex/white\"}\r\n\t{parallax_scale 1000}\r\n");
#if DEBUG
                Trace.WriteLine("modified mtl=" + mtl);
#endif


                return true;
            }
            else
            {
                return false;
            }
        }

        private void enableEnvMaps_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(targetPath) == true)
            {
                console.Clear();
                //no need to check because mod with shader must have resource folder
                DirectoryInfo target = new DirectoryInfo(targetPath);


                //check if mtl is in paks
                foreach (var obj in target.GetFiles("*.pak", SearchOption.AllDirectories))
                {
                    ZipArchive curPak = ZipFile.Open(obj.FullName, ZipArchiveMode.Update);

                    //path,content
                    Dictionary<string, string> mtls = new Dictionary<string, string>();
                    //read and modify
                    foreach (var file in curPak.Entries)
                    {
                        if (file.Name.EndsWith(".mtl") == true)
                        {
                            
                            using (var reader= new StreamReader(file.Open()))
                            {
                                string mtl= reader.ReadToEnd();

                                bool needModify=EnableEnvInMTLFile(ref mtl);

                                if (needModify == true)
                                {
                                    console.AppendText("[" + DateTime.Now + "] A .mtl file in " + obj.Name + " with path " + file.FullName + " has been updated.\n");
                                    mtls.Add(file.FullName, mtl);
                                }
                                reader.Close();
                            }
                        }
                    }
                    
                    //write
                    foreach(var mtl in mtls)
                    {
                        var ori = curPak.GetEntry(mtl.Key);
                        ori?.Delete();

                        using (var writer = new StreamWriter(curPak.CreateEntry(mtl.Key).Open()))
                        {
                            writer.Write(mtl.Value);
                            writer.Close();
                        }
                    }

                    curPak.Dispose();

                }

                //normal mtl
                foreach (FileInfo file in target.GetFiles("*.mtl", SearchOption.AllDirectories))
                {
                    string mtl = File.ReadAllText(file.FullName);
                    bool needModify = EnableEnvInMTLFile(ref mtl);

                    if (needModify == true)
                    {
                        console.AppendText("["+DateTime.Now+"] A .mtl file with path " + file.FullName + " has been updated.\n");
                        File.WriteAllText(file.FullName, mtl);
                    }
                    
                }


                MessageBox.Show("mtl convert complete!", "Notice");

            }
            else
            {
                //path not found
                MessageBox.Show("Path not found!", "Warning");
            }
        }
    }
}