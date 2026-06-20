using Microsoft.Win32;
using System;
using System.IO;

namespace WellBot.Desktop.Services;

public interface IAutoStartService
{
    void Enable();
    void Disable();
    bool IsEnabled();
}

public class AutoStartService : IAutoStartService
{
    private const string AppName = "WellBot";
    private const string RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public void Enable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
            if (key != null)
            {
                string exePath = Environment.ProcessPath ?? string.Empty;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, exePath);
                }
            }
        }
        catch { /* Handle permissions if needed */ }
    }

    public void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
            if (key != null)
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch { }
    }

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, false);
            if (key != null)
            {
                return key.GetValue(AppName) != null;
            }
        }
        catch { }
        return false;
    }
}
