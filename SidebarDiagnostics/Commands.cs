using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace SidebarDiagnostics.Commands
{
    public class ActivateCommand : ICommand
    {
        public void Execute(object parameter)
        {
            Sidebar _sidebar = App.Current.Sidebar;

            if (_sidebar == null)
            {
                return;
            }
            
            _sidebar.Activate();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }

    /// <summary>
    /// Opens the settings window, or brings it forward if it is already open.
    /// </summary>
    /// <remarks>
    /// Bound to a double-click on the tray icon. A single click shows the sidebar, which is the
    /// right thing when the sidebar is what you lost; but the tray icon is also where people go
    /// looking for settings, and double-clicking a tray icon to open an app's window is a habit
    /// Windows taught everyone.
    /// </remarks>
    public class SettingsCommand : ICommand
    {
        public void Execute(object parameter)
        {
            App.Current.OpenSettings();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
