using Cupscale.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cupscale
{
  internal class IoUtils
  {
    public static string[] compatibleExtensions = new string[] { ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".webp", ".dds", ".gif" };
    public static string[] videoExtensions = new string[] { ".mp4", ".m4v", ".mkv", ".webm", ".gif", ".avi" };
    public static bool hasShownPortableInfo = false;

    public static bool IsPortable()
    {
      foreach (string arg in Environment.GetCommandLineArgs())
      {
        if (arg == "-appdata")
          return false;
      }

      return true;
    }

    public static string[] ReadLines(string path)
    {
      List<string> lines = new List<string>();
      using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0x1000, FileOptions.SequentialScan))
      using (var sr = new StreamReader(fs, Encoding.UTF8))
      {
        string line;
        while ((line = sr.ReadLine()) != null)
        {
          lines.Add(line);
        }
      }
      return lines.ToArray();
    }

    public static bool IsPathDirectory(string path)
    {
      if (path == null)
      {
        throw new ArgumentNullException("path");
      }
      path = path.Trim();
      if (Directory.Exists(path))
      {
        return true;
      }
      if (File.Exists(path))
      {
        return false;
      }
      if (new string[2]
      {
                "\\",
                "/"
      }.Any((string x) => path.EndsWith(x)))
      {
        return true;
      }
      return string.IsNullOrWhiteSpace(Path.GetExtension(path));
    }

    public static bool IsFileValid(string path)
    {
      if (path == null)
        return false;

      if (!File.Exists(path))
        return false;

      return true;
    }

    public static bool IsDirValid(string path)
    {
      if (path == null)
        return false;

      if (!Directory.Exists(path))
        return false;

      return true;
    }

    public static bool IsPathValid(string path)
    {
      if (path == null)
        return false;

      if (IsPathDirectory(path))
        return IsDirValid(path);
      else
        return IsFileValid(path);
    }

    public static async Task CopyDir(string sourceDir, string targetDir, string wildcard = "*", bool move = false, bool onlyCompatibles = false, string removeFromName = "")
    {
      Logger.Log("[IOUtils] Copying directory \"" + sourceDir + "\" to \"" + targetDir + "\" (Move: " + move + " - RemoveFromName: " + removeFromName + ")");
      Directory.CreateDirectory(targetDir);
      DirectoryInfo source = new DirectoryInfo(sourceDir);
      DirectoryInfo target = new DirectoryInfo(targetDir);
      Stopwatch sw = new Stopwatch();
      sw.Restart();
      await CopyWork(source, target, wildcard, move, onlyCompatibles, removeFromName, sw);
    }

    private static async Task CopyWork(DirectoryInfo source, DirectoryInfo target, string wildcard, bool move, bool onlyCompatibles, string removeFromName, Stopwatch sw)
    {
      DirectoryInfo[] directories = source.GetDirectories();

      foreach (DirectoryInfo directoryInfo in directories)
        await CopyWork(directoryInfo, target.CreateSubdirectory(directoryInfo.Name), wildcard, move, onlyCompatibles, removeFromName, sw);

      FileInfo[] files = source.GetFiles(wildcard);

      foreach (FileInfo fileInfo in files)
      {
        if (sw.ElapsedMilliseconds > 100)
        {
          await Task.Delay(1);
          sw.Restart();
        }

        if (onlyCompatibles && !compatibleExtensions.Contains(fileInfo.Extension.ToLower()))
          continue;

        string targetPath = Path.Combine(target.FullName, fileInfo.Name);

        if (move)
          fileInfo.MoveTo(targetPath);
        else
          fileInfo.CopyTo(targetPath, overwrite: true);
      }
    }

    public static void ClearDir(string path)
    {
      if (!Directory.Exists(path))
        return;
      if (Logger.doLogIo) Logger.Log("[IOUtils] Clearing " + path);
      try
      {
        DirectoryInfo directoryInfo = new DirectoryInfo(path);
        FileInfo[] files = directoryInfo.GetFiles();
        foreach (FileInfo fileInfo in files)
        {
          fileInfo.Delete();
        }
        DirectoryInfo[] directories = directoryInfo.GetDirectories();
        foreach (DirectoryInfo directoryInfo2 in directories)
        {
          directoryInfo2.Delete(recursive: true);
        }
      }
      catch (Exception e)
      {
        Logger.Log($"Failed to clear {path}: {e.Message}");
      }
    }

    public static void DeleteFilesWithoutExt(string path, bool recursive)
    {
      DirectoryInfo d = new DirectoryInfo(path);
      FileInfo[] files = null;
      if (recursive)
        files = d.GetFiles("*", SearchOption.AllDirectories);
      else
        files = d.GetFiles("*", SearchOption.TopDirectoryOnly);
      foreach (FileInfo fileInfo in files)
      {
        if (string.IsNullOrWhiteSpace(fileInfo.Extension))
          fileInfo.Delete();
      }
    }

    public static void ReplaceInFilenamesDir(string dir, string textToFind, string textToReplace, bool recursive = true, string wildcard = "*")
    {
      int counter = 1;
      DirectoryInfo d = new DirectoryInfo(dir);
      FileInfo[] files = null;
      if (recursive)
        files = d.GetFiles(wildcard, SearchOption.AllDirectories);
      else
        files = d.GetFiles(wildcard, SearchOption.TopDirectoryOnly);
      foreach (FileInfo file in files)
      {
        ReplaceInFilename(file.FullName, textToFind, textToReplace);
        counter++;
      }
    }

    public static string ReplaceInFilename(string path, string textToFind, string textToReplace)
    {
      string ext = Path.GetExtension(path);
      string newFilename = Path.GetFileNameWithoutExtension(path).Replace(textToFind, textToReplace);
      string targetPath = Path.Combine(Path.GetDirectoryName(path), newFilename + ext);
      if (File.Exists(targetPath))
        File.Delete(targetPath);

      File.Move(path, targetPath);
      return targetPath;
    }

    public static void RenameExtensions(string dir, string oldExt, string newExt, bool recursive = true, string wildcard = "*")
    {
      DirectoryInfo d = new DirectoryInfo(dir);
      FileInfo[] files = null;
      if (recursive)
        files = d.GetFiles(wildcard, SearchOption.AllDirectories);
      else
        files = d.GetFiles(wildcard, SearchOption.TopDirectoryOnly);

      string targetPath = "";
      foreach (FileInfo file in files)
      {
        if (file.Extension.Replace(".", "") == oldExt.Replace(".", ""))
        {
          targetPath = Path.ChangeExtension(file.FullName, newExt);
          if (!File.Exists(targetPath))
            File.Delete(targetPath);
          File.Move(file.FullName, targetPath);
        }
      }
    }

    public static string RenameExtension(string filepath, string oldExt, string newExt)
    {
      try
      {
        string targetPath = filepath;
        FileInfo file = new FileInfo(filepath);

        if (file.Extension.Replace(".", "") == oldExt.Replace(".", ""))
        {
          targetPath = Path.ChangeExtension(file.FullName, newExt);

          if (!File.Exists(targetPath))
            File.Delete(targetPath);

          File.Move(file.FullName, targetPath);
        }

        return targetPath;
      }
      catch (Exception e)
      {
        Logger.Log($"RenameExtension Error: {e.Message}\n{e.Message}");
        return filepath;
      }
    }

    public static void AppendToFilenames(string dir, string append, bool recursive = true, string wildcard = "*")
    {
      DirectoryInfo d = new DirectoryInfo(dir);
      FileInfo[] files = null;
      if (recursive)
        files = d.GetFiles(wildcard, SearchOption.AllDirectories);
      else
        files = d.GetFiles(wildcard, SearchOption.TopDirectoryOnly);

      string targetPath = "";
      foreach (FileInfo file in files)
      {
        targetPath = file.FullName + append;
        if (!File.Exists(targetPath))
          File.Delete(targetPath);
        File.Move(file.FullName, targetPath);
      }
    }

    public static bool TryCopy(string source, string dest, bool overwrite)    // Copy with error handling. Returns false if failed
    {
      try
      {
        File.Copy(source, dest, overwrite);
      }
      catch (Exception e)
      {
        Logger.ErrorMessage($"[IOUtils] Copy from \"{source}\" to \"{dest}\" (Overwrite: {overwrite}) failed:", e);
        return false;
      }
      return true;
    }

    public static int GetAmountOfFiles(string path, bool recursive, string wildcard = "*")
    {
      try
      {
        DirectoryInfo d = new DirectoryInfo(path);
        FileInfo[] files = null;
        if (recursive)
          files = d.GetFiles(wildcard, SearchOption.AllDirectories);
        else
          files = d.GetFiles(wildcard, SearchOption.TopDirectoryOnly);
        return files.Length;
      }
      catch
      {
        return 0;
      }
    }

    public static string[] GetCompatibleFiles(string path, bool recursive, string wildcard = "*")
    {
      DirectoryInfo d = new DirectoryInfo(path);
      string[] files = null;
      SearchOption rec = SearchOption.AllDirectories;
      SearchOption top = SearchOption.TopDirectoryOnly;
      StringComparison ignCase = StringComparison.OrdinalIgnoreCase;

      if (recursive)
        files = Directory.GetFiles(path, wildcard, rec).Where(file => compatibleExtensions.Any(x => file.EndsWith(x, ignCase))).ToArray();
      else
        files = Directory.GetFiles(path, wildcard, top).Where(file => compatibleExtensions.Any(x => file.EndsWith(x, ignCase))).ToArray();

      return files;
    }

    public static int GetAmountOfCompatibleFiles(string path, bool recursive, string wildcard = "*")
    {
      return GetCompatibleFiles(path, recursive, wildcard).Length;
    }

    public static int GetAmountOfCompatibleFiles(string[] files)
    {
      int num = 0;
      foreach (string file in files)
      {
        if (compatibleExtensions.Contains(Path.GetExtension(file).ToLower()))
          num++;
      }
      return num;
    }

    public static long GetDirSize(string path, bool recursive, string[] includedExtensions = null)
    {
      long size = 0;
      // Add file sizes.
      string[] files;
      StringComparison ignCase = StringComparison.OrdinalIgnoreCase;
      if (includedExtensions == null)
        files = Directory.GetFiles(path);
      else
        files = Directory.GetFiles(path).Where(file => includedExtensions.Any(x => file.EndsWith(x, ignCase))).ToArray();

      foreach (string file in files)
        size += new FileInfo(file).Length;

      if (!recursive)
        return size;

      // Add subdirectory sizes.
      DirectoryInfo[] dis = new DirectoryInfo(path).GetDirectories();
      foreach (DirectoryInfo di in dis)
        size += GetDirSize(di.FullName, true, includedExtensions);

      return size;
    }

    public static void TrimFilenames(string path, int trimAmont = 4, bool recursive = true, string wildcard = "*")
    {
      DirectoryInfo d = new DirectoryInfo(path);
      FileInfo[] files = null;
      if (recursive)
        files = d.GetFiles(wildcard, SearchOption.AllDirectories);
      else
        files = d.GetFiles(wildcard, SearchOption.TopDirectoryOnly);

      foreach (FileInfo file in files)
      {
        string newPath = file.FullName.Substring(0, file.FullName.Length - 4);
        file.MoveTo(newPath);
      }
    }

    public static int GetFilenameCounterLength(string file, string prefixToRemove = "")
    {
      string filenameNoExt = Path.GetFileNameWithoutExtension(file);
      if (!string.IsNullOrEmpty(prefixToRemove))
        filenameNoExt = filenameNoExt.Replace(prefixToRemove, "");
      string onlyNumbersFilename = Regex.Replace(filenameNoExt, "[^.0-9]", "");
      return onlyNumbersFilename.Length;
    }

    public static bool TryDeleteIfExists(string path)      // Returns true if no exception occurs
    {
      try
      {
        if (path == null)
          return false;
        DeleteIfExists(path);
        return true;
      }
      catch (Exception e)
      {
        Logger.Log($"TryDeleteIfExists: Error trying to delete {path}: {e.Message}", true);
        return false;
      }
    }

    public static void DeleteIfExists(string path)
    {
      if (File.Exists(path))
        File.Delete(path);
    }

    public static void PrintFilesInDir(string path, string pattern = "*.*", bool recursive = true)
    {
      SearchOption searchOpt = SearchOption.TopDirectoryOnly;
      if (recursive)
        searchOpt = SearchOption.AllDirectories;

      foreach (string file in Directory.GetFiles(path, pattern, searchOpt))
        Logger.Log($"[IOUtils] File in {path}: {file}");
    }

    public static bool IsFileLocked(string path)
    {
      try
      {
        string newPath = path + ".locktest.tmp";
        File.Move(path, newPath);
        File.Move(newPath, path);
        return false;
      }
      catch (Exception e)
      {
        return true;
      }
    }

    public static long GetDirSize(string path)
    {
      if (!Directory.Exists(path))
        return 0;
      return GetDirSize(new DirectoryInfo(path));
    }

    public static long GetDirSize(DirectoryInfo d)
    {
      long size = 0;
      // Add file sizes.
      FileInfo[] fis = d.GetFiles();
      foreach (FileInfo fi in fis)
        size += fi.Length;

      // Add subdirectory sizes.
      DirectoryInfo[] dis = d.GetDirectories();
      foreach (DirectoryInfo di in dis)
        size += GetDirSize(di);

      return size;
    }

    public static void RemoveReadonlyFlag(string path)
    {
      try
      {
        File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);
      }
      catch (Exception e)
      {
        Logger.Log($"[IOUtils] Failed removing ReadOnly flag on {path}:\n{e.Message}");
      }
    }

    public static long GetDiskSpace(string path, bool mbytes = true)
    {
      try
      {
        string driveLetter = path.Substring(0, 2);      // Make 'C:/some/random/path' => 'C:' etc
        DriveInfo[] allDrives = DriveInfo.GetDrives();
        foreach (DriveInfo d in allDrives)
        {
          if (d.IsReady == true && d.Name.StartsWith(driveLetter))
          {
            if (mbytes)
              return (long)(d.AvailableFreeSpace / 1024f / 1000f);
            else
              return d.AvailableFreeSpace;
          }
        }
      }
      catch (Exception e)
      {
        Logger.ErrorMessage("Error trying to get disk space.", e);
      }
      return 0;
    }

    public static bool HasEnoughDiskSpace(string path, float multiplier = 2.0f)
    {
      int requiredDiskSpaceMb = 0;

      if (IsPathDirectory(path))
        requiredDiskSpaceMb = (GetDirSize(new DirectoryInfo(path)) / 1024f / 1000f).RoundToInt();
      else
        requiredDiskSpaceMb = (new FileInfo(path).Length / 1024f / 1000f).RoundToInt();

      return HasEnoughDiskSpace(requiredDiskSpaceMb, path, multiplier);
    }

    public static bool HasEnoughDiskSpace(int mBytes, string drivePath, float multiplier = 2.0f)
    {
      int requiredDiskSpaceMb = mBytes;
      long availDiskSpaceMb = GetDiskSpace(drivePath);
      Logger.Log($"Disk space check for {drivePath} with multiplier {multiplier} - {requiredDiskSpaceMb} MB needed, {availDiskSpaceMb} MB available");
      if (availDiskSpaceMb > requiredDiskSpaceMb)
        return true;
      return false;
    }

    public static string[] GetFilesSorted(string path, bool recursive = false, string pattern = "*")
    {
      SearchOption opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
      return Directory.GetFiles(path, pattern, opt).OrderBy(x => Path.GetFileName(x)).ToArray();
    }

    public static string[] GetFilesSorted(string path, string pattern = "*")
    {
      return GetFilesSorted(path, false, pattern);
    }

    public static string[] GetFilesSorted(string path)
    {
      return GetFilesSorted(path, false, "*");
    }

    public static FileInfo[] GetFileInfosSorted(string path, bool recursive = false, string pattern = "*")
    {
      SearchOption opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
      DirectoryInfo dir = new DirectoryInfo(path);
      return dir.GetFiles(pattern, opt).OrderBy(x => x.Name).ToArray();
    }

    /// <summary>
    /// Easily rename a file without needing to specify the full move path
    /// </summary>
    public static bool RenameFile(string path, string newName, bool alsoRenameExtension = false)
    {
      try
      {
        string dir = Path.GetDirectoryName(path);
        string ext = Path.GetExtension(path);
        string movePath = Path.Combine(dir, newName);

        if (!alsoRenameExtension)
          movePath += ext;

        File.Move(path, movePath);
        return true;
      }
      catch (Exception e)
      {
        Logger.Log($"Failed to rename '{path}' to '{newName}': {e.Message}", true);
        return false;
      }
    }

    /// <summary>
    /// Checks if a file seems to be a video based on its extensions
    /// </summary>
    public static bool IsFileVideo(string path)
    {
      string[] exts = new string[] { "mp4", "mkv", "mov", "webm", "avi", "m4v", "gif", "bik" };

      if (exts.Contains(Path.GetExtension(path).Replace(".", "").ToLower()))
        return true;

      return false;
    }

    public static bool CreateFileIfNotExists(string path)
    {
      if (File.Exists(path))
        return false;

      try
      {
        File.Create(path).Close();
        return true;
      }
      catch (Exception e)
      {
        Logger.Log($"Failed to create file at '{path}': {e.Message}");
        return false;
      }
    }
  }
}
