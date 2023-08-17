using NugetCleaner;
using System.Reflection;
using System.Text;
class Program
{
    static void Main()
    {
        var directoryPath = Directory.GetCurrentDirectory();

        // Get all relevant package files
        (FileInfo[] nupkgs, FileInfo[] snupkgs) packageFiles = GetPackageFiles(directoryPath);

        var nugetPackagesToKeep = FilesToKeep(packageFiles.nupkgs);
        var snugetPackagesToKeep = FilesToKeep(packageFiles.snupkgs);

        DeleteOldVersions(packageFiles.nupkgs, nugetPackagesToKeep);
        DeleteOldVersions(packageFiles.snupkgs, snugetPackagesToKeep);

        Console.WriteLine("Deduplication completed!");
    }

    private static (FileInfo[] nupkgs, FileInfo[] snupkgs)GetPackageFiles(string directoryPath)
    {
        EnumerationOptions options = new EnumerationOptions();
        options.RecurseSubdirectories = true;
        // pull files
        var nupkgFiles = Directory.GetFiles(directoryPath, "*.nupkg", options);
        var snupkgFiles = Directory.GetFiles(directoryPath, "*.snupkg", options);
        // convert to fileinfo array
        FileInfo[] nupkgs = new FileInfo[nupkgFiles.Length];
        for (int i = 0; i < nupkgFiles.Length; i++)
        {
            nupkgs[i] = new FileInfo(nupkgFiles[i]);
        }
        FileInfo[] snupkgs = new FileInfo[snupkgFiles.Length];
        for (int i = 0; i < snupkgFiles.Length; i++)
        {
            snupkgs[i] = new FileInfo(snupkgFiles[i]);
        }
        return (nupkgs, snupkgs);
    }
    private static StringBuilder stringBuider = new StringBuilder();
    private static (string packageName, string packageIdentifier, Version version) GetpackageName(FileInfo file)
    {
        string name = Path.GetFileNameWithoutExtension(file.Name);
        // read version (inverted)
        List<char> versionCharArray = new List<char>();
        for (int i = name.Length - 1; i >= 0; i--)
        {
            char c = name[i];
            if (char.IsDigit(c) || c == '.' || i == name.Length - 1 || c == '-')
            {
                versionCharArray.Add(c);
            }
            else
            {
                if (versionCharArray.Last() == '.')
                {
                    versionCharArray.RemoveAt(versionCharArray.Count - 1);
                }
                break;
            }
        }
        // correct inversion
        versionCharArray.Reverse();
        string versionString = string.Join("", versionCharArray);
        // trim version
        string[] versionParts = versionString.Split('-')[0].Split('.');
        string trimmedVersion = string.Join('.', versionParts.Take(4));
        // convert to version
        Version packageVersion = Version.Parse(trimmedVersion);
        // get packageName
        string packageName = name.Substring(0, (name.Length - versionString.Length)-1);
        // get package version identifier
        stringBuider.Clear();
        stringBuider.Append(packageName);
        stringBuider.Append(packageVersion.Major);
        if (Config.Settings.VersionRevision == "MINOR"
            || Config.Settings.VersionRevision == "BUILD"
            || Config.Settings.VersionRevision == "REVISION")
        {
            stringBuider.Append('.');
            stringBuider.Append(packageVersion.Minor);
        }
        if (Config.Settings.VersionRevision == "BUILD"
            || Config.Settings.VersionRevision == "REVISION")
        {
            stringBuider.Append('.');
            stringBuider.Append(packageVersion.Build);
        }
        string packageIdentifier = stringBuider.ToString();
        return (packageName, packageIdentifier, packageVersion);
    }
    /// <summary>
    /// enumerates which files to keep
    /// </summary>
    /// <param name="packageFiles"></param>
    /// <returns></returns>
    private static HashSet<FileInfo> FilesToKeep(FileInfo[] packageFiles)
    {
        Dictionary<string, Version> packageVersions = new Dictionary<string, Version>();
        Dictionary<string, FileInfo> packages = new Dictionary<string, FileInfo>();

        
        foreach(FileInfo file in packageFiles)
        {
            (string packageName, string packageIdentifier, Version version) packageInfo = GetpackageName(file);
            if (packageVersions.TryGetValue(packageInfo.packageIdentifier, out Version lastVersion))
            {
                if (packageInfo.version > lastVersion)
                {
                    packageVersions[packageInfo.packageIdentifier] = packageInfo.version;
                    packages[packageInfo.packageIdentifier] = file;
                }
            }
            else
            {
                packageVersions[packageInfo.packageIdentifier] = packageInfo.version;
                packages[packageInfo.packageIdentifier] = file;
            }
        }
        return packages.Values.ToHashSet();
    }
    /// <summary>
    /// deletes and reorganizes the old package files
    /// </summary>
    /// <param name="packageFiles"></param>
    /// <param name="packagesToKeep"></param>
    private static void DeleteOldVersions(FileInfo[] packageFiles, HashSet<FileInfo> packagesToKeep)
    {
        foreach (FileInfo oldPackage in packageFiles.Except(packagesToKeep))
        {
            oldPackage.Delete();
            Console.WriteLine($"Deleting {oldPackage}...");
        }
        if (Config.Settings.MoveToSubdirs)
        {
            foreach (FileInfo package in packagesToKeep)
            {
                (string packageName, string packageIdentifier, Version version) packageInfo = GetpackageName(package);
                DirectoryInfo targetDir = new DirectoryInfo(packageInfo.packageName);
                if (!targetDir.Exists)
                {
                    targetDir.Create();
                }
                string targetPath = Path.Combine(targetDir.FullName, package.Name);
                if (package.FullName != targetPath)
                {
                    package.MoveTo(targetPath, overwrite: true);
                }
            }
        }
    }
}
