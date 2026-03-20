using Microsoft.Win32;
using System;
using System.Drawing;

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
                    PagerHeight = ReadInt(key, "PagerHeight", defaults.PagerHeight),
                    PrimaryUpdateDelay = ReadInt(key, "PrimaryUpdateDelay", defaults.PrimaryUpdateDelay),
                    SecondaryUpdateDelay = ReadInt(key, "SecondaryUpdateDelay", defaults.SecondaryUpdateDelay),
                    DesktopColor = ReadColor(key, "DesktopColor", defaults.DesktopColor),
                    BackgroundColor = ReadColor(key, "BackgroundColor", defaults.BackgroundColor),
                    ActiveDesktopBorderColor = ReadColor(key, "ActiveDesktopBorderColor", defaults.ActiveDesktopBorderColor)
                };
            }
        }

        public static void Save(AppSettings settings)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
            {
                if (key == null) return;
                key.SetValue("RenderMode", settings.RenderMode, RegistryValueKind.DWord);
                key.SetValue("PagerHeight", settings.PagerHeight, RegistryValueKind.DWord);
                key.SetValue("PrimaryUpdateDelay", settings.PrimaryUpdateDelay, RegistryValueKind.DWord);
                key.SetValue("SecondaryUpdateDelay", settings.SecondaryUpdateDelay, RegistryValueKind.DWord);
                key.SetValue("DesktopColor", settings.DesktopColor.ToArgb(), RegistryValueKind.DWord);
                key.SetValue("BackgroundColor", settings.BackgroundColor.ToArgb(), RegistryValueKind.DWord);
                key.SetValue("ActiveDesktopBorderColor", settings.ActiveDesktopBorderColor.ToArgb(), RegistryValueKind.DWord);
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
    }
}

