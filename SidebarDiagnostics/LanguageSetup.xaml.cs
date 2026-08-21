using System.Windows;
using SidebarDiagnostics.Style;
using SidebarDiagnostics.Utilities;

namespace SidebarDiagnostics
{
    /// <summary>
    /// First-run language picker.
    /// </summary>
    /// <remarks>
    /// Shown once, before the setup wizard, so everything the user sees afterwards is already in
    /// the language they chose. It runs before Culture.SetCurrent, which is the only chance to
    /// call FrameworkElement.LanguageProperty.OverrideMetadata - that can be done once per run.
    ///
    /// The window has no close button and no cancel: there is always a valid answer preselected
    /// from the machine's own language, so dismissing it would only mean picking that one anyway.
    /// </remarks>
    public partial class LanguageSetup : FlatWindow
    {
        public LanguageSetup()
        {
            InitializeComponent();

            LanguageBox.ItemsSource = Culture.GetNative();
            LanguageBox.SelectedValue = Culture.GetNativeDefault();
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            string _culture = LanguageBox.SelectedValue as string;

            if (_culture != null)
            {
                Framework.Settings.Instance.Culture = _culture;
                Framework.Settings.Instance.Save();
            }

            Close();
        }
    }
}
