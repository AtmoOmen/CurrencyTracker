using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace CurrencyTracker.Utilities;

public static class FileHelper
{
    public static void OpenDirectory(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/c start \"\" \"{path}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = path
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = path
                });
            }
            else
                DService.Instance().Log.Error("Unsupported OS");
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Error($"Error :{ex.Message}");
        }
    }

    public static void OpenAndSelectFile(string filePath)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", $"-R \"{filePath}\"");
            else
                DService.Instance().Log.Error("Unsupported OS");
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Error($"Error :{ex.Message}");
        }
    }

    public static bool IsFileLocked(FileInfo file)
    {
        FileStream? stream = null;

        try
        {
            stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            return true;
        } finally
        {
            stream?.Close();
        }

        return false;
    }
}
