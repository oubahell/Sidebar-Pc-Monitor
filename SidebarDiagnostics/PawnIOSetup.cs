using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace SidebarDiagnostics.Utilities
{
    /// <summary>
    /// Installs the PawnIO kernel driver, which LibreHardwareMonitor needs for any reading that
    /// comes from the CPU itself.
    ///
    /// LibreHardwareMonitor 0.9.5 dropped the old self-installing WinRing0 driver and routes all
    /// MSR/SMN/LPC access through PawnIO instead. Without it the library still enumerates the
    /// sensors but every one of those readings comes back as 0 or null -- CPU temperature, core
    /// clocks, package power and the Super I/O chip (board temperatures and fan speeds) all go
    /// dead, while OS-sourced readings like per-core load keep working. That asymmetry is the
    /// signature of PawnIO being absent.
    ///
    /// The installer is embedded so this ships as one download. PawnIO is GPL-2.0-or-later, and its
    /// licence carries a special exception permitting combination with independent modules that
    /// talk to it purely over the device IO control interface, which is exactly how
    /// LibreHardwareMonitor uses it. See NOTICE.md for the attribution and source offer.
    /// </summary>
    public static class PawnIOSetup
    {
        private const string RESOURCE_NAME = "SidebarDiagnostics.Resources.PawnIO_setup.exe";
        private const string UNINSTALL_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO";

        /// <summary>
        /// Whether PawnIO is installed. This mirrors the check inside LibreHardwareMonitor itself,
        /// which reads the installed version out of the uninstall key -- a running driver alone is
        /// not enough, because the library resolves the module set through the installed product.
        /// </summary>
        public static bool IsInstalled
        {
            get
            {
                return GetInstalledVersion() != null;
            }
        }

        public static string GetInstalledVersion()
        {
            try
            {
                using (RegistryKey _base = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (RegistryKey _key = _base.OpenSubKey(UNINSTALL_KEY))
                {
                    if (_key != null)
                    {
                        return _key.GetValue("DisplayVersion") as string;
                    }
                }
            }
            catch (Exception)
            {
                // A missing or unreadable key just means "not installed" as far as we're concerned.
            }

            return null;
        }

        /// <summary>
        /// Extracts the embedded installer and runs it silently. The app already runs elevated (see
        /// app.manifest), so this does not raise a second UAC prompt.
        /// </summary>
        /// <returns>True if PawnIO is installed once this returns.</returns>
        public static bool Install()
        {
            if (IsInstalled)
            {
                return true;
            }

            string _path = Path.Combine(Path.GetTempPath(), "PawnIO_setup.exe");

            try
            {
                using (Stream _resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(RESOURCE_NAME))
                {
                    if (_resource == null)
                    {
                        return false;
                    }

                    using (FileStream _file = File.Create(_path))
                    {
                        _resource.CopyTo(_file);
                    }
                }

                ProcessStartInfo _info = new ProcessStartInfo(_path, "-install -silent")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process _process = Process.Start(_info))
                {
                    // Driver installation is quick, but cap the wait so a hung installer can never
                    // block startup indefinitely.
                    if (_process != null && _process.WaitForExit(120000))
                    {
                        return _process.ExitCode == 0 && IsInstalled;
                    }
                }
            }
            catch (Exception)
            {
                // Fall through: treated as "could not install", and the app carries on with the
                // CPU-sourced readings unavailable rather than failing to start.
            }
            finally
            {
                try
                {
                    if (File.Exists(_path))
                    {
                        File.Delete(_path);
                    }
                }
                catch (IOException)
                {
                }
            }

            return false;
        }
    }
}
