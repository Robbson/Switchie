using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Switchie
{
    public class Utilties
    {
        private readonly static string registryKey = @"SOFTWARE\Switchie";

        #region Registry Storage
        public static System.Drawing.Point? GetLocationFromRegistry()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(registryKey);
            if (key != null)
            {
                return new System.Drawing.Point(
                    int.Parse(key.GetValue("PagerLocationX").ToString()),
                    int.Parse(key.GetValue("PagerLocationY").ToString())
                );
            }
            return null;
        }

        public static void SaveLocationToRegistry(System.Drawing.Point location)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(registryKey);
            key.SetValue("PagerLocationX", location.X);
            key.SetValue("PagerLocationY", location.Y);
            key.Close();
        }

        public bool getMode()
        {
            return true;
        }

        public void setMode()
        {

        }

        #endregion
    }
}
