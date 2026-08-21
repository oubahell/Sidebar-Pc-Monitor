using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Win32.TaskScheduler;
using SidebarDiagnostics.Framework;
using System.Diagnostics;

namespace SidebarDiagnostics.Utilities
{
    public static class Paths
    {
        private const string SETTINGS = "settings.json";
        private const string CHANGELOG = "ChangeLog.json";

        public static string Install(Version version)
        {
            return Path.Combine(LocalApp, string.Format("app-{0}", version.ToString(3)));
        }

        public static string Exe(Version version)
        {
            return Path.Combine(Install(version), ExeName);
        }

        public static string ChangeLog
        {
            get
            {
                return Path.Combine(CurrentDirectory, CHANGELOG);
            }
        }

        public static string CurrentDirectory
        {
            get
            {
                return Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            }
        }

        public static string TaskBar
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
            }
        }

        private static string _assemblyName { get; set; } = null;

        public static string AssemblyName
        {
            get
            {
                if (_assemblyName == null)
                {
                    _assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                }

                return _assemblyName;
            }
        }

        private static string _exeName { get; set; } = null;

        public static string ExeName
        {
            get
            {
                if (_exeName == null)
                {
                    _exeName = string.Format("{0}.exe", AssemblyName);
                }

                return _exeName;
            }
        }

        private static string _settingsFile { get; set; } = null;

        public static string SettingsFile
        {
            get
            {
                if (_settingsFile == null)
                {
                    _settingsFile = Path.Combine(LocalApp, SETTINGS);
                }

                return _settingsFile;
            }
        }

        private static string _localApp { get; set; } = null;

        public static string LocalApp
        {
            get
            {
                if (_localApp == null)
                {
                    _localApp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AssemblyName);
                }

                return _localApp;
            }
        }
    }

    public static class Startup
    {        
        public static bool StartupTaskExists()
        {
            using (TaskService _taskService = new TaskService())
            {
                Task _task = _taskService.FindTask(Constants.Generic.TASKNAME);

                if (_task == null)
                {
                    return false;
                }

                ExecAction _action = _task.Definition.Actions.OfType<ExecAction>().FirstOrDefault();

                if (_action == null || _action.Path != Assembly.GetExecutingAssembly().Location)
                {
                    return false;
                }

                return true;
            }
        }

        public static void EnableStartupTask(string exePath = null)
        {
            try
            {
                using (TaskService _taskService = new TaskService())
                {
                    TaskDefinition _def = _taskService.NewTask();
                    _def.Triggers.Add(new LogonTrigger() { Enabled = true });
                    _def.Actions.Add(new ExecAction(exePath ?? Assembly.GetExecutingAssembly().Location));
                    _def.Principal.RunLevel = TaskRunLevel.Highest;

                    _def.Settings.DisallowStartIfOnBatteries = false;
                    _def.Settings.StopIfGoingOnBatteries = false;
                    _def.Settings.ExecutionTimeLimit = TimeSpan.Zero;

                    _taskService.RootFolder.RegisterTaskDefinition(Constants.Generic.TASKNAME, _def);
                }
            }
            catch (Exception e)
            {
                using (EventLog _log = new EventLog("Application"))
                {
                    _log.Source = Resources.AppName;
                    _log.WriteEntry(e.ToString(), EventLogEntryType.Error, 100, 1);
                }
            }
        }

        public static void DisableStartupTask()
        {
            using (TaskService _taskService = new TaskService())
            {
                _taskService.RootFolder.DeleteTask(Constants.Generic.TASKNAME, false);
            }
        }
    }

    public static class Culture
    {
        public const string DEFAULT = "Default";

        public static void SetDefault()
        {
            Default = Thread.CurrentThread.CurrentUICulture;
        }

        public static void SetCurrent(bool init)
        {
            Resources.Culture = CultureInfo;

            Thread.CurrentThread.CurrentCulture = CultureInfo;
            Thread.CurrentThread.CurrentUICulture = CultureInfo;

            if (init)
            {
                FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.Name)));
            }
        }

        /// <summary>
        /// The language list for Settings: "Default", then one entry per supported language.
        /// </summary>
        /// <remarks>
        /// One entry per language, not per region. This used to enumerate every specific culture
        /// whose language we support, which meant scrolling past a dozen Englishes and a dozen
        /// Arabics to find the only distinction that actually changes anything - the app has one
        /// translation per language, so "English (Belize)" and "English (Jamaica)" were the same
        /// choice listed twice.
        ///
        /// A culture already saved from the old list is added back in if it is not one of the
        /// canonical entries, otherwise upgrading would leave the box blank with nothing selected.
        /// </remarks>
        public static CultureItem[] GetAll()
        {
            List<CultureItem> _items = new List<CultureItem>();

            _items.Add(new CultureItem() { Value = DEFAULT, Text = Resources.SettingsLanguageDefault });
            _items.AddRange(GetNative());

            string _saved = Framework.Settings.Instance.Culture;

            if (!string.IsNullOrEmpty(_saved) && !_items.Any(i => string.Equals(i.Value, _saved, StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    _items.Add(new CultureItem() { Value = _saved, Text = new CultureInfo(_saved).NativeName });
                }
                catch (CultureNotFoundException)
                {
                    // A settings file naming a culture Windows does not know. Leaving it out is
                    // right - CultureInfo throws the same way when we try to apply it.
                }
            }

            return _items.ToArray();
        }

        /// <summary>
        /// The supported languages, each named in its own language, for the first-run picker.
        /// </summary>
        /// <remarks>
        /// Autonyms rather than DisplayName: at first run nobody has told us which language the
        /// user reads, so an entry reading "German" only helps someone who already reads English.
        /// "Deutsch" is recognisable to the person hunting for it whatever the app is set to.
        ///
        /// The text comes from the neutral culture ("Deutsch") but the stored value is the
        /// specific one ("de-DE"). Specific cultures carry the date and number formats the sidebar
        /// renders with; a neutral one leaves those to chance.
        /// </remarks>
        public static CultureItem[] GetNative()
        {
            return Languages
                .Select(l => new CultureItem()
                {
                    Value = CultureInfo.CreateSpecificCulture(l).Name,
                    Text = CultureInfo.GetCultureInfo(l).NativeName
                })
                .OrderBy(c => c.Text, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        /// <summary>
        /// The supported language closest to the machine's own, for preselecting the picker.
        /// </summary>
        public static string GetNativeDefault()
        {
            string _language = Languages.Contains(Default.TwoLetterISOLanguageName) ? Default.TwoLetterISOLanguageName : "en";

            return CultureInfo.CreateSpecificCulture(_language).Name;
        }

        public static string[] Languages
        {
            get
            {
                return new string[13] { "en", "da", "de", "fr", "ja", "nl", "zh", "it", "ru", "fi", "es", "ar", "tr" };
            }
        }

        public static CultureInfo Default { get; private set; }

        public static CultureInfo CultureInfo
        {
            get
            {
                string culture = Framework.Settings.Instance.Culture;
                return string.Equals(culture, DEFAULT, StringComparison.Ordinal)
                    ? Default
                    : new CultureInfo(culture);
            }
        }
    }

    public class CultureItem
    {
        public string Value { get; set; }

        public string Text { get; set; }
    }
}
