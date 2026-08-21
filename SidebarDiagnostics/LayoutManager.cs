using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Markup;

namespace SidebarDiagnostics.Framework
{
    /// <summary>
    /// Loads the resource dictionary that decides how a metric is drawn.
    ///
    /// The sidebar binds its metric rows to a DynamicResource named "MetricTemplate" rather than
    /// declaring the template inline, so swapping the dictionary swaps the entire presentation --
    /// text rows, bars, tiles -- with no code change. Built-in layouts ship as compiled Pages;
    /// anything dropped into the user layouts folder is loaded as loose XAML at runtime, which is
    /// what lets people write their own without rebuilding the app.
    /// </summary>
    public static class LayoutManager
    {
        public const string DEFAULT = "Classic";

        private static readonly string[] BUILTIN = new string[] { "Classic", "Compact", "Bars", "Tiles" };

        /// <summary>Where user-authored layouts are read from, alongside settings.json.</summary>
        public static string UserLayoutPath
        {
            get
            {
                return Path.Combine(Utilities.Paths.LocalApp, "Layouts");
            }
        }

        /// <summary>
        /// Every selectable layout: the built-ins first, then any .xaml found in the user folder.
        /// A user file whose name matches a built-in replaces it, so a shipped layout can be
        /// overridden without editing the app.
        /// </summary>
        public static string[] GetAvailable()
        {
            List<string> _names = new List<string>(BUILTIN);

            try
            {
                if (Directory.Exists(UserLayoutPath))
                {
                    foreach (string _file in Directory.GetFiles(UserLayoutPath, "*.xaml"))
                    {
                        string _name = Path.GetFileNameWithoutExtension(_file);

                        if (!_names.Contains(_name, StringComparer.OrdinalIgnoreCase))
                        {
                            _names.Add(_name);
                        }
                    }
                }
            }
            catch (IOException)
            {
                // An unreadable folder just means no user layouts are offered.
            }

            return _names.ToArray();
        }

        /// <summary>
        /// Swaps the active layout dictionary in the application's merged resources.
        /// </summary>
        /// <returns>True if the requested layout loaded; false if it fell back to the default.</returns>
        public static bool Apply(string name)
        {
            ResourceDictionary _layout = Load(name);

            bool _loaded = _layout != null;

            if (!_loaded && !string.Equals(name, DEFAULT, StringComparison.OrdinalIgnoreCase))
            {
                // A layout that fails to parse must not leave the sidebar with no metric template
                // at all, so fall back rather than propagating the failure.
                _layout = Load(DEFAULT);
            }

            if (_layout == null)
            {
                return false;
            }

            Collection<ResourceDictionary> _merged = Application.Current.Resources.MergedDictionaries;

            ResourceDictionary _existing = _merged.FirstOrDefault(IsLayoutDictionary);

            if (_existing != null)
            {
                int _index = _merged.IndexOf(_existing);
                _merged.RemoveAt(_index);
                _merged.Insert(_index, _layout);
            }
            else
            {
                _merged.Add(_layout);
            }

            return _loaded;
        }

        /// <summary>
        /// Identifies the currently applied layout dictionary. Matched on the template key it
        /// contributes rather than on its Source, because a user layout loaded from loose XAML has
        /// no Source to compare against.
        /// </summary>
        private static bool IsLayoutDictionary(ResourceDictionary dictionary)
        {
            if (dictionary.Source != null && dictionary.Source.OriginalString.IndexOf("Layouts/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return dictionary.Source == null && dictionary.Contains("MetricTemplate");
        }

        private static ResourceDictionary Load(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            ResourceDictionary _user = LoadUser(name);

            if (_user != null)
            {
                return _user;
            }

            if (!BUILTIN.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            try
            {
                return new ResourceDictionary()
                {
                    Source = new Uri(string.Format("Layouts/{0}.xaml", name), UriKind.Relative)
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static ResourceDictionary LoadUser(string name)
        {
            string _path = Path.Combine(UserLayoutPath, name + ".xaml");

            try
            {
                if (!File.Exists(_path))
                {
                    return null;
                }

                using (FileStream _stream = File.OpenRead(_path))
                {
                    ResourceDictionary _dictionary = XamlReader.Load(_stream) as ResourceDictionary;

                    // Anything without the template contributes nothing and would silently blank the
                    // metric rows, so treat it as invalid rather than applying it.
                    return _dictionary != null && _dictionary.Contains("MetricTemplate") ? _dictionary : null;
                }
            }
            catch (Exception)
            {
                // Hand-written XAML is expected to be broken sometimes; the caller falls back.
                return null;
            }
        }
    }
}
