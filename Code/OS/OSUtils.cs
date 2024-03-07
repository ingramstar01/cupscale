﻿using System;
using System.Diagnostics;
using System.Management;
using System.Security.Principal;

namespace Cupscale.OS
{
  class OsUtils
  {
    public static bool IsUserAdministrator()
    {
      //bool value to hold our return value
      bool isAdmin;
      WindowsIdentity user = null;
      try
      {
        //get the currently logged in user
        user = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(user);
        isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
      }
      catch (UnauthorizedAccessException ex)
      {
        isAdmin = false;
      }
      catch (Exception ex)
      {
        isAdmin = false;
      }
      finally
      {
        if (user != null)
          user.Dispose();
      }
      return isAdmin;
    }

    //public enum ProcessMode { Visible }
    public static Process SetStartInfo(Process proc, bool hidden, string filename = "cmd.exe")
    {
      proc.StartInfo.UseShellExecute = !hidden;
      proc.StartInfo.RedirectStandardOutput = hidden;
      proc.StartInfo.RedirectStandardError = hidden;
      proc.StartInfo.CreateNoWindow = hidden;
      proc.StartInfo.FileName = filename;
      return proc;
    }

    public static Process NewProcess(bool hidden, string filename = "cmd.exe")
    {
      Process proc = new Process();
      return SetStartInfo(proc, hidden, filename);
    }

    public static void KillProcessTree(Process proc)
    {
      if (proc != null)
        KillProcessTree(proc.Id);
    }

    public static void KillProcessTree(int pid)
    {
      try
      {
        ManagementObjectSearcher processSearcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
        ManagementObjectCollection processCollection = processSearcher.Get();

        Process proc = Process.GetProcessById(pid);
        if (!proc.HasExited) proc.Kill();

        if (processCollection != null)
        {
          foreach (ManagementObject mo in processCollection)
          {
            KillProcessTree(Convert.ToInt32(mo["ProcessID"])); //kill child processes(also kills childrens of childrens etc.)
          }
        }
      }
      catch
      {
        // has probably exited already
      }
    }
  }
}
