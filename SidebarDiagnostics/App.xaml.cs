using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Reflection;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using Velopack;
using Velopack.Sources;
using Hardcodet.Wpf.TaskbarNotification;
using SidebarDiagnostics.Monitoring;
using SidebarDiagnostics.Utilities;
using SidebarDiagnostics.Windows;

namespace SidebarDiagnostics
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // UPDATER HOOKS
            //
            // These must run before any UI exists. An install, update or uninstall relaunches
            // this exe with private arguments; Run() handles those and exits the process.
            //
            // The constructor is the earliest point we can reach. WPF generates our Main, and it
            // reads "new App(); app.InitializeComponent(); app.Run();" - InitializeComponent is
            // what builds App.xaml's resources, which include the whole theme and a live tray
            // icon. Doing that during an install hook is wasted work at best, and a throwing
            // resource would leave the install half-finished. OnStartup is later still.
            // Before anything else, including the updater hooks: this same binary also ships as
            // uninstall.exe, and in that guise it has one job and must not start an app.
            RunAsUninstallerIfNamedSo();

            VelopackApp.Build()
                .OnAfterInstallFastCallback(_v => PlaceUninstaller())
                .OnAfterUpdateFastCallback(_v => PlaceUninstaller())
                .OnBeforeUninstallFastCallback(_v => CleanUp())
                .Run();
        }

        /// <summary>
        /// When this executable is called uninstall.exe, hand over to the real uninstaller and exit.
        /// </summary>
        /// <remarks>
        /// The installer registers itself in Add/Remove Programs and that is the supported route,
        /// but people open the install folder and look for an uninstaller, and finding none reads
        /// as an app that does not want to be removed. Velopack does not produce one, so we ship a
        /// copy of this exe named uninstall.exe alongside it - the copy already sits next to every
        /// DLL it needs, which a purpose-built stub in the root folder would not.
        ///
        /// It does not uninstall anything itself. It starts Update.exe, which owns that job and
        /// runs the cleanup hook on the way through, and gets out of the way immediately: deleting
        /// this very folder while this process is holding its own exe open would not end well.
        /// </remarks>
        /// <summary>
        /// Writes uninstall.exe next to the app, on install and after each update.
        /// </summary>
        /// <remarks>
        /// It is a copy of this executable - the app checks its own filename and, under that name,
        /// hands over to Update.exe. See RunAsUninstallerIfNamedSo below.
        ///
        /// Made here rather than shipped in the package. Putting the copy in the package meant
        /// every download carried the 3.7 MB application twice, which is a poor trade for a
        /// convenience file; the update deltas carried it too. Copying it locally after install
        /// costs nothing to download and leaves exactly the same file in the folder.
        ///
        /// An update replaces the whole "current" folder, so this has to run again afterwards.
        /// </remarks>
        private static void PlaceUninstaller()
        {
            try
            {
                string _exe = Process.GetCurrentProcess().MainModule.FileName;
                string _target = Path.Combine(Path.GetDirectoryName(_exe), UNINSTALL_NAME + ".exe");

                if (!string.Equals(_exe, _target, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(_exe, _target, true);
                }
            }
            catch
            {
                // Add/Remove Programs is the supported route and is registered either way. Failing
                // to place a convenience copy is not worth failing an install over.
            }
        }

        private static void RunAsUninstallerIfNamedSo()
        {
            string _exe = Process.GetCurrentProcess().MainModule.FileName;

            if (!string.Equals(Path.GetFileNameWithoutExtension(_exe), UNINSTALL_NAME, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // An installed copy lives in "current" with Update.exe one level up in the install
            // root. The parent is only considered when this folder really is called "current":
            // reaching up to any parent that happens to hold a file by that name found a stale
            // Update.exe from an unrelated install during testing and ran it.
            string _dir = Path.GetDirectoryName(_exe);
            string _updater = Path.Combine(_dir, UPDATER_NAME);

            if (!File.Exists(_updater) && string.Equals(Path.GetFileName(_dir), CURRENT_DIR, StringComparison.OrdinalIgnoreCase))
            {
                DirectoryInfo _parent = Directory.GetParent(_dir);

                _updater = _parent == null ? null : Path.Combine(_parent.FullName, UPDATER_NAME);
            }

            if (_updater == null || !File.Exists(_updater))
            {
                // The portable build has no updater, and nothing to uninstall - it is a folder.
                MessageBox.Show(
                    Framework.Resources.UninstallPortableText,
                    Framework.Resources.AppName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information,
                    MessageBoxResult.OK,
                    MessageBoxOptions.DefaultDesktopOnly);

                Environment.Exit(0);
            }

            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = _updater,
                    Arguments = "--uninstall",
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
                // Nothing useful to offer if the updater will not start; Add/Remove Programs runs
                // the same command and is still there.
            }

            Environment.Exit(0);
        }

        private const string UNINSTALL_NAME = "uninstall";

        private const string UPDATER_NAME = "Update.exe";

        private const string CURRENT_DIR = "current";

        /// <summary>
        /// Relaunches the app elevated and ends this process, unless it is elevated already.
        /// </summary>
        /// <remarks>
        /// The manifest asks for asInvoker rather than requireAdministrator, so this has to be done
        /// by hand. requireAdministrator looks like the obvious choice and is a trap: the installer
        /// launches the exe with CreateProcess to run its hooks, CreateProcess refuses to elevate,
        /// and the install fails without ever registering an uninstaller.
        ///
        /// This belongs in OnStartup, not in the App constructor where it was first put. The
        /// constructor runs before InitializeComponent, so App.xaml's resources do not exist yet -
        /// and Process.Start with a "runas" verb goes through ShellExecuteEx, which pumps a nested
        /// message loop. That pump let the queued startup work run early, and the first window it
        /// opened died on "Cannot find resource named 'FlatWindowStyle'". By OnStartup the
        /// resources are built, and the installer hooks in the constructor have already had their
        /// un-elevated process to work in.
        ///
        /// The startup scheduled task runs the app elevated already, so a logon start goes straight
        /// past this with no prompt. A manual launch costs one UAC prompt, which is what the old
        /// manifest cost too.
        /// </remarks>
        private static void Elevate()
        {
            if (IsElevated())
            {
                return;
            }

            string[] _args = Environment.GetCommandLineArgs().Skip(1).ToArray();

            // Guard against a relaunch loop: if the elevated copy somehow comes back un-elevated,
            // carry on degraded rather than spawning copies of ourselves forever.
            if (_args.Contains(ELEVATED_ARG, StringComparer.Ordinal))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = Process.GetCurrentProcess().MainModule.FileName,
                    Arguments = string.Join(" ", _args.Concat(new string[] { ELEVATED_ARG })),
                    UseShellExecute = true,
                    Verb = "runas"
                });
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // The user dismissed the prompt. Nothing useful left to do - without elevation
                // every reading would be blank - so leave quietly rather than showing an error.
            }

            Environment.Exit(0);
        }

        private static bool IsElevated()
        {
            using (WindowsIdentity _identity = WindowsIdentity.GetCurrent())
            {
                return new WindowsPrincipal(_identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private const string ELEVATED_ARG = "--elevated";

        /// <summary>
        /// Removes everything the app leaves outside its own folder, on uninstall.
        /// </summary>
        /// <remarks>
        /// The installer deletes the install directory, the shortcuts and its own registry entry,
        /// and nothing else. Left to itself that leaves behind the "SidebarStartup" scheduled task
        /// - a logon task still asking for elevation to run an exe that is gone - the Application
        /// event log source the startup-task error path registers under HKLM, and, worst of the
        /// three, the app itself still running.
        ///
        /// A running copy is not just untidy: the app runs elevated and this hook does not, so
        /// nothing here can close it, and while it lives it holds its own exe open and the
        /// installer cannot delete the folder around it.
        ///
        /// Settings live inside the install directory, so the installer already takes them - the
        /// delete here is for the case where that stops being true.
        ///
        /// Every step swallows its own failure. A leftover file is a much smaller problem than an
        /// uninstall that dies partway and leaves the app half-removed and unlaunchable.
        /// </remarks>
        private static void CleanUp()
        {
            // Steps that need no extra rights come first, so they still happen even if the
            // elevated step below is refused.
            try
            {
                if (EventLog.SourceExists(Framework.Resources.AppName))
                {
                    EventLog.DeleteEventSource(Framework.Resources.AppName);
                }
            }
            catch { }

            try
            {
                if (File.Exists(Utilities.Paths.SettingsFile))
                {
                    File.Delete(Utilities.Paths.SettingsFile);
                }
            }
            catch { }

            // The remaining work needs administrator rights, so it goes out as one batch - one
            // prompt rather than two - and only when there is actually something to do.
            List<string> _steps = new List<string>();

            // Close any copy that is still running. This hook is not elevated and the app is, so
            // it cannot be killed from here; and while it runs it holds its own exe open, which
            // stops the installer from deleting the folder that exe lives in.
            try
            {
                int _self = Process.GetCurrentProcess().Id;

                if (Process.GetProcessesByName(Utilities.Paths.AssemblyName).Any(p => p.Id != _self))
                {
                    _steps.Add(string.Format("taskkill /f /im \"{0}\" /fi \"PID ne {1}\"", Utilities.Paths.ExeName, _self));
                }
            }
            catch { }

            // Registered at Highest run level by an elevated process, so removing it takes the
            // same rights. Left alone it survives as a logon task pointing at an exe that is gone.
            //
            // Asked about with schtasks rather than the scheduler library. Two earlier attempts
            // leaned on managed code here - TaskService.FindTask, then the saved RunAtStartup flag
            // - and both came back empty inside the uninstall hook while the task sat plainly in
            // the scheduler, so the step was never queued and the task outlived the app twice.
            // Querying costs no elevation; only the delete does.
            if (StartupTaskPresent())
            {
                _steps.Add(string.Format("schtasks /delete /tn \"{0}\" /f", Constants.Generic.TASKNAME));
            }

            if (_steps.Count == 0)
            {
                return;
            }

            try
            {
                Process _elevated = Process.Start(new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + string.Join(" & ", _steps.ToArray()),
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                // Bounded wait rather than fire and forget: the installer starts deleting the app
                // folder the moment this returns, and that fails while the old process still has
                // its exe open. Capped so a dismissed prompt cannot hang the uninstall.
                if (_elevated != null)
                {
                    _elevated.WaitForExit(15000);
                }
            }
            catch { }
        }

        /// <summary>
        /// Whether the logon task exists, asked of schtasks rather than of managed code.
        /// </summary>
        /// <remarks>
        /// Querying a task needs no elevation - only deleting one does - and schtasks answers the
        /// same way inside the uninstall hook as it does anywhere else, which is more than can be
        /// said for the managed scheduler API in that context.
        /// </remarks>
        private static bool StartupTaskPresent()
        {
            try
            {
                Process _probe = Process.Start(new ProcessStartInfo()
                {
                    FileName = "schtasks.exe",
                    Arguments = string.Format("/query /tn \"{0}\"", Constants.Generic.TASKNAME),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

                if (_probe == null)
                {
                    return false;
                }

                _probe.WaitForExit(5000);

                return _probe.HasExited && _probe.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        protected async override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // The app cannot read a single sensor without administrator rights, so get them before
            // doing anything else. Exits this process when it relaunches, so nothing below runs.
            Elevate();

            // ERROR HANDLING
            #if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomain_Error);
            #endif

            // LANGUAGE
            Culture.SetDefault();

            // THEME
            // Applied up here, ahead of the first-run language picker, so that window is dressed
            // like the rest of the app rather than in bare WPF grey.
            ApplyTheme(Framework.Settings.Instance.Theme);

            // FIRST RUN LANGUAGE
            // Asked before SetCurrent for two reasons: SetCurrent's OverrideMetadata call can only
            // happen once per run, and everything after this point - the setup wizard, the tray
            // tooltip, the sidebar - should already be in the language just chosen.
            if (Framework.Settings.Instance.InitialSetup)
            {
                new LanguageSetup().ShowDialog();
            }

            Culture.SetCurrent(true);

            // SETTINGS
            CheckSettings();

            // LAYOUT
            // Applied before the sidebar is built so the metric rows render in the chosen layout
            // from the first frame rather than flashing the default first.
            Framework.LayoutManager.Apply(Framework.Settings.Instance.Layout);

            // HARDWARE ACCESS DRIVER
            // Must run before the first MonitorManager is built: LibreHardwareMonitor probes for
            // PawnIO when it opens the Computer, so installing it afterwards would leave every
            // CPU-sourced reading (temperature, clocks, package power) stuck at zero until restart.
            CheckPawnIO();

            // VERSION
            Version _version = Assembly.GetExecutingAssembly().GetName().Version;
            string _vstring = _version.ToString(3);

            // TRAY ICON
            TrayIcon = (TaskbarIcon)FindResource("TrayIcon");
            TrayIcon.ToolTipText = string.Format("{0} v{1}", Framework.Resources.AppName, _vstring);
            TrayIcon.TrayContextMenuOpen += TrayIcon_TrayContextMenuOpen;

            // UPDATE
            //
            // Deliberately not awaited: a launch should not wait on a network round trip, and the
            // old behaviour - download and relaunch silently, before the sidebar had even appeared
            // - meant the app could vanish and come back while you were still looking for it.
            // NotifyUpdate only looks, and tells you.
            #if !DEBUG
            if (Framework.Settings.Instance.AutoUpdate)
            {
                NotifyUpdate();
            }
            #endif

            // START APP
            if (Framework.Settings.Instance.InitialSetup)
            {
                new Setup();
            }
            else
            {
                StartApp(false);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            TrayIcon.Dispose();

            base.OnExit(e);
        }

        public static void StartApp(bool openSettings)
        {
            Version _version = Assembly.GetExecutingAssembly().GetName().Version;
            string _vstring = _version.ToString(3);

            if (!string.Equals(Framework.Settings.Instance.ChangeLog, _vstring, StringComparison.OrdinalIgnoreCase))
            {
                Framework.Settings.Instance.ChangeLog = _vstring;
                Framework.Settings.Instance.Save();

                new ChangeLog(_version).Show();
            }

            new Sidebar(openSettings, Framework.Settings.Instance.InitiallyHidden).Show();

            RefreshIcon();
        }

        public static void RefreshIcon()
        {
            TrayIcon.Visibility = Framework.Settings.Instance.ShowTrayIcon ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Relaunches the app in place.
        /// </summary>
        /// <remarks>
        /// This is what a language change needs. Every visible string is bound with x:Static, which
        /// XAML resolves once when the window is loaded, so pointing Resources.Culture somewhere new
        /// underneath a running window changes nothing on screen. Reloading only the sidebar - which
        /// is what Apply used to do - is why the app appeared to restart and come back in the old
        /// language: the sidebar was rebuilt from the same already-resolved strings, and the settings
        /// window, tray menu and tooltips were never rebuilt at all.
        ///
        /// Settings are written to disk before we get here, so the new process reads the new value.
        /// The relaunched process inherits this one's elevation, so there is no second UAC prompt.
        /// </remarks>
        public static void Restart()
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                UseShellExecute = true
            });

            Current.Shutdown();
        }

        /// <summary>
        /// Installs the bundled PawnIO driver if it's missing, asking first, since installing a
        /// kernel driver isn't something to do silently behind the user's back. Declining is
        /// remembered so this only ever asks once.
        /// </summary>
        private static void CheckPawnIO()
        {
            if (PawnIOSetup.IsInstalled || Framework.Settings.Instance.SkipDriverPrompt)
            {
                return;
            }

            MessageBoxResult _result = MessageBox.Show(
                Framework.Resources.DriverPromptText,
                Framework.Resources.DriverPromptTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.Yes,
                MessageBoxOptions.DefaultDesktopOnly);

            if (_result != MessageBoxResult.Yes)
            {
                Framework.Settings.Instance.SkipDriverPrompt = true;
                Framework.Settings.Instance.Save();
                return;
            }

            if (!PawnIOSetup.Install())
            {
                MessageBox.Show(
                    Framework.Resources.DriverFailedText,
                    Framework.Resources.DriverPromptTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning,
                    MessageBoxResult.OK,
                    MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        public static void ApplyTheme(Framework.ThemeKind theme)
        {
            ResourceDictionary _themeDict = new ResourceDictionary()
            {
                Source = new Uri(Framework.ThemePreset.Get(theme).ResourcePath, UriKind.Relative)
            };

            Collection<ResourceDictionary> _merged = Current.Resources.MergedDictionaries;

            ResourceDictionary _existing = _merged.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.StartsWith("Themes/", StringComparison.OrdinalIgnoreCase));

            if (_existing != null)
            {
                int _index = _merged.IndexOf(_existing);
                _merged.RemoveAt(_index);
                _merged.Insert(_index, _themeDict);
            }
            else
            {
                _merged.Insert(0, _themeDict);
            }
        }

        public static void ShowPerformanceCounterError()
        {
            MessageBoxResult _result = MessageBox.Show(Framework.Resources.ErrorPerformanceCounter, Framework.Resources.ErrorTitle, MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);

            if (_result == MessageBoxResult.OK)
            {
                Process.Start(ConfigurationManager.AppSettings["WikiURL"]);
            }
        }

        public void OpenSettings()
        {
            Settings _settings = Windows.OfType<Settings>().FirstOrDefault();

            if (_settings != null)
            {
                _settings.WindowState = WindowState.Normal;
                _settings.Activate();
                return;
            }

            Sidebar _sidebar = Sidebar;

            if (_sidebar == null)
            {
                return;
            }

            new Settings(_sidebar);
        }

        public void OpenGraph()
        {
            Sidebar _sidebar = Sidebar;

            if (_sidebar == null || !_sidebar.Ready)
            {
                return;
            }

            new Graph(_sidebar);
        }

        /// <summary>
        /// Checks GitHub Releases for a newer build and installs it.
        ///
        /// Replaces the original Squirrel.Windows implementation, which is unmaintained and whose
        /// release feed pointed at the upstream project -- left as it was, it would have replaced
        /// this fork with someone else's binary. Velopack is Squirrel's actively maintained
        /// successor and reads releases straight from this repository.
        ///
        /// Updates only work for an installed build. Running loose from a build output folder there
        /// is nothing for the updater to replace, so it reports "up to date" rather than failing.
        /// </summary>
        /// <summary>
        /// Looks for a newer release and, if there is one, says so in a tray balloon. Clicking the
        /// balloon installs it.
        /// </summary>
        /// <remarks>
        /// Runs on every launch. It deliberately does not install anything on its own: an update
        /// that downloads and relaunches the app unasked is the kind of helpfulness that loses
        /// someone's arrangement of windows mid-session. Looking is free; deciding is the user's.
        ///
        /// Failures are silent by design. Nobody wants an error box on every launch because their
        /// connection is down, and the same failure surfaces properly through the tray menu's
        /// check, which the user asked for and is waiting on.
        /// </remarks>
        private async void NotifyUpdate()
        {
            string _feed = ConfigurationManager.AppSettings["CurrentReleaseURL"];

            if (string.IsNullOrWhiteSpace(_feed))
            {
                return;
            }

            try
            {
                UpdateManager _manager = new UpdateManager(new GithubSource(_feed, null, false));

                if (!_manager.IsInstalled)
                {
                    // Running from the portable build or straight out of a build folder. There is
                    // nothing for the updater to replace.
                    return;
                }

                UpdateInfo _update = await _manager.CheckForUpdatesAsync();

                if (_update == null || TrayIcon == null)
                {
                    return;
                }

                _pendingManager = _manager;
                _pendingUpdate = _update;

                TrayIcon.TrayBalloonTipClicked -= UpdateBalloon_Clicked;
                TrayIcon.TrayBalloonTipClicked += UpdateBalloon_Clicked;

                TrayIcon.ShowBalloonTip(
                    Framework.Resources.AppName,
                    string.Format(Framework.Resources.UpdateAvailableText, _update.TargetFullRelease.Version),
                    BalloonIcon.Info);
            }
            catch (Exception)
            {
                // See the remarks: a launch-time check stays quiet about its own failures.
            }
        }

        private static async void UpdateBalloon_Clicked(object sender, RoutedEventArgs e)
        {
            UpdateManager _manager = _pendingManager;
            UpdateInfo _update = _pendingUpdate;

            if (_manager == null || _update == null)
            {
                return;
            }

            // Cleared first: a second click while the download is running would start it twice.
            _pendingManager = null;
            _pendingUpdate = null;

            Update _updateWindow = new Update();
            _updateWindow.Show();

            try
            {
                await _manager.DownloadUpdatesAsync(_update, p => _updateWindow.SetProgress(p));

                _updateWindow.Close();

                // Hands over to the updater, which swaps the files and relaunches.
                _manager.ApplyUpdatesAndRestart(_update);
            }
            catch (Exception)
            {
                _updateWindow.Close();

                MessageBox.Show(Framework.Resources.UpdateErrorText, Framework.Resources.UpdateErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        private static UpdateManager _pendingManager { get; set; }

        private static UpdateInfo _pendingUpdate { get; set; }

        private async Task AppUpdate(bool showInfo)
        {
            string _feed = ConfigurationManager.AppSettings["CurrentReleaseURL"];

            if (string.IsNullOrWhiteSpace(_feed))
            {
                return;
            }

            try
            {
                UpdateManager _manager = new UpdateManager(new GithubSource(_feed, null, false));

                if (!_manager.IsInstalled)
                {
                    if (showInfo)
                    {
                        MessageBox.Show(Framework.Resources.UpdateSuccessText, Framework.Resources.AppName, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    }

                    return;
                }

                UpdateInfo _update = await _manager.CheckForUpdatesAsync();

                if (_update == null)
                {
                    if (showInfo)
                    {
                        MessageBox.Show(Framework.Resources.UpdateSuccessText, Framework.Resources.AppName, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    }

                    return;
                }

                Update _updateWindow = new Update();
                _updateWindow.Show();

                await _manager.DownloadUpdatesAsync(_update, p => _updateWindow.SetProgress(p));

                _updateWindow.Close();

                // Hands over to the updater, which swaps the files and relaunches. Nothing after
                // this runs, so the settings write above must already have happened.
                _manager.ApplyUpdatesAndRestart(_update);
            }
            catch (WebException)
            {
                // No connectivity, or the feed is unreachable. Silent unless the user asked.
                if (showInfo)
                {
                    MessageBox.Show(Framework.Resources.UpdateErrorText, Framework.Resources.UpdateErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }
            }
            catch (Exception e)
            {
                // Something structural is wrong with updating on this machine. Switch auto-update
                // off so it cannot fail on every launch, and leave a trace in the event log.
                Framework.Settings.Instance.AutoUpdate = false;
                Framework.Settings.Instance.Save();

                try
                {
                    using (EventLog _log = new EventLog("Application"))
                    {
                        _log.Source = Framework.Resources.AppName;
                        _log.WriteEntry(e.ToString(), EventLogEntryType.Error, 100, 1);
                    }
                }
                catch (Exception)
                {
                }

                if (showInfo)
                {
                    MessageBox.Show(Framework.Resources.UpdateErrorFatalText, Framework.Resources.UpdateErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }
            }
        }

        private void CheckSettings()
        {
            if (Framework.Settings.Instance.RunAtStartup && !Utilities.Startup.StartupTaskExists())
            {
                Utilities.Startup.EnableStartupTask();
            }

            Framework.Settings.Instance.MonitorConfig = MonitorConfig.CheckConfig(Framework.Settings.Instance.MonitorConfig);
        }

        private void TrayIcon_TrayContextMenuOpen(object sender, RoutedEventArgs e)
        {
            Monitor _primary = Monitor.GetMonitors().GetPrimary();

            TrayIcon.ContextMenu.HorizontalOffset *= _primary.InverseScaleX;
            TrayIcon.ContextMenu.VerticalOffset *= _primary.InverseScaleY;
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            OpenSettings();
        }

        private void Reload_Click(object sender, EventArgs e)
        {
            Sidebar _sidebar = Sidebar;

            if (_sidebar == null)
            {
                return;
            }

            _sidebar.Reload();
        }

        private void Graph_Click(object sender, EventArgs e)
        {
            OpenGraph();
        }

        private void Visibility_SubmenuOpened(object sender, EventArgs e)
        {
            Sidebar _sidebar = Sidebar;

            if (_sidebar == null)
            {
                return;
            }

            MenuItem _this = (MenuItem)sender;

            (_this.Items.GetItemAt(0) as MenuItem).IsChecked = _sidebar.Visibility == Visibility.Visible;
            (_this.Items.GetItemAt(1) as MenuItem).IsChecked = _sidebar.Visibility == Visibility.Hidden;
        }
        
        private void Show_Click(object sender, EventArgs e)
        {
            Sidebar _sidebar = Sidebar;

            if (_sidebar == null || _sidebar.Visibility == Visibility.Visible)
            {
                return;
            }

            _sidebar.AppBarShow();
        }

        private void Hide_Click(object sender, EventArgs e)
        {
            Sidebar _sidebar = Sidebar;

            if (_sidebar == null || _sidebar.Visibility == Visibility.Hidden)
            {
                return;
            }

            _sidebar.AppBarHide();
        }

        private void Donate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(ConfigurationManager.AppSettings["DonateURL"]);
        }

        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(ConfigurationManager.AppSettings["RepoURL"]);
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            await AppUpdate(true);
        }

        private void Close_Click(object sender, EventArgs e)
        {
            Shutdown();
        }
        
        private static void AppDomain_Error(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;

            MessageBox.Show(ex.ToString(), Framework.Resources.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }
        
        public Sidebar Sidebar
        {
            get
            {
                return Windows.OfType<Sidebar>().FirstOrDefault();
            }
        }

        public IEnumerable<Graph> Graphs
        {
            get
            {
                return Windows.OfType<Graph>();
            }
        }

        public new static App Current
        {
            get
            {
                return (App)Application.Current;
            }
        }

        public static TaskbarIcon TrayIcon { get; set; }

        internal static bool _reloading { get; set; } = false;
    }
}
