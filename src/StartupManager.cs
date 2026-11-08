using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using Microsoft.Win32;

namespace HyteFlow
{
    public static class StartupManager
    {
        private const string TaskName = "HyteFlow_UniversalFanControl";
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppValueName = "HyteFlow";

        public static bool IsRunningAsAdmin()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsStartupEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false))
                {
                    if (key != null && key.GetValue(AppValueName) != null)
                        return true;
                }
            }
            catch { }

            try
            {
                var psi = new ProcessStartInfo("schtasks.exe", $"/Query /TN \"{TaskName}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var p = Process.Start(psi))
                {
                    p.WaitForExit(1500);
                    if (p.ExitCode == 0) return true;
                }
            }
            catch { }

            return false;
        }

        public static bool SetStartupEnabled(bool enable, bool startMinimized, out string statusMessage)
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            string args = startMinimized ? "--minimized" : "";
            string fullCmd = string.IsNullOrEmpty(args) ? $"\"{exePath}\"" : $"\"{exePath}\" {args}";

            try
            {
                if (enable)
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true))
                    {
                        key?.SetValue(AppValueName, fullCmd);
                    }

                    if (IsRunningAsAdmin())
                    {
                        var psi = new ProcessStartInfo("schtasks.exe",
                            $"/Create /F /RL HIGHEST /SC ONLOGON /TN \"{TaskName}\" /TR \"{fullCmd}\"")
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        using (var p = Process.Start(psi))
                        {
                            p.WaitForExit(2500);
                        }
                        statusMessage = "Enabled (Task Scheduler Highest Privileges + Registry)";
                    }
                    else
                    {
                        statusMessage = "Enabled (Windows Logon Startup Registry)";
                    }
                    return true;
                }
                else
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true))
                    {
                        if (key?.GetValue(AppValueName) != null)
                            key.DeleteValue(AppValueName, false);
                    }

                    if (IsRunningAsAdmin())
                    {
                        var psi = new ProcessStartInfo("schtasks.exe", $"/Delete /F /TN \"{TaskName}\"")
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        using (var p = Process.Start(psi))
                        {
                            p.WaitForExit(2000);
                        }
                    }
                    statusMessage = "Disabled Windows Auto-Startup";
                    return true;
                }
            }
            catch (Exception ex)
            {
                statusMessage = "Startup update failed: " + ex.Message;
                return false;
            }
        }

        public static void RestartAsAdmin()
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                var psi = new ProcessStartInfo(exePath)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    WorkingDirectory = Path.GetDirectoryName(exePath)
                };
                Process.Start(psi);
                Environment.Exit(0);
            }
            catch
            {
            }
        }
    }
}
