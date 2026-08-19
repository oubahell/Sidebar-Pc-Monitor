using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace SidebarDiagnostics.Controls
{
    /// <summary>
    /// A small colour picker that replaces Xceed's ColorPicker.
    ///
    /// Xceed's control paints its drop-down (and the swatch/hex row inside its button) from a
    /// hardcoded light palette in its own template, which ignored implicit styles, ButtonStyle and
    /// SystemColors overrides alike -- leaving a white popup with unreadable text on the dark themes.
    /// This keeps the same contract (a "#RRGGBB" string, two-way bound) but is fully themeable.
    /// </summary>
    public partial class ColorPickerBox : UserControl
    {
        public ColorPickerBox()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
            "SelectedColor",
            typeof(string),
            typeof(ColorPickerBox),
            new FrameworkPropertyMetadata("#000000", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));

        /// <summary>The selected colour as a "#RRGGBB" string.</summary>
        public string SelectedColor
        {
            get { return (string)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        private static readonly DependencyPropertyKey SwatchBrushPropertyKey = DependencyProperty.RegisterReadOnly(
            "SwatchBrush",
            typeof(Brush),
            typeof(ColorPickerBox),
            new PropertyMetadata(Brushes.Black));

        public static readonly DependencyProperty SwatchBrushProperty = SwatchBrushPropertyKey.DependencyProperty;

        /// <summary>The brush shown on the button face, kept in sync with <see cref="SelectedColor"/>.</summary>
        public Brush SwatchBrush
        {
            get { return (Brush)GetValue(SwatchBrushProperty); }
        }

        /// <summary>
        /// A curated palette: neutrals for backgrounds and text, then the accent hues used by the
        /// built-in themes, so the common choices are one click away.
        /// </summary>
        public IEnumerable<string> Presets { get; } = new string[]
        {
            "#000000", "#0D0D14", "#12141A", "#1E1E2E", "#202020", "#2C2C2C", "#3A3A3A", "#555555",
            "#808080", "#BFBFBF", "#E4E4E7", "#F3F3F3", "#FFFFFF", "#00F0FF", "#60CDFF", "#3498DB",
            "#6366F1", "#7B61FF", "#2ECC71", "#F1C40F", "#FF8C00", "#FF6B6B", "#FF2D75", "#E74C3C"
        };

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ColorPickerBox)d).SyncFromSelectedColor();
        }

        private void SyncFromSelectedColor()
        {
            string _hex = Normalize(SelectedColor);

            SetValue(SwatchBrushPropertyKey, new SolidColorBrush(Parse(_hex)));

            if (PART_Hex != null && !PART_Hex.IsFocused)
            {
                PART_Hex.Text = _hex;
            }
        }

        private void Preset_Click(object sender, RoutedEventArgs e)
        {
            string _hex = ((FrameworkElement)sender).Tag as string;

            if (_hex != null)
            {
                SelectedColor = _hex;
            }

            PART_Toggle.IsChecked = false;
        }

        private void Hex_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommitHex();
                PART_Toggle.IsChecked = false;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                PART_Hex.Text = Normalize(SelectedColor);
                PART_Toggle.IsChecked = false;
                e.Handled = true;
            }
        }

        private void Hex_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitHex();
        }

        private void CommitHex()
        {
            string _text = (PART_Hex.Text ?? string.Empty).Trim();

            if (!_text.StartsWith("#", StringComparison.Ordinal))
            {
                _text = "#" + _text;
            }

            // Ignore anything that isn't a complete #RRGGBB value and restore the previous colour,
            // so a half-typed value can never be written back into settings.
            if (Regex.IsMatch(_text, "^#[0-9a-fA-F]{6}$"))
            {
                SelectedColor = _text.ToUpperInvariant();
            }
            else
            {
                PART_Hex.Text = Normalize(SelectedColor);
            }
        }

        private static string Normalize(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                return "#000000";
            }

            hex = hex.Trim();

            if (!hex.StartsWith("#", StringComparison.Ordinal))
            {
                hex = "#" + hex;
            }

            // Settings written by older builds (and by WPF's Color converter) can carry an alpha
            // byte; drop it so the field always shows a plain #RRGGBB value.
            if (hex.Length == 9)
            {
                hex = "#" + hex.Substring(3);
            }

            return Regex.IsMatch(hex, "^#[0-9a-fA-F]{6}$") ? hex.ToUpperInvariant() : "#000000";
        }

        private static Color Parse(string hex)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch (FormatException)
            {
                return Colors.Black;
            }
        }
    }

    /// <summary>Converts a "#RRGGBB" preset string into a brush for the swatch buttons.</summary>
    public class HexToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string _hex = value as string;

            if (string.IsNullOrEmpty(_hex))
            {
                return Brushes.Transparent;
            }

            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(_hex));
            }
            catch (FormatException)
            {
                return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
