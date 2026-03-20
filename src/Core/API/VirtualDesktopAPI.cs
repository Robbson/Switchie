using System;
using System.Collections.Generic;
using System.Diagnostics;
namespace Switchie
{
    public class WindowsVirtualDesktop
    {
        private static IWindowsVirtualDesktop _instance;
        public static void Restart()
        {
            WindowsVirtualDesktop._instance = null;
            GetInstance();
            WindowsVirtualDesktop._instance.Restart();
        }

        public static IWindowsVirtualDesktop GetInstance()
        {
            if (WindowsVirtualDesktop._instance == null)
            {
                _instance = CreateForCurrentOS();
            }
            return WindowsVirtualDesktop._instance;
        }

        private static IWindowsVirtualDesktop CreateForCurrentOS()
        {
            if (Program.WindowsVersion.IsWin11())
            {
                IWindowsVirtualDesktop desktop;
                Exception win11Error;
                if (TryCreateWin11(out desktop, out win11Error))
                    return desktop;

                Trace.WriteLine("[Switchie] Win11 desktop API initialization failed. Falling back. " + win11Error);

                Exception win10Error;
                if (TryCreateWin10(out desktop, out win10Error))
                {
                    Trace.WriteLine("[Switchie] Using Win10 desktop API fallback on Win11.");
                    return desktop;
                }

                Exception ltscError;
                if (TryCreateWin10LTSC(out desktop, out ltscError))
                {
                    Trace.WriteLine("[Switchie] Using Win10 LTSC desktop API fallback on Win11.");
                    return desktop;
                }

                throw BuildInitializationException("virtual desktop", win11Error, win10Error, ltscError);
            }

            if (Program.WindowsVersion.IsWin10())
            {
                IWindowsVirtualDesktop desktop;
                Exception error;
                if (TryCreateWin10(out desktop, out error))
                    return desktop;
                throw new InvalidOperationException("Failed to initialize Win10 virtual desktop backend.", error);
            }

            if (Program.WindowsVersion.IsWin10LTSC())
            {
                IWindowsVirtualDesktop desktop;
                Exception error;
                if (TryCreateWin10LTSC(out desktop, out error))
                    return desktop;
                throw new InvalidOperationException("Failed to initialize Win10 LTSC virtual desktop backend.", error);
            }

            throw new PlatformNotSupportedException();
        }

        private static InvalidOperationException BuildInitializationException(string featureName, params Exception[] errors)
        {
            var innerExceptions = new List<Exception>();
            foreach (Exception error in errors)
            {
                if (error != null)
                    innerExceptions.Add(error);
            }

            return new InvalidOperationException(
                "Failed to initialize " + featureName + " backend for this Windows version.",
                new AggregateException(innerExceptions));
        }

        private static bool TryCreateWin11(out IWindowsVirtualDesktop desktop, out Exception error)
        {
            try
            {
                desktop = new Switchie.VirtualDesktopAPI.Win11.WindowsVirtualDesktop();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                desktop = null;
                error = ex;
                return false;
            }
        }

        private static bool TryCreateWin10(out IWindowsVirtualDesktop desktop, out Exception error)
        {
            try
            {
                desktop = new Switchie.VirtualDesktopAPI.Win10.WindowsVirtualDesktop();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                desktop = null;
                error = ex;
                return false;
            }
        }

        private static bool TryCreateWin10LTSC(out IWindowsVirtualDesktop desktop, out Exception error)
        {
            try
            {
                desktop = new Switchie.VirtualDesktopAPI.Win10LTSC.WindowsVirtualDesktop();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                desktop = null;
                error = ex;
                return false;
            }
        }

    }

    public class WindowsVirtualDesktopManager
    {
        private static IWindowsVirtualDesktopManager _instance;
        public static void Restart() => WindowsVirtualDesktopManager._instance = null;

        public static IWindowsVirtualDesktopManager GetInstance()
        {
            if (WindowsVirtualDesktopManager._instance == null)
            {
                _instance = CreateForCurrentOS();
            }
            return WindowsVirtualDesktopManager._instance;
        }

        private static IWindowsVirtualDesktopManager CreateForCurrentOS()
        {
            if (Program.WindowsVersion.IsWin11())
            {
                IWindowsVirtualDesktopManager manager;
                Exception win11Error;
                if (TryCreateWin11(out manager, out win11Error))
                    return manager;

                Trace.WriteLine("[Switchie] Win11 desktop manager API initialization failed. Falling back. " + win11Error);

                Exception win10Error;
                if (TryCreateWin10(out manager, out win10Error))
                {
                    Trace.WriteLine("[Switchie] Using Win10 desktop manager API fallback on Win11.");
                    return manager;
                }

                Exception ltscError;
                if (TryCreateWin10LTSC(out manager, out ltscError))
                {
                    Trace.WriteLine("[Switchie] Using Win10 LTSC desktop manager API fallback on Win11.");
                    return manager;
                }

                throw BuildInitializationException("virtual desktop manager", win11Error, win10Error, ltscError);
            }

            if (Program.WindowsVersion.IsWin10())
            {
                IWindowsVirtualDesktopManager manager;
                Exception error;
                if (TryCreateWin10(out manager, out error))
                    return manager;
                throw new InvalidOperationException("Failed to initialize Win10 virtual desktop manager backend.", error);
            }

            if (Program.WindowsVersion.IsWin10LTSC())
            {
                IWindowsVirtualDesktopManager manager;
                Exception error;
                if (TryCreateWin10LTSC(out manager, out error))
                    return manager;
                throw new InvalidOperationException("Failed to initialize Win10 LTSC virtual desktop manager backend.", error);
            }

            throw new PlatformNotSupportedException();
        }

        private static InvalidOperationException BuildInitializationException(string featureName, params Exception[] errors)
        {
            var innerExceptions = new List<Exception>();
            foreach (Exception error in errors)
            {
                if (error != null)
                    innerExceptions.Add(error);
            }

            return new InvalidOperationException(
                "Failed to initialize " + featureName + " backend for this Windows version.",
                new AggregateException(innerExceptions));
        }

        private static bool TryCreateWin11(out IWindowsVirtualDesktopManager manager, out Exception error)
        {
            try
            {
                manager = new Switchie.VirtualDesktopAPI.Win11.WindowsVirtualDesktopManager();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                manager = null;
                error = ex;
                return false;
            }
        }

        private static bool TryCreateWin10(out IWindowsVirtualDesktopManager manager, out Exception error)
        {
            try
            {
                manager = new Switchie.VirtualDesktopAPI.Win10.WindowsVirtualDesktopManager();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                manager = null;
                error = ex;
                return false;
            }
        }

        private static bool TryCreateWin10LTSC(out IWindowsVirtualDesktopManager manager, out Exception error)
        {
            try
            {
                manager = new Switchie.VirtualDesktopAPI.Win10LTSC.WindowsVirtualDesktopManager();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                manager = null;
                error = ex;
                return false;
            }
        }
    }

}
