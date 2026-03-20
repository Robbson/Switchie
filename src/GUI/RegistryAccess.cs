using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Switchie
{
    public class RegistryAccess
    {
        private readonly static string registryKey = @"SOFTWARE\Switchie";

        public static System.Drawing.Point? RestoreLocation()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(registryKey);
            if (key == null) return null;
            var x = key.GetValue("PagerLocationX");
            var y = key.GetValue("PagerLocationY");
            if (x == null || y == null) return null;
            return new System.Drawing.Point(
              int.Parse(x.ToString()),
              int.Parse(y.ToString())
            );
        }

        public static void SaveLocation(System.Drawing.Point location)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(registryKey);
            key.SetValue("PagerLocationX", location.X);
            key.SetValue("PagerLocationY", location.Y);
            key.Close();
        }

        public static int getRenderMode()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(registryKey);
            if (key == null) return 0;
            var renderMode = key.GetValue("RenderMode");
            if (renderMode == null) return 0;
            return int.Parse(key.GetValue("RenderMode").ToString());
        }

        public static void saveRenderMode(int renderMode)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(registryKey);
            key.SetValue("RenderMode", renderMode);
            key.Close();
        }
    }
}
