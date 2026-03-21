using Microsoft.Win32;
using System;
using System.Drawing;
using System.Globalization;

namespace Switchie
{
    public static class AppSettingsStore
    {
        private const string RegistryKeyPath = @"SOFTWARE\Switchie";

        public static AppSettings Load()
        {
            var defaults = new AppSettings();
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                return key == null ? defaults : new AppSettings
                {
                    RenderMode = ReadInt(key, "RenderMode", defaults.RenderMode),
                    DesktopBorderStyle = ReadInt(key, "DesktopBorderStyle", defaults.DesktopBorderStyle),
                    BackgroundOpacity = ReadDouble(key, "BackgroundOpacity", defaults.BackgroundOpacity),
                    PagerHeight = ReadInt(key, "PagerHeight", defaults.PagerHeight),
                    PaddingSize = ReadInt(key, "PaddingSize", defaults.PaddingSize),
                    IconPaddingX = ReadInt(key, "IconPaddingX", defaults.IconPaddingX),
                    IconPaddingY = ReadInt(key, "IconPaddingY", defaults.IconPaddingY),

                    BackgroundColor = ReadColor(key, "BackgroundColor", defaults.BackgroundColor),

                    DesktopBorderColor = ReadColor(key, "DesktopBorderColor", defaults.DesktopBorderColor),
                    ActiveDesktopBorderColor = ReadColor(key, "ActiveDesktopBorderColor", defaults.ActiveDesktopBorderColor),

                    WindowColor = ReadColor(key, "WindowColor", defaults.WindowColor),
                    ActiveWindowColor = ReadColor(key, "ActiveWindowColor", defaults.ActiveWindowColor),
                    WindowBorderColor = ReadColor(key, "WindowBorderColor", defaults.WindowBorderColor),
                    ActiveWindowBorderColor = ReadColor(key, "ActiveWindowBorderColor", defaults.ActiveWindowBorderColor),

                    PrimaryUpdateDelay = ReadInt(key, "PrimaryUpdateDelay", defaults.PrimaryUpdateDelay),
                    SecondaryUpdateDelay = ReadInt(key, "SecondaryUpdateDelay", defaults.SecondaryUpdateDelay),
                    ShowAppInTaskbar = ReadInt(key, "ShowAppInTaskbar", defaults.ShowAppInTaskbar ? 1 : 0) == 1
                };
            }
        }

        public static void Save(AppSettings settings)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
            {
                if (key == null) return;
                key.SetValue("RenderMode", settings.RenderMode, RegistryValueKind.DWord);
                key.SetValue("DesktopBorderStyle", settings.DesktopBorderStyle, RegistryValueKind.DWord);
                key.SetValue("BackgroundOpacity", settings.BackgroundOpacity.ToString(CultureInfo.InvariantCulture), RegistryValueKind.String);
                key.SetValue("PagerHeight", settings.PagerHeight, RegistryValueKind.DWord);
                key.SetValue("PaddingSize", settings.PaddingSize, RegistryValueKind.DWord);
                key.SetValue("IconPaddingX", settings.IconPaddingX, RegistryValueKind.DWord);
                key.SetValue("IconPaddingY", settings.IconPaddingY, RegistryValueKind.DWord);

                key.SetValue("BackgroundColor", settings.BackgroundColor.ToArgb(), RegistryValueKind.DWord);

                key.SetValue("DesktopBorderColor", settings.DesktopBorderColor.ToArgb(), RegistryValueKind.DWord);
                key.SetValue("ActiveDesktopBorderColor", settings.ActiveDesktopBorderColor.ToArgb(), RegistryValueKind.DWord);

                key.SetValue("WindowColor", settings.WindowColor.ToArgb(), RegistryValueKind.DWord);
                key.SetValue("ActiveWindowColor", settings.ActiveWindowColor.ToArgb(), RegistryValueKind.DWord);
                key.SetValue("WindowBorderColor", settings.WindowBorderColor.ToArgb(), RegistryValueKind.DWord);
                key.SetValue("ActiveWindowBorderColor", settings.ActiveWindowBorderColor.ToArgb(), RegistryValueKind.DWord);

                key.SetValue("PrimaryUpdateDelay", settings.PrimaryUpdateDelay, RegistryValueKind.DWord);
                key.SetValue("SecondaryUpdateDelay", settings.SecondaryUpdateDelay, RegistryValueKind.DWord);
                key.SetValue("ShowAppInTaskbar", settings.ShowAppInTaskbar ? 1 : 0, RegistryValueKind.DWord);
            }
        }

        private static int ReadInt(RegistryKey key, string valueName, int fallback)
        {
            object value = key.GetValue(valueName);
            return value == null ? fallback : int.TryParse(value.ToString(), out int result) ? result : fallback;
        }

        private static Color ReadColor(RegistryKey key, string valueName, Color fallback)
        {
            object value = key.GetValue(valueName);
            return value == null ? fallback : int.TryParse(value.ToString(), out int argb) ? Color.FromArgb(argb) : fallback;
        }

        private static double ReadDouble(RegistryKey key, string valueName, double fallback)
        {
            object value = key.GetValue(valueName);
            return value == null
                ? fallback
                : double.TryParse(value.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double result)
                    ? result
                    : fallback;
        }
    }
}

