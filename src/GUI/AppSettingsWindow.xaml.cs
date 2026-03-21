using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;
using DrawingColor = System.Drawing.Color;

namespace Switchie
{
    public partial class AppSettingsWindow : System.Windows.Window
    {
        private readonly AppSettings _defaults = new AppSettings();
        private DrawingColor _backgroundColor;
        private DrawingColor _desktopBorderColor;
        private DrawingColor _activeDesktopBorderColor;
        private DrawingColor _windowColor;
        private DrawingColor _activeWindowColor;
        private DrawingColor _windowBorderColor;
        private DrawingColor _activeWindowBorderColor;

        public AppSettings Settings { get; private set; }

        public AppSettingsWindow(AppSettings settings)
        {
            InitializeComponent();

            Settings = Clone(settings);

            cmbRenderMode.Items.Add("Windows");
            cmbRenderMode.Items.Add("Icons");

            cmbDesktopBorderStyle.Items.Add("Box");
            cmbDesktopBorderStyle.Items.Add("Underline");

            cmbRenderMode.SelectedIndex = Math.Max(0, Math.Min(1, Settings.RenderMode));
            cmbDesktopBorderStyle.SelectedIndex = Math.Max(0, Math.Min(1, Settings.DesktopBorderStyle));
            if (sldBackgroundOpacity != null)
            {
                sldBackgroundOpacity.Value = Math.Max(0.25, Math.Min(1.0, Settings.BackgroundOpacity));
            }
            if (txtBackgroundOpacityValue != null && sldBackgroundOpacity != null)
            {
                txtBackgroundOpacityValue.Text = sldBackgroundOpacity.Value.ToString("0.00", CultureInfo.InvariantCulture);
            }
            txtPagerHeight.Text = Settings.PagerHeight.ToString(CultureInfo.InvariantCulture);
            txtPaddingSize.Text = Settings.PaddingSize.ToString(CultureInfo.InvariantCulture);
            txtIconPaddingX.Text = Settings.IconPaddingX.ToString(CultureInfo.InvariantCulture);
            txtIconPaddingY.Text = Settings.IconPaddingY.ToString(CultureInfo.InvariantCulture);
            chkShowAppInTaskbar.IsChecked = Settings.ShowAppInTaskbar;
            
            _backgroundColor = Settings.BackgroundColor;

            _desktopBorderColor = Settings.DesktopBorderColor;
            _activeDesktopBorderColor = Settings.ActiveDesktopBorderColor;
            _windowColor = Settings.WindowColor;
            _activeWindowColor = Settings.ActiveWindowColor;
            _windowBorderColor = Settings.WindowBorderColor;
            _activeWindowBorderColor = Settings.ActiveWindowBorderColor;

            txtPrimaryDelay.Text = Settings.PrimaryUpdateDelay.ToString(CultureInfo.InvariantCulture);
            txtSecondaryDelay.Text = Settings.SecondaryUpdateDelay.ToString(CultureInfo.InvariantCulture);

            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void ChooseBackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            _backgroundColor = PickColor(_backgroundColor);
            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void ChooseDesktopColor_Click(object sender, RoutedEventArgs e)
        {
            _desktopBorderColor = PickColor(_desktopBorderColor);
            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void ChooseActiveDesktopBorderColor_Click(object sender, RoutedEventArgs e)
        {
            _activeDesktopBorderColor = PickColor(_activeDesktopBorderColor);
            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void ChooseWindowColor_Click(object sender, RoutedEventArgs e)
        {
            _windowColor = PickColor(_windowColor);
            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void ChooseActiveWindowColor_Click(object sender, RoutedEventArgs e)
        {
            _activeWindowColor = PickColor(_activeWindowColor);
            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void ChooseWindowBorderColor_Click(object sender, RoutedEventArgs e)
        {
            _windowBorderColor = PickColor(_windowBorderColor);
            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void ChooseActiveWindowBorderColor_Click(object sender, RoutedEventArgs e)
        {
            _activeWindowBorderColor = PickColor(_activeWindowBorderColor);
            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void RenderModeChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateRevertButtons();
        }

        private void TextInputChanged(object sender, TextChangedEventArgs e)
        {
            UpdateRevertButtons();
        }

        private void BooleanInputChanged(object sender, RoutedEventArgs e)
        {
            UpdateRevertButtons();
        }

        private void BackgroundOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtBackgroundOpacityValue != null)
            {
                txtBackgroundOpacityValue.Text = sldBackgroundOpacity.Value.ToString("0.00", CultureInfo.InvariantCulture);
            }

            if (!IsLoaded)
            {
                return;
            }

            UpdateRevertButtons();
        }

        private void RevertAll_Click(object sender, RoutedEventArgs e)
        {
            cmbRenderMode.SelectedIndex = _defaults.RenderMode <= 0 ? 0 : 1;
            cmbDesktopBorderStyle.SelectedIndex = _defaults.DesktopBorderStyle <= 0 ? 0 : 1;
            if (sldBackgroundOpacity != null)
            {
                sldBackgroundOpacity.Value = Math.Max(0.25, Math.Min(1.0, _defaults.BackgroundOpacity));
            }
            txtPagerHeight.Text = _defaults.PagerHeight.ToString(CultureInfo.InvariantCulture);
            txtPaddingSize.Text = _defaults.PaddingSize.ToString(CultureInfo.InvariantCulture);
            txtIconPaddingX.Text = _defaults.IconPaddingX.ToString(CultureInfo.InvariantCulture);
            txtIconPaddingY.Text = _defaults.IconPaddingY.ToString(CultureInfo.InvariantCulture);
            chkShowAppInTaskbar.IsChecked = _defaults.ShowAppInTaskbar;

            _backgroundColor = _defaults.BackgroundColor;

            _desktopBorderColor = _defaults.DesktopBorderColor;
            _activeDesktopBorderColor = _defaults.ActiveDesktopBorderColor;
            _windowColor = _defaults.WindowColor;
            _activeWindowColor = _defaults.ActiveWindowColor;
            _windowBorderColor = _defaults.WindowBorderColor;
            _activeWindowBorderColor = _defaults.ActiveWindowBorderColor;

            txtPrimaryDelay.Text = _defaults.PrimaryUpdateDelay.ToString(CultureInfo.InvariantCulture);
            txtSecondaryDelay.Text = _defaults.SecondaryUpdateDelay.ToString(CultureInfo.InvariantCulture);

            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void RevertRenderMode_Click(object sender, RoutedEventArgs e)
        {
            cmbRenderMode.SelectedIndex = _defaults.RenderMode <= 0 ? 0 : 1;
            UpdateRevertButtons();
        }

        private void RevertPagerHeight_Click(object sender, RoutedEventArgs e)
        {
            txtPagerHeight.Text = _defaults.PagerHeight.ToString(CultureInfo.InvariantCulture);
            UpdateRevertButtons();
        }

        private void RevertDesktopBorderStyle_Click(object sender, RoutedEventArgs e)
        {
            cmbDesktopBorderStyle.SelectedIndex = _defaults.DesktopBorderStyle <= 0 ? 0 : 1;
            UpdateRevertButtons();
        }

        private void RevertBackgroundOpacity_Click(object sender, RoutedEventArgs e)
        {
            if (sldBackgroundOpacity != null)
            {
                sldBackgroundOpacity.Value = Math.Max(0.25, Math.Min(1.0, _defaults.BackgroundOpacity));
            }
            UpdateRevertButtons();
        }

        private void RevertPaddingSize_Click(object sender, RoutedEventArgs e)
        {
            txtPaddingSize.Text = _defaults.PaddingSize.ToString(CultureInfo.InvariantCulture);
            UpdateRevertButtons();
        }

        private void RevertIconPaddingX_Click(object sender, RoutedEventArgs e)
        {
            txtIconPaddingX.Text = _defaults.IconPaddingX.ToString(CultureInfo.InvariantCulture);
            UpdateRevertButtons();
        }

        private void RevertIconPaddingY_Click(object sender, RoutedEventArgs e)
        {
            txtIconPaddingY.Text = _defaults.IconPaddingY.ToString(CultureInfo.InvariantCulture);
            UpdateRevertButtons();
        }

        private void RevertShowAppInTaskbar_Click(object sender, RoutedEventArgs e)
        {
            chkShowAppInTaskbar.IsChecked = _defaults.ShowAppInTaskbar;
            UpdateRevertButtons();
        }

        private void RevertBackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            _backgroundColor = _defaults.BackgroundColor;
            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void RevertDesktopColor_Click(object sender, RoutedEventArgs e)
        {
            _desktopBorderColor = _defaults.DesktopBorderColor;
            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void RevertActiveDesktopBorderColor_Click(object sender, RoutedEventArgs e)
        {
            _activeDesktopBorderColor = _defaults.ActiveDesktopBorderColor;
            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void RevertWindowColor_Click(object sender, RoutedEventArgs e)
        {
            _windowColor = _defaults.WindowColor;
            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void RevertActiveWindowColor_Click(object sender, RoutedEventArgs e)
        {
            _activeWindowColor = _defaults.ActiveWindowColor;
            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void RevertWindowBorderColor_Click(object sender, RoutedEventArgs e)
        {
            _windowBorderColor = _defaults.WindowBorderColor;
            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void RevertActiveWindowBorderColor_Click(object sender, RoutedEventArgs e)
        {
            _activeWindowBorderColor = _defaults.ActiveWindowBorderColor;
            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void RevertPrimaryDelay_Click(object sender, RoutedEventArgs e)
        {
            txtPrimaryDelay.Text = _defaults.PrimaryUpdateDelay.ToString(CultureInfo.InvariantCulture);
            UpdateRevertButtons();
        }

        private void RevertSecondaryDelay_Click(object sender, RoutedEventArgs e)
        {
            txtSecondaryDelay.Text = _defaults.SecondaryUpdateDelay.ToString(CultureInfo.InvariantCulture);
            UpdateRevertButtons();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParsePositiveInt(txtPagerHeight.Text, 1, out int pagerHeight))
            {
                ShowValidationError("Pager Height must be a positive number.");
                return;
            }

            if (!TryParsePositiveInt(txtPaddingSize.Text, 0, out int paddingSize))
            {
                ShowValidationError("Padding Size must be 0 or greater.");
                return;
            }

            if (!TryParseInt(txtIconPaddingX.Text, out int iconPaddingX))
            {
                ShowValidationError("Icon Padding X must be a valid whole number.");
                return;
            }

            if (!TryParseInt(txtIconPaddingY.Text, out int iconPaddingY))
            {
                ShowValidationError("Icon Padding Y must be a valid whole number.");
                return;
            }

            if (!TryParsePositiveInt(txtPrimaryDelay.Text, 1, out int primaryDelay))
            {
                ShowValidationError("Primary Update Delay must be at least 1 ms.");
                return;
            }

            if (!TryParsePositiveInt(txtSecondaryDelay.Text, 100, out int secondaryDelay))
            {
                ShowValidationError("Secondary Update Delay must be at least 100 ms.");
                return;
            }

            Settings = new AppSettings
            {
                RenderMode = cmbRenderMode.SelectedIndex <= 0 ? 0 : 1,
                DesktopBorderStyle = cmbDesktopBorderStyle.SelectedIndex <= 0 ? 0 : 1,
                BackgroundOpacity = sldBackgroundOpacity == null
                    ? Math.Max(0.25, Math.Min(1.0, _defaults.BackgroundOpacity))
                    : Math.Max(0.25, Math.Min(1.0, sldBackgroundOpacity.Value)),
                PagerHeight = pagerHeight,
                PaddingSize = paddingSize,
                IconPaddingX = iconPaddingX,
                IconPaddingY = iconPaddingY,
                ShowAppInTaskbar = chkShowAppInTaskbar.IsChecked == true,
                PrimaryUpdateDelay = primaryDelay,
                SecondaryUpdateDelay = secondaryDelay,
                BackgroundColor = _backgroundColor,
                DesktopBorderColor = _desktopBorderColor,
                ActiveDesktopBorderColor = _activeDesktopBorderColor,
                WindowColor = _windowColor,
                ActiveWindowColor = _activeWindowColor,
                WindowBorderColor = _windowBorderColor,
                ActiveWindowBorderColor = _activeWindowBorderColor
            };

            DialogResult = true;
            Close();
        }

        private static bool TryParsePositiveInt(string input, int min, out int value)
        {
            return int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= min;
        }

        private static bool TryParseInt(string input, out int value)
        {
            return int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static AppSettings Clone(AppSettings source)
        {
            return new AppSettings
            {
                RenderMode = source.RenderMode,
                DesktopBorderStyle = source.DesktopBorderStyle,
                BackgroundOpacity = source.BackgroundOpacity,
                PagerHeight = source.PagerHeight,
                PaddingSize = source.PaddingSize,
                IconPaddingX = source.IconPaddingX,
                IconPaddingY = source.IconPaddingY,
                ShowAppInTaskbar = source.ShowAppInTaskbar,
                PrimaryUpdateDelay = source.PrimaryUpdateDelay,
                SecondaryUpdateDelay = source.SecondaryUpdateDelay,
                DesktopBorderColor = source.DesktopBorderColor,
                BackgroundColor = source.BackgroundColor,
                ActiveDesktopBorderColor = source.ActiveDesktopBorderColor,
                WindowColor = source.WindowColor,
                ActiveWindowColor = source.ActiveWindowColor,
                WindowBorderColor = source.WindowBorderColor,
                ActiveWindowBorderColor = source.ActiveWindowBorderColor
            };
        }

        private static DrawingColor PickColor(DrawingColor initial)
        {
            using (var dlg = new Forms.ColorDialog())
            {
                dlg.FullOpen = true;
                dlg.Color = initial;
                return dlg.ShowDialog() == Forms.DialogResult.OK ? dlg.Color : initial;
            }
        }

        private void RefreshColorPreviews()
        {
            previewBackgroundColor.Background = ToBrush(_backgroundColor);
            previewDesktopColor.Background = ToBrush(_desktopBorderColor);
            previewActiveDesktopBorderColor.Background = ToBrush(_activeDesktopBorderColor);
            previewWindowColor.Background = ToBrush(_windowColor);
            previewActiveWindowColor.Background = ToBrush(_activeWindowColor);
            previewWindowBorderColor.Background = ToBrush(_windowBorderColor);
            previewActiveWindowBorderColor.Background = ToBrush(_activeWindowBorderColor);
        }

        private static System.Windows.Media.SolidColorBrush ToBrush(DrawingColor color)
        {
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        private void UpdateRevertButtons()
        {
            if (btnRevertBackgroundOpacity == null || sldBackgroundOpacity == null)
            {
                return;
            }

            btnRevertRenderMode.Visibility = IsDifferent(cmbRenderMode.SelectedIndex, _defaults.RenderMode) ? Visibility.Visible : Visibility.Hidden;
            btnRevertDesktopBorderStyle.Visibility = IsDifferent(cmbDesktopBorderStyle.SelectedIndex, _defaults.DesktopBorderStyle) ? Visibility.Visible : Visibility.Hidden;
            btnRevertBackgroundOpacity.Visibility = IsDifferent(sldBackgroundOpacity.Value, _defaults.BackgroundOpacity) ? Visibility.Visible : Visibility.Hidden;
            btnRevertPagerHeight.Visibility = IsDifferent(txtPagerHeight.Text, _defaults.PagerHeight) ? Visibility.Visible : Visibility.Hidden;
            btnRevertPaddingSize.Visibility = IsDifferent(txtPaddingSize.Text, _defaults.PaddingSize) ? Visibility.Visible : Visibility.Hidden;
            btnRevertIconPaddingX.Visibility = IsDifferent(txtIconPaddingX.Text, _defaults.IconPaddingX) ? Visibility.Visible : Visibility.Hidden;
            btnRevertIconPaddingY.Visibility = IsDifferent(txtIconPaddingY.Text, _defaults.IconPaddingY) ? Visibility.Visible : Visibility.Hidden;
            btnRevertShowAppInTaskbar.Visibility = (chkShowAppInTaskbar.IsChecked == true) != _defaults.ShowAppInTaskbar ? Visibility.Visible : Visibility.Hidden;
            btnRevertPrimaryDelay.Visibility = IsDifferent(txtPrimaryDelay.Text, _defaults.PrimaryUpdateDelay) ? Visibility.Visible : Visibility.Hidden;
            btnRevertSecondaryDelay.Visibility = IsDifferent(txtSecondaryDelay.Text, _defaults.SecondaryUpdateDelay) ? Visibility.Visible : Visibility.Hidden;

            btnRevertBackgroundColor.Visibility = _backgroundColor.ToArgb() != _defaults.BackgroundColor.ToArgb() ? Visibility.Visible : Visibility.Hidden;
            btnRevertDesktopColor.Visibility = _desktopBorderColor.ToArgb() != _defaults.DesktopBorderColor.ToArgb() ? Visibility.Visible : Visibility.Hidden;
            btnRevertActiveDesktopBorderColor.Visibility = _activeDesktopBorderColor.ToArgb() != _defaults.ActiveDesktopBorderColor.ToArgb() ? Visibility.Visible : Visibility.Hidden;
            btnRevertWindowColor.Visibility = _windowColor.ToArgb() != _defaults.WindowColor.ToArgb() ? Visibility.Visible : Visibility.Hidden;
            btnRevertActiveWindowColor.Visibility = _activeWindowColor.ToArgb() != _defaults.ActiveWindowColor.ToArgb() ? Visibility.Visible : Visibility.Hidden;
            btnRevertWindowBorderColor.Visibility = _windowBorderColor.ToArgb() != _defaults.WindowBorderColor.ToArgb() ? Visibility.Visible : Visibility.Hidden;
            btnRevertActiveWindowBorderColor.Visibility = _activeWindowBorderColor.ToArgb() != _defaults.ActiveWindowBorderColor.ToArgb() ? Visibility.Visible : Visibility.Hidden;
        }

        private static bool IsDifferent(string input, int defaultValue)
        {
            return !int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) || parsed != defaultValue;
        }

        private static bool IsDifferent(int input, int defaultValue)
        {
            return input != defaultValue;
        }

        private static bool IsDifferent(double input, double defaultValue)
        {
            return Math.Abs(input - defaultValue) > 0.0001;
        }

        private void ShowValidationError(string message)
        {
            MessageBox.Show(this, message, "Invalid Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

