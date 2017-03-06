using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
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

                File.WriteAllText(configurationFilePath,
@"<configuration>
    <copyTo>C:\Users\sterpa1\Desktop\Erik Paldanius</copyTo>
    <removeEmptyDirectories>true</removeEmptyDirectories>
    
	<!-- Files matching any of the following patterns will be deleted unless they match any of the do not delete patterns below. -->
    <deletePatterns>
        <pattern>\.(db|exe|exp|sdf|idb|pdb|ipdb|ilk|ipch|opendb|suo|pch|lib|log|tlog|lastbuildstate|cache|tmp|temp|obj|iobj|gitignore|gitattributes)$</pattern>        <pattern>\.exe$</pattern>
        <pattern>exposedScriptFunctions.txt$</pattern>
        <pattern>.[^\\/]+ Bin[\\/](logs|\.vs|PBS)[\\/].+</pattern>
        <pattern>\.git[\\/].+</pattern>
    </deletePatterns>
	
	<!-- Files matching any of the following patterns will not be deleted. -->
	<doNotDeletePatterns>
        <pattern>extlibs[\\/].+\.lib$</pattern>
        <pattern>^TGA2D/Lib/(freetype_Debug_x64|freetype_Release_x64|avcodec|avdevice|avfilter|avformat|avutil|postproc|swresample|swscale).lib$</pattern>
        <pattern>.[^\\/]+ Bin[\\/]Application \(([Dd]ebug|[Rr]elease)\).exe</pattern>
	</doNotDeletePatterns>
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

            // try
            // {
                int filesRemoved = 0,
                    filesCopied = 0;
                var watch = Stopwatch.StartNew();

                Configuration configuration = (Configuration)new XmlSerializer(typeof(Configuration)).Deserialize(new FileStream(configurationFilePath, FileMode.OpenOrCreate));

                List<Regex> removePatterns = new List<Regex>();
                List<Regex> doNotRemovePatterns = new List<Regex>();

                foreach (var remove in configuration.DeletePatterns)
                {
                    removePatterns.Add(new Regex(remove, RegexOptions.IgnoreCase));
                }
                foreach (var doNotRemove in configuration.DoNotDeletePatterns)
                {
                    doNotRemovePatterns.Add(new Regex(doNotRemove, RegexOptions.IgnoreCase));
                }
                
                if (configuration.CopyTo != args[0])
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
                    
                    Uri copyToUri = new Uri(configuration.CopyTo + "/");

                    DirectoryCopy(args[0], configuration.CopyTo, true, file =>
                    {
                        string str = copyToUri.MakeRelativeUri(new Uri(file)).OriginalString;
                        
                        bool delete = removePatterns.Any(o => o.IsMatch(str));
                        bool doNotDelete = doNotRemovePatterns.Any(o => o.IsMatch(str));

                        if (delete && doNotDelete == false)
                        {
                            filesRemoved++;
                            filesCopied++;
                            return false;
                        }

                        return true;
                    });
                }
                
                Console.Clear();
                Console.WriteLine("Cleaning finised in {0:N2} seconds.\n{1} files were copied\n{2} files were not copied.", watch.Elapsed.TotalSeconds, filesCopied, filesRemoved);
                Thread.Sleep(3000);
            // }
            // catch (Exception e)
            // {
            //     Console.WriteLine("Cleaning failed:\n{0}", e.Message);
            //     Console.ReadKey();
            // }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, Func<string, bool> predicate)
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

            bool createdDirectory = false;
            
            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string fullPath = Path.Combine(destDirName, file.Name);
                if (predicate(fullPath))
                {
                    if (createdDirectory == false)
                    {
                        // If the destination directory doesn't exist, create it.
                        if (!Directory.Exists(destDirName))
                        {
                            Directory.CreateDirectory(destDirName);
                        }

                        createdDirectory = true;
                    }
                    file.CopyTo(fullPath, false);
                }
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, true, predicate);
                }
            }
        }

    }
}
