using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ShortcutFixerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        public string InputPath { get; set; }
        public static SolidColorBrush NormalBrush = Brushes.Black;
        public static SolidColorBrush ImportantBrush = Brushes.Red;
        public void WriteLog(string inp, bool isImportant = false)
        {
            Dispatcher.InvokeAsync(() =>
            {
                var para = new Paragraph();
                para.Inlines.Add(new Run() { Text = inp, Foreground = isImportant ? ImportantBrush : NormalBrush });
                OutputTB.Document.Blocks.Add(para);
                OutputTB.ScrollToEnd();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var diag = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                EnsurePathExists = true
            };
            if (diag.ShowDialog(this) == CommonFileDialogResult.Ok)
            {
                InputPath = diag.FileName;
                pathViewer.Text = InputPath;
                OutputTB.Document.Blocks.Clear();
                WriteLog($@"Set Path to {InputPath}", true);
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                var dir = new DirectoryInfo(InputPath);
                WriteLog("Searching Folder ...");

                var allEntries = GenerateAccessibleList(dir);
                try
                {
                    var toRemove = allEntries
                    .Where(file =>
                    {
                        try
                        {
                            if (CheckIfDir(file.Attributes))
                                return false;
                            var ext = Path.GetExtension(file.FullName);
                            return ext == ".vbs" || ext == ".lnk";
                        }
                        catch
                        {
                            return false;
                        }
                    });
                    WriteLog("Removing virus files ...");
                    foreach (var item in toRemove)
                    {
                        try
                        {
                            item.Delete();
                            WriteLog($"Deleted {item.FullName}");
                        }
                        catch
                        {
                            WriteLog($"Couldn't delete {item.FullName}", true);
                        }
                    }

                    WriteLog("Restoring original files ...");
                    foreach (var item in allEntries)
                    {
                        try
                        {
                            var oldAttr = item.Attributes;
                            var newAttr = (~FileAttributes.System & ~FileAttributes.ReadOnly & ~FileAttributes.Hidden);
                            if ((oldAttr & newAttr) == newAttr)
                            {
                                continue;
                            }
                            else
                            {
                                item.Attributes &= newAttr;
                                WriteLog($"Restored {item.FullName}");
                            }                            
                        }
                        catch
                        {
                            WriteLog($"Couldn't set attributes to item {item.FullName}", true);
                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    WriteLog("Please Start the application as admin", true);
                }
                catch (Exception ex)
                {
                    WriteLog(ex.ToString(), true);
                }
                WriteLog("Done!",true);
            });
        }
        private static List<FileSystemInfo> GenerateAccessibleList(DirectoryInfo root, DirectoryInfo dir = null, Func<FileSystemInfo, bool> condition = null)
        {
            var fils = new List<FileSystemInfo>();
            IEnumerable<FileInfo> subFiles;
            if (dir == null)
            {
                dir = root;
            }
            try
            {
                subFiles = dir.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException)
            {
                return new List<FileSystemInfo>();
            }
            if (dir != root)
            {
                fils.Add(dir);
            }
            if (condition == null)
            {
                fils.AddRange(subFiles);
            }
            else
            {
                fils.AddRange(subFiles.Where(condition));
            }
            var SubDirectories = dir.EnumerateDirectories();
            foreach (var subDir in SubDirectories) // add each file in directory
            {
                fils.AddRange(GenerateAccessibleList(root, subDir));
            }
            return fils; // return file list
        }
        private static bool CheckIfDir(FileAttributes attr)
        {
            return (attr & FileAttributes.Directory) == FileAttributes.Directory;
        }
    }
}
