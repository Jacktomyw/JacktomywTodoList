using Microsoft.Win32;

namespace TodoWidget.Native;

public static class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "TodoWidget";
    public static bool IsAutoStartEnabled() { using var k = Registry.CurrentUser.OpenSubKey(RunKey, false); return k?.GetValue(AppName) != null; }
    public static void SetAutoStart(bool enabled) { using var k = Registry.CurrentUser.OpenSubKey(RunKey, true); if (enabled) k?.SetValue(AppName, $"\"{Environment.ProcessPath}\""); else k?.DeleteValue(AppName, false); }
}
