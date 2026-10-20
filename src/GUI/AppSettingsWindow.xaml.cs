using System;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Controls;
using DrawingColor = System.Drawing.Color;
using Forms = System.Windows.Forms;

namespace Switchie
{
    public partial class AppSettingsWindow : System.Windows.Window
    {
        private readonly AppSettings _defaults = new AppSettings();
        private DrawingColor _desktopBackgroundColor;
        private DrawingColor _desktopBorderColor;
        private DrawingColor _activeDesktopBorderColor;
        private DrawingColor _windowBackgroundColor;
        private DrawingColor _activeWindowBackgroundColor;
        private DrawingColor _windowBorderColor;
        private DrawingColor _activeWindowBorderColor;

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
                DesktopBackgroundColor = source.DesktopBackgroundColor,
                ActiveDesktopBorderColor = source.ActiveDesktopBorderColor,
                WindowBackgroundColor = source.WindowBackgroundColor,
                ActiveWindowBackgroundColor = source.ActiveWindowBackgroundColor,
                WindowBorderColor = source.WindowBorderColor,
                ActiveWindowBorderColor = source.ActiveWindowBorderColor
            };
        }

        // TODO: Currently only one global settings but we need a currenty copy for the preview
        public AppSettings Settings { get; private set; }
        private AppSettings _settingsBefore { get; set; }

        public AppSettingsWindow(AppSettings settings)
        {
            InitializeComponent();

            _settingsBefore = Clone(settings);
            Settings = Clone(settings); // Now without copying

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

            _desktopBackgroundColor = Settings.DesktopBackgroundColor;

            _desktopBorderColor = Settings.DesktopBorderColor;
            _activeDesktopBorderColor = Settings.ActiveDesktopBorderColor;
            _windowBackgroundColor = Settings.WindowBackgroundColor;
            _activeWindowBackgroundColor = Settings.ActiveWindowBackgroundColor;
            _windowBorderColor = Settings.WindowBorderColor;
            _activeWindowBorderColor = Settings.ActiveWindowBorderColor;

            txtPrimaryDelay.Text = Settings.PrimaryUpdateDelay.ToString(CultureInfo.InvariantCulture);
            txtSecondaryDelay.Text = Settings.SecondaryUpdateDelay.ToString(CultureInfo.InvariantCulture);

            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void ResetToDefaults_Click(object sender, RoutedEventArgs e)
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

            _desktopBackgroundColor = _defaults.DesktopBackgroundColor;

            _desktopBorderColor = _defaults.DesktopBorderColor;
            _activeDesktopBorderColor = _defaults.ActiveDesktopBorderColor;
            _windowBackgroundColor = _defaults.WindowBackgroundColor;
            _activeWindowBackgroundColor = _defaults.ActiveWindowBackgroundColor;
            _windowBorderColor = _defaults.WindowBorderColor;
            _activeWindowBorderColor = _defaults.ActiveWindowBorderColor;

            txtPrimaryDelay.Text = _defaults.PrimaryUpdateDelay.ToString(CultureInfo.InvariantCulture);
            txtSecondaryDelay.Text = _defaults.SecondaryUpdateDelay.ToString(CultureInfo.InvariantCulture);

            RefreshColorPreviews();
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
                DesktopBackgroundColor = _desktopBackgroundColor,
                DesktopBorderColor = _desktopBorderColor,
                ActiveDesktopBorderColor = _activeDesktopBorderColor,
                WindowBackgroundColor = _windowBackgroundColor,
                ActiveWindowBackgroundColor = _activeWindowBackgroundColor,
                WindowBorderColor = _windowBorderColor,
                ActiveWindowBorderColor = _activeWindowBorderColor
            };

            DialogResult = true;
            Close();
        }

        private void ShowValidationError(string message)
        {
            MessageBox.Show(this, message, "Invalid Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void UpdateRevertButtons()
        {
            if (btnRevertBackgroundOpacity == null || sldBackgroundOpacity == null)
            {
                return;
            }

            btnRevertRenderMode.Visibility = IsDifferent(cmbRenderMode.SelectedIndex, _settingsBefore.RenderMode) ? Visibility.Visible : Visibility.Hidden;
            btnRevertDesktopBorderStyle.Visibility = IsDifferent(cmbDesktopBorderStyle.SelectedIndex, _settingsBefore.DesktopBorderStyle) ? Visibility.Visible : Visibility.Hidden;
            btnRevertBackgroundOpacity.Visibility = IsDifferent(sldBackgroundOpacity.Value, _settingsBefore.BackgroundOpacity) ? Visibility.Visible : Visibility.Hidden;
            btnRevertPagerHeight.Visibility = IsDifferent(txtPagerHeight.Text, _settingsBefore.PagerHeight) ? Visibility.Visible : Visibility.Hidden;
            btnRevertPaddingSize.Visibility = IsDifferent(txtPaddingSize.Text, _settingsBefore.PaddingSize) ? Visibility.Visible : Visibility.Hidden;
            btnRevertIconPaddingX.Visibility = IsDifferent(txtIconPaddingX.Text, _settingsBefore.IconPaddingX) ? Visibility.Visible : Visibility.Hidden;
            btnRevertIconPaddingY.Visibility = IsDifferent(txtIconPaddingY.Text, _settingsBefore.IconPaddingY) ? Visibility.Visible : Visibility.Hidden;
            btnRevertShowAppInTaskbar.Visibility = (chkShowAppInTaskbar.IsChecked == true) != _settingsBefore.ShowAppInTaskbar ? Visibility.Visible : Visibility.Hidden;
            btnRevertPrimaryDelay.Visibility = IsDifferent(txtPrimaryDelay.Text, _settingsBefore.PrimaryUpdateDelay) ? Visibility.Visible : Visibility.Hidden;
            btnRevertSecondaryDelay.Visibility = IsDifferent(txtSecondaryDelay.Text, _settingsBefore.SecondaryUpdateDelay) ? Visibility.Visible : Visibility.Hidden;

            btnRevertBackgroundColor.Visibility = _desktopBackgroundColor.ToArgb() != _settingsBefore.DesktopBackgroundColor.ToArgb() ? Visibility.Visible : Visibility.Hidden;
            btnRevertDesktopColor.Visibility = _desktopBorderColor.ToArgb() != _settingsBefore.DesktopBorderColor.ToArgb() ? Visibility.Visible : Visibility.Hidden;
            btnRevertActiveDesktopBorderColor.Visibility = _activeDesktopBorderColor.ToArgb() != _settingsBefore.ActiveDesktopBorderColor.ToArgb() ? Visibility.Visible : Visibility.Hidden;
            btnRevertWindowColor.Visibility = _windowBackgroundColor.ToArgb() != _settingsBefore.WindowBackgroundColor.ToArgb() ? Visibility.Visible : Visibility.Hidden;
            btnRevertActiveWindowColor.Visibility = _activeWindowBackgroundColor.ToArgb() != _settingsBefore.ActiveWindowBackgroundColor.ToArgb() ? Visibility.Visible : Visibility.Hidden;
            btnRevertWindowBorderColor.Visibility = _windowBorderColor.ToArgb() != _settingsBefore.WindowBorderColor.ToArgb() ? Visibility.Visible : Visibility.Hidden;
            btnRevertActiveWindowBorderColor.Visibility = _activeWindowBorderColor.ToArgb() != _settingsBefore.ActiveWindowBorderColor.ToArgb() ? Visibility.Visible : Visibility.Hidden;
        }

        #region Generic control handlers, parsers & utilites
        private void TextInputChanged(object sender, TextChangedEventArgs e)
        {
            UpdateRevertButtons();
        }

        private void BooleanInputChanged(object sender, RoutedEventArgs e)
        {
            UpdateRevertButtons();
        }

        private static bool TryParsePositiveInt(string input, int min, out int value)
        {
            return int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= min;
        }

        private static bool TryParseInt(string input, out int value)
        {
            return int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
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
        #endregion

        #region General Tab Handlers 
        private void RenderModeChanged(object sender, SelectionChangedEventArgs e)
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

        private void RevertRenderMode_Click(object sender, RoutedEventArgs e)
        {
            cmbRenderMode.SelectedIndex = _settingsBefore.RenderMode <= 0 ? 0 : 1;
            UpdateRevertButtons();
        }

        private void RevertPagerHeight_Click(object sender, RoutedEventArgs e)
        {
            txtPagerHeight.Text = _settingsBefore.PagerHeight.ToString(CultureInfo.InvariantCulture);
            UpdateRevertButtons();
        }

        private void RevertDesktopBorderStyle_Click(object sender, RoutedEventArgs e)
        {
            cmbDesktopBorderStyle.SelectedIndex = _settingsBefore.DesktopBorderStyle <= 0 ? 0 : 1;
            UpdateRevertButtons();
        }

        private void RevertBackgroundOpacity_Click(object sender, RoutedEventArgs e)
        {
            if (sldBackgroundOpacity != null)
            {
                sldBackgroundOpacity.Value = Math.Max(0.25, Math.Min(1.0, _settingsBefore.BackgroundOpacity));
            }
            UpdateRevertButtons();
        }

        private void RevertPaddingSize_Click(object sender, RoutedEventArgs e)
        {
            txtPaddingSize.Text = _settingsBefore.PaddingSize.ToString(CultureInfo.InvariantCulture);
            UpdateRevertButtons();
        }

        private void RevertIconPaddingX_Click(object sender, RoutedEventArgs e)
        {
            txtIconPaddingX.Text = _settingsBefore.IconPaddingX.ToString(CultureInfo.InvariantCulture);
            UpdateRevertButtons();
        }

        private void RevertIconPaddingY_Click(object sender, RoutedEventArgs e)
        {
            txtIconPaddingY.Text = _settingsBefore.IconPaddingY.ToString(CultureInfo.InvariantCulture);
            UpdateRevertButtons();
        }

        private void RevertShowAppInTaskbar_Click(object sender, RoutedEventArgs e)
        {
            chkShowAppInTaskbar.IsChecked = _settingsBefore.ShowAppInTaskbar;
            UpdateRevertButtons();
        }
        #endregion

        #region Colors Tab Handlers 
        private void UpdateColorsTab()
        {
            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void ChooseColor(ref DrawingColor color)
        {
            color = PickColor(color);
            UpdateColorsTab();
        }

        private void RevertColor(ref DrawingColor color, DrawingColor beforeColor)
        {
            color = beforeColor;
            UpdateColorsTab();
        }

        private void ChooseDesktopBackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            ChooseColor(ref _desktopBackgroundColor);
        }

        private void ChooseDesktopBorderColor_Click(object sender, RoutedEventArgs e)
        {
            ChooseColor(ref _desktopBorderColor);
        }

        private void ChooseActiveDesktopBorderColor_Click(object sender, RoutedEventArgs e)
        {
            ChooseColor(ref _activeDesktopBorderColor);
        }

        private void ChooseWindowBackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            ChooseColor(ref _windowBackgroundColor);
        }

        private void ChooseActiveWindowBackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            ChooseColor(ref _activeWindowBackgroundColor);
        }

        private void ChooseWindowBorderColor_Click(object sender, RoutedEventArgs e)
        {
            ChooseColor(ref _windowBorderColor);
        }

        private void ChooseActiveWindowBorderColor_Click(object sender, RoutedEventArgs e)
        {
            ChooseColor(ref _activeWindowBorderColor);
        }

        private void RevertDesktopBackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            RevertColor(ref _desktopBackgroundColor, _settingsBefore.DesktopBackgroundColor);
        }

        private void RevertDesktopBorderColor_Click(object sender, RoutedEventArgs e)
        {
            RevertColor(ref _desktopBorderColor, _settingsBefore.DesktopBorderColor);
        }

        private void RevertActiveDesktopBorderColor_Click(object sender, RoutedEventArgs e)
        {
            RevertColor(ref _activeDesktopBorderColor, _settingsBefore.ActiveDesktopBorderColor);
        }

        private void RevertWindowBackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            RevertColor(ref _windowBackgroundColor, _settingsBefore.WindowBackgroundColor);
        }

        private void RevertActiveWindowBackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            RevertColor(ref _activeWindowBackgroundColor, _settingsBefore.ActiveWindowBackgroundColor);
        }

        private void RevertWindowBorderColor_Click(object sender, RoutedEventArgs e)
        {
            RevertColor(ref _windowBorderColor, _settingsBefore.WindowBorderColor);
        }

        private void RevertActiveWindowBorderColor_Click(object sender, RoutedEventArgs e)
        {
            RevertColor(ref _activeWindowBorderColor, _settingsBefore.ActiveWindowBorderColor);
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
            previewBackgroundColor.Background = ToBrush(_desktopBackgroundColor);
            previewDesktopColor.Background = ToBrush(_desktopBorderColor);
            previewActiveDesktopBorderColor.Background = ToBrush(_activeDesktopBorderColor);
            previewWindowColor.Background = ToBrush(_windowBackgroundColor);
            previewActiveWindowColor.Background = ToBrush(_activeWindowBackgroundColor);
            previewWindowBorderColor.Background = ToBrush(_windowBorderColor);
            previewActiveWindowBorderColor.Background = ToBrush(_activeWindowBorderColor);
        }

        private static System.Windows.Media.SolidColorBrush ToBrush(DrawingColor color)
        {
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }
        #endregion

        # region Performance Tab Handlers
        private void RevertPrimaryDelay_Click(object sender, RoutedEventArgs e)
        {
            txtPrimaryDelay.Text = _settingsBefore.PrimaryUpdateDelay.ToString(CultureInfo.InvariantCulture);
            UpdateRevertButtons();
        }

        private void RevertSecondaryDelay_Click(object sender, RoutedEventArgs e)
        {
            txtSecondaryDelay.Text = _settingsBefore.SecondaryUpdateDelay.ToString(CultureInfo.InvariantCulture);
            UpdateRevertButtons();
        }
        #endregion
    }
}

