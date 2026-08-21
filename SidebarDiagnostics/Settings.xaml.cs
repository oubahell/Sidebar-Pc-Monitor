using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using SidebarDiagnostics.Models;
using SidebarDiagnostics.Windows;
using SidebarDiagnostics.Style;

namespace SidebarDiagnostics
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : FlatWindow
    {
        public Settings(Sidebar sidebar)
        {
            InitializeComponent();

            DataContext = Model = new SettingsModel(sidebar);

            Owner = sidebar;
            ShowDialog();
        }

        private async Task Save(bool finalize)
        {
            Model.Save();

            App.ApplyTheme(Model.Theme);
            Framework.LayoutManager.Apply(Model.Layout);

            await App.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(async () =>
            {
                Sidebar _sidebar = App.Current.Sidebar;

                if (_sidebar == null)
                {
                    return;
                }

                await _sidebar.Reset(finalize);
            }));
        }
        
        private void NumberBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (new Regex("[^0-9.-]+").IsMatch(e.Text))
            {
                e.Handled = true;
            }
        }

        private void OffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.NewValue != 0d)
            {
                ShowTrayIconCheckbox.IsChecked = true;
            }
        }

        private void ClickThroughCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            ShowTrayIconCheckbox.IsChecked = true;
        }

        private void ShowTrayIconCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            XOffsetSlider.Value = 0d;
            YOffsetSlider.Value = 0d;

            ClickThroughCheckbox.IsChecked = false;
        }
        
        private void BindButton_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_hotkey != null)
            {
                EndBind();
            }

            (sender as ToggleButton).IsChecked = false;
        }

        private void BindToggle_Click(object sender, RoutedEventArgs e)
        {
            _keybinder = (ToggleButton)sender;

            if (_keybinder.IsChecked == true)
            {
                BeginBind(Hotkey.KeyAction.Toggle);
            }
            else
            {
                EndBind();
            }
        }

        private void BindShow_Click(object sender, RoutedEventArgs e)
        {
            _keybinder = (ToggleButton)sender;

            if (_keybinder.IsChecked == true)
            {
                BeginBind(Hotkey.KeyAction.Show);
            }
            else
            {
                EndBind();
            }
        }

        private void BindHide_Click(object sender, RoutedEventArgs e)
        {
            _keybinder = (ToggleButton)sender;

            if (_keybinder.IsChecked == true)
            {
                BeginBind(Hotkey.KeyAction.Hide);
            }
            else
            {
                EndBind();
            }
        }

        private void BindReload_Click(object sender, RoutedEventArgs e)
        {
            _keybinder = (ToggleButton)sender;

            if (_keybinder.IsChecked == true)
            {
                BeginBind(Hotkey.KeyAction.Reload);
            }
            else
            {
                EndBind();
            }
        }

        private void BindClose_Click(object sender, RoutedEventArgs e)
        {
            _keybinder = (ToggleButton)sender;

            if (_keybinder.IsChecked == true)
            {
                BeginBind(Hotkey.KeyAction.Close);
            }
            else
            {
                EndBind();
            }
        }

        private void BindCycleEdge_Click(object sender, RoutedEventArgs e)
        {
            _keybinder = (ToggleButton)sender;

            if (_keybinder.IsChecked == true)
            {
                BeginBind(Hotkey.KeyAction.CycleEdge);
            }
            else
            {
                EndBind();
            }
        }

        private void BindCycleScreen_Click(object sender, RoutedEventArgs e)
        {
            _keybinder = (ToggleButton)sender;

            if (_keybinder.IsChecked == true)
            {
                BeginBind(Hotkey.KeyAction.CycleScreen);
            }
            else
            {
                EndBind();
            }
        }

        private void BindReserveSpace_Click(object sender, RoutedEventArgs e)
        {
            _keybinder = (ToggleButton)sender;

            if (_keybinder.IsChecked == true)
            {
                BeginBind(Hotkey.KeyAction.ReserveSpace);
            }
            else
            {
                EndBind();
            }
        }

        private void BeginBind(Hotkey.KeyAction action)
        {
            _hotkey = new Hotkey();
            _hotkey.Action = action;
            _hotkey.WinKey = Key.Escape;

            KeyDown += Window_KeyDown;
        }

        private void EndBind()
        {
            KeyDown -= Window_KeyDown;

            Hotkey.KeyAction _action = _hotkey.Action;

            if (_hotkey.WinKey == Key.Escape)
            {
                _hotkey = null;
            }

            switch (_action)
            {
                case Hotkey.KeyAction.Toggle:
                    Model.ToggleKey = _hotkey;
                    break;

                case Hotkey.KeyAction.Show:
                    Model.ShowKey = _hotkey;
                    break;

                case Hotkey.KeyAction.Hide:
                    Model.HideKey = _hotkey;
                    break;

                case Hotkey.KeyAction.Reload:
                    Model.ReloadKey = _hotkey;
                    break;

                case Hotkey.KeyAction.Close:
                    Model.CloseKey = _hotkey;
                    break;

                case Hotkey.KeyAction.CycleEdge:
                    Model.CycleEdgeKey = _hotkey;
                    break;

                case Hotkey.KeyAction.CycleScreen:
                    Model.CycleScreenKey = _hotkey;
                    break;

                case Hotkey.KeyAction.ReserveSpace:
                    Model.ReserveSpaceKey = _hotkey;
                    break;
            }

            _keybinder.IsChecked = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            Key _key = e.Key == Key.System ? e.SystemKey : e.Key;

            if (new Key[] { Key.LeftAlt, Key.RightAlt, Key.LeftCtrl, Key.RightCtrl, Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin }.Contains(_key))
            {
                return;
            }

            if ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                _hotkey.CtrlMod = true;
            }

            if ((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                _hotkey.ShiftMod = true;
            }

            if ((e.KeyboardDevice.Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
            {
                _hotkey.WinMod = true;
            }

            if ((e.KeyboardDevice.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                _hotkey.AltMod = true;
            }

            _hotkey.WinKey = _key;

            EndBind();

            e.Handled = true;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await Save(true);

            Close();
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            await Save(false);
        }

        private void ResetColorsButton_Click(object sender, RoutedEventArgs e)
        {
            Model.ResetColorsToTheme();
        }

        private async void ResetDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult _confirm = MessageBox.Show(
                Framework.Resources.SettingsResetAllConfirm,
                Framework.Resources.SettingsResetAll,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No,
                MessageBoxOptions.DefaultDesktopOnly);

            if (_confirm != MessageBoxResult.Yes)
            {
                return;
            }

            Framework.Settings.Instance.Reset();
            Framework.Settings.Instance.Save();

            App.ApplyTheme(Framework.Settings.Instance.Theme);

            // Rebuild the view model so the dialog reflects the restored values, then run the normal
            // save/apply path to push them out to the sidebar.
            Sidebar _sidebar = App.Current.Sidebar;

            if (_sidebar != null)
            {
                DataContext = Model = new SettingsModel(_sidebar);
            }

            await Save(false);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Model.IsChanged)
            {
                Sidebar _sidebar = App.Current.Sidebar;

                if (_sidebar != null)
                {
                    DataContext = Model = new SettingsModel(_sidebar);
                    return;
                }
            }

            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Hotkey.Disable();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            DataContext = null;
            Model = null;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Hotkey.Enable();
        }

        public SettingsModel Model { get; private set; }

        private Hotkey _hotkey { get; set; }

        private ToggleButton _keybinder { get; set; }
    }
}
