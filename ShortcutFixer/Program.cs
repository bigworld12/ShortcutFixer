using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShortcutFixer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Please enter the USB drive letter : ");
            var letter = Console.ReadLine()[0];
            var path = $@"{letter.ToString().ToUpper()}:\";
            DirectoryInfo dir;
            try
            {
                dir = new DirectoryInfo(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Directory not valid !!!");
                Console.WriteLine(ex.ToString());
                return;
            }

            Console.WriteLine("Searching Folder ...");
            try
            {
                var allEntries = GenerateAccessibleList(dir);
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
                Console.WriteLine("Removing virus files ...");
                foreach (var item in toRemove)
                {
                    try
                    {
                        item.Delete();
                    }
                    catch
                    {
                        Console.WriteLine($"Couldn't delete {item}");
                    }
                }
                Console.WriteLine("Restoring original files ...");
                foreach (var item in allEntries)
                {
                    try
                    {
                        item.Attributes &= (~FileAttributes.System & ~FileAttributes.ReadOnly & ~FileAttributes.Hidden);
                    }
                    catch
                    {
                        Console.WriteLine($"Couldn't set attributes to item {item.FullName}");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("Please Start the application as admin");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("Done! Press Any key to close.");
            Console.ReadKey();
        }
        private static List<FileSystemInfo> GenerateAccessibleList(DirectoryInfo root, DirectoryInfo dir = null, Func<FileSystemInfo, bool> condition = null)
        {
            var fils = new List<FileSystemInfo>();
            IEnumerable<FileInfo> subFiles;
            if (dir == null)
            {
                dir = root;
            }
            if (string.IsNullOrWhiteSpace(dir.Name))
            {
                dir.Attributes &= (~FileAttributes.System & ~FileAttributes.ReadOnly & ~FileAttributes.Hidden);
                var RenameRes = RenameDirectoryWithTrailingWhiteSpace(dir, "oldFiles");
                dir.Refresh();
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


        [DllImport("kernel32.dll", EntryPoint = "MoveFileW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool MoveFile(string lpExistingFileName, string lpNewFileName);

        static bool? RenameDirectoryWithTrailingWhiteSpace(DirectoryInfo di, string newName)
        {
            if (Regex.IsMatch(di.Name, @"\s+$"))
            {
                var oldPath = "\\\\?\\" + di.FullName;
                var newPath = newName;
                var result = MoveFile(oldPath, newPath);
                return result;
            }
            return null;
        }

    }
}
