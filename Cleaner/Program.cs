using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Cleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            var configurationFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "clean_configuration.xml");
            if (File.Exists(configurationFilePath) == false)
            {
                Console.WriteLine("Generating configuration...");

                File.WriteAllText(configurationFilePath, @"

<configuration>
    <copyDirectory>true</copyDirectory>
    <copyTo>" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), UserPrincipal.Current.DisplayName) + @"</copyTo>
    <removeEmptyDirectories>true</removeEmptyDirectories>
    
    <extensionsToDelete>
        <extension>.db</extension>
        <extension>.sdf</extension>
        <extension>.idb</extension>
        <extension>.pdb</extension>
        <extension>.ipdb</extension>
        <extension>.ilk</extension>
        <extension>.ipch</extension>
        <extension>.opendb</extension>
        <extension>.suo</extension>
        <extension>.pch</extension>
        <extension>.lib</extension>
        <extension>.log</extension>
        <extension>.tlog</extension>
        <extension>.lastbuildstate</extension>
        <extension>.cache</extension>
        <extension>.tmp</extension>
        <extension>.temp</extension>
        <extension>.obj</extension>
        <extension>.iobj</extension>
    </extensionsToDelete>
</configuration>

");
            }

            if (args.Length == 0)
            {
                Console.WriteLine("No Input Directory");
                Console.ReadKey();
                return;
            }

            if (Directory.Exists(args[0]) == false)
            {
                Console.WriteLine("Input Directory Does Not Exist");
                Console.ReadKey();
                return;
            }

            try
            {
                int directoriesRemoved = 0,
                    filesRemoved = 0;
                var watch = Stopwatch.StartNew();

                Configuration configuration = new XmlSerializer(typeof(Configuration)).Deserialize(new FileStream(configurationFilePath, FileMode.OpenOrCreate)) as Configuration;

                var directoryToClean = args[0];

                if (configuration.CopyDirectory && configuration.CopyTo != args[0])
                {
                    Console.WriteLine("Copying directory...");
                    if (Directory.Exists(configuration.CopyTo))
                    {
                        Console.WriteLine("Target directory already exists, do you want to remove it? (Y/N)");

                        if (Console.ReadKey().Key == ConsoleKey.Y)
                        {
                            Directory.Delete(configuration.CopyTo, true);
                        }
                        else
                        {
                            return;
                        }
                    }
                    DirectoryCopy(args[0], configuration.CopyTo, true);
                    directoryToClean = configuration.CopyTo;
                }

                var cleanUri = new Uri(directoryToClean);

                List<string> filesToRemove = Directory.EnumerateFiles(directoryToClean, "*", SearchOption.AllDirectories).Where(o => configuration.ExtensionsToDelete.Contains(Path.GetExtension(o))).ToList();
                HashSet<string> directoriesToCheck = new HashSet<string>();

                foreach (var file in filesToRemove)
                {
                    var path = Path.Combine(directoryToClean, file);
                    File.Delete(path);
                    filesRemoved++;

                    Console.WriteLine("Removed file {0}", file);

                    if (configuration.RemoveEmptyDirectories)
                    {
                        var directory = Path.GetDirectoryName(path);

                        while (new Uri(directory).MakeRelativeUri(cleanUri).OriginalString.Length > 0)
                        {
                            directoriesToCheck.Add(directory);
                            directory = Directory.GetParent(directory).FullName;
                        }
                    }
                }

                int count = 0;

                do
                {
                    count = 0;

                    foreach (var directory in directoriesToCheck)
                    {
                        if (Directory.Exists(directory) == false)
                            continue;

                        if (Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).FirstOrDefault() == null && Directory.EnumerateDirectories(directory, "*", SearchOption.AllDirectories).FirstOrDefault() == null)
                        {
                            count++;
                            Directory.Delete(directory);
                            directoriesRemoved++;
                            Console.WriteLine("Removed empty directory {0}", directory);
                        }
                    }

                } while (count > 0);

                Console.Clear();
                Console.WriteLine("Cleaning finised in {0} seconds.\nRemoved {1} files and {2} directories.", watch.Elapsed.TotalSeconds.ToString("N2"), filesRemoved, directoriesRemoved);
                Thread.Sleep(3000);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cleaning failed:\n{0}", e.Message);
                Console.ReadKey();
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

    }
}
