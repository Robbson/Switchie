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
        private DrawingColor _desktopColor;
        private DrawingColor _activeDesktopBorderColor;

        public AppSettings Settings { get; private set; }

        public AppSettingsWindow(AppSettings settings)
        {
            InitializeComponent();

            Settings = Clone(settings);

            cmbRenderMode.Items.Add("Windows");
            cmbRenderMode.Items.Add("Icons");

            cmbRenderMode.SelectedIndex = Math.Max(0, Math.Min(1, Settings.RenderMode));
            txtPagerHeight.Text = Settings.PagerHeight.ToString(CultureInfo.InvariantCulture);
            txtPrimaryDelay.Text = Settings.PrimaryUpdateDelay.ToString(CultureInfo.InvariantCulture);
            txtSecondaryDelay.Text = Settings.SecondaryUpdateDelay.ToString(CultureInfo.InvariantCulture);

            _backgroundColor = Settings.BackgroundColor;
            _desktopColor = Settings.DesktopColor;
            _activeDesktopBorderColor = Settings.ActiveDesktopBorderColor;

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
            _desktopColor = PickColor(_desktopColor);
            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void ChooseActiveDesktopBorderColor_Click(object sender, RoutedEventArgs e)
        {
            _activeDesktopBorderColor = PickColor(_activeDesktopBorderColor);
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

        private void RevertAll_Click(object sender, RoutedEventArgs e)
        {
            cmbRenderMode.SelectedIndex = _defaults.RenderMode <= 0 ? 0 : 1;
            txtPagerHeight.Text = _defaults.PagerHeight.ToString(CultureInfo.InvariantCulture);
            txtPrimaryDelay.Text = _defaults.PrimaryUpdateDelay.ToString(CultureInfo.InvariantCulture);
            txtSecondaryDelay.Text = _defaults.SecondaryUpdateDelay.ToString(CultureInfo.InvariantCulture);

            _backgroundColor = _defaults.BackgroundColor;
            _desktopColor = _defaults.DesktopColor;
            _activeDesktopBorderColor = _defaults.ActiveDesktopBorderColor;

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

        private void RevertBackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            _backgroundColor = _defaults.BackgroundColor;
            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void RevertDesktopColor_Click(object sender, RoutedEventArgs e)
        {
            _desktopColor = _defaults.DesktopColor;
            RefreshColorPreviews();
            UpdateRevertButtons();
        }

        private void RevertActiveDesktopBorderColor_Click(object sender, RoutedEventArgs e)
        {
            _activeDesktopBorderColor = _defaults.ActiveDesktopBorderColor;
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

            if (!TryParsePositiveInt(txtPrimaryDelay.Text, 10, out int primaryDelay))
            {
                ShowValidationError("Primary Update Delay must be at least 10 ms.");
                return;
            }

            if (!TryParsePositiveInt(txtSecondaryDelay.Text, 10, out int secondaryDelay))
            {
                ShowValidationError("Secondary Update Delay must be at least 10 ms.");
                return;
            }

            Settings = new AppSettings
            {
                RenderMode = cmbRenderMode.SelectedIndex <= 0 ? 0 : 1,
                PagerHeight = pagerHeight,
                PrimaryUpdateDelay = primaryDelay,
                SecondaryUpdateDelay = secondaryDelay,
                BackgroundColor = _backgroundColor,
                DesktopColor = _desktopColor,
                ActiveDesktopBorderColor = _activeDesktopBorderColor
            };

            DialogResult = true;
            Close();
        }

        private static bool TryParsePositiveInt(string input, int min, out int value)
        {
            return int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= min;
        }

        private static AppSettings Clone(AppSettings source)
        {
            return new AppSettings
            {
                RenderMode = source.RenderMode,
                PagerHeight = source.PagerHeight,
                PrimaryUpdateDelay = source.PrimaryUpdateDelay,
                SecondaryUpdateDelay = source.SecondaryUpdateDelay,
                DesktopColor = source.DesktopColor,
                BackgroundColor = source.BackgroundColor,
                ActiveDesktopBorderColor = source.ActiveDesktopBorderColor
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
            previewDesktopColor.Background = ToBrush(_desktopColor);
            previewActiveDesktopBorderColor.Background = ToBrush(_activeDesktopBorderColor);
        }

        private static System.Windows.Media.SolidColorBrush ToBrush(DrawingColor color)
        {
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        private void UpdateRevertButtons()
        {
            btnRevertRenderMode.Visibility = IsDifferent(cmbRenderMode.SelectedIndex, _defaults.RenderMode) ? Visibility.Visible : Visibility.Hidden;
            btnRevertPagerHeight.Visibility = IsDifferent(txtPagerHeight.Text, _defaults.PagerHeight) ? Visibility.Visible : Visibility.Hidden;
            btnRevertPrimaryDelay.Visibility = IsDifferent(txtPrimaryDelay.Text, _defaults.PrimaryUpdateDelay) ? Visibility.Visible : Visibility.Hidden;
            btnRevertSecondaryDelay.Visibility = IsDifferent(txtSecondaryDelay.Text, _defaults.SecondaryUpdateDelay) ? Visibility.Visible : Visibility.Hidden;

            btnRevertBackgroundColor.Visibility = _backgroundColor.ToArgb() != _defaults.BackgroundColor.ToArgb() ? Visibility.Visible : Visibility.Hidden;
            btnRevertDesktopColor.Visibility = _desktopColor.ToArgb() != _defaults.DesktopColor.ToArgb() ? Visibility.Visible : Visibility.Hidden;
            btnRevertActiveDesktopBorderColor.Visibility = _activeDesktopBorderColor.ToArgb() != _defaults.ActiveDesktopBorderColor.ToArgb() ? Visibility.Visible : Visibility.Hidden;
        }

        private static bool IsDifferent(string input, int defaultValue)
        {
            return !int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) || parsed != defaultValue;
        }

        private static bool IsDifferent(int input, int defaultValue)
        {
            return input != defaultValue;
        }

        private void ShowValidationError(string message)
        {
            MessageBox.Show(this, message, "Invalid Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

