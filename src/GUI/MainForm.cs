using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Switchie
{
    // Main Application Form
    public class MainForm : Form
    {
        private string version = "1.2.0";

        // This works only when the pinning is manually done by the user after application has started (because auto pinning will always fail)
        private bool _showAppInTaskbar = true;
        
        private bool _isAppPinned = false;
        private int _activeDesktopIndex = 0;
        private int _currentDesktopCount = 0;
        private bool _forceAlwaysOnTop = false;
        private string _windowsHash = string.Empty;

        private readonly List<VirtualDesktop> _virtualDesktops = new List<VirtualDesktop>();

        private Point dragOffset;
        public bool IsDraggingWindow { get; set; }

        private readonly int primaryUpdateDelay = 200;
        private readonly int secondaryUpdateDelay = 500;

        public int BorderSize { get; set; } = 1;
        public int PagerHeight { get; set; } = 40;
        public int VirtualDesktopSpacing { get; set; } = 4;

        public Color DesktopColor { get; set; } = Color.FromArgb(32, 32, 32); // Background inbetween desktops
        public Color BackgroundColor { get; set; } = Color.FromArgb(64, 64, 64);
        public Color WindowColor { get; set; } = Color.FromArgb(255, Color.Gray);
        public Color WindowBorderColor { get; set; } = Color.Silver;

        public Color ActiveWindowColor { get; set; } = Color.FromArgb(255, Color.Silver);
        public Color ActiveWindowBorderColor { get; set; } = Color.White;
        public Color ActiveDesktopBorderColor { get; set; } = Color.LightBlue;

        public ConcurrentBag<Window> Windows = new ConcurrentBag<Window>();

        private readonly WinEventHook.WinEventDelegate _proc;
        private readonly IntPtr _hook;

        public enum RenderMode
        {
            Thumbnails,
            Icons
        }

        public RenderMode WindowRenderMode { get; set; } = RenderMode.Thumbnails;

        public MainForm()
        {
            SuspendLayout();
            Name = "frmMain";

            DoubleBuffered = true;
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;

            StartPosition = FormStartPosition.Manual;
            ControlBox = false;
            MaximizeBox = false;
            MinimizeBox = false;
            TopMost = true;
            AllowDrop = true;

            BackColor = BackgroundColor;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Icon = new System.Drawing.Icon(new MemoryStream(Helpers.GetResourceFromAssembly(typeof(Program), "Switchie.Resources.icon.ico")));

            ShowInTaskbar = _showAppInTaskbar;

            // Collect all virtual desktops and add mouse event listeners to them
            GetVirtualDesktopsAndAddMouseHandlers(_virtualDesktops);

            // Pager size depending on current amount of virtual desktops
            Size = new Size(_virtualDesktops.Sum(x => x.Size.Width), PagerHeight);
            MinimumSize = Size;
            MaximumSize = Size;
            ClientSize = Size;

            WindowRenderMode = (RenderMode)RegistryAccess.getRenderMode();

            var storedLocation = RegistryAccess.RestoreLocation();
            Location = storedLocation ?? getDefaultLocation();

            ResumeLayout(false);
            Shown += OnShown;
            MouseUp += OnMouseUp;
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;

            // Start global Windows Event Hook to bring app window to front again when covered by the taskbar before
            // -> only useful whenn overlapping with taskbar is required
            // -> currently not used because of overlapping detection
            //_proc = WinEventCallback;

            /*_hook = WinEventHook.SetWinEventHook(
                WinEventHook.EVENT_SYSTEM_FOREGROUND,
                WinEventHook.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, _proc, 0, 0,
                WinEventHook.WINEVENT_OUTOFCONTEXT);*/
        }

        private void WinEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            var className = new StringBuilder(256);
            IntPtr nRet = WinAPI.GetClassName(hwnd, className, className.Capacity);
            Debug.WriteLine("Hook called for " + className);

            // Ingore own window
            if (hwnd == Handle)
                return;

            // Currently we pay only attention to Shell TrayWnd events
            if (className.ToString() == "Shell_TrayWnd")
            {
                Task.Delay(50).ContinueWith(_ =>
                {
                    if (!IsDisposed)
                    {
                        BeginInvoke((Action)(() => WindowManager.RestoreWindow(Handle)));
                    }
                });
            }
        }

        // Default app window location: Centered and above Taskbar
        private Point getDefaultLocation() => new Point((Screen.PrimaryScreen.Bounds.Width / 2) - (Size.Width / 2), Screen.PrimaryScreen.WorkingArea.Bottom - Size.Height);

        // App Window covering detection: Currently only by its center point
        private bool isCovered()
        {
            var rect = this.Bounds;
            var point = new WinAPI.POINT
            {
                X = rect.Left + rect.Width / 2,
                Y = rect.Top + rect.Height / 2
            };
            var hwndAtPoint = WinAPI.WindowFromPoint(point);
            return hwndAtPoint != this.Handle;
        }

        private bool hasDesktopCountChanged() => _currentDesktopCount != WindowsVirtualDesktop.GetInstance().Count;

        private void ResetVirtualDesktop()
        {
            // Remove mouse listeners for all existing desktops first before removing them
            _virtualDesktops.ForEach((desktop) =>
            {
                MouseUp -= desktop.OnMouseUp;
                MouseDown -= desktop.OnMouseDown;
                MouseMove -= desktop.OnMouseMove;
                DragOver -= desktop.OnDragOver;
                DragDrop -= desktop.OnDragDrop;
            });

            WindowsVirtualDesktop.Restart();
            WindowsVirtualDesktopManager.Restart();

            _virtualDesktops.Clear();
            GetVirtualDesktopsAndAddMouseHandlers(_virtualDesktops);

            SuspendLayout();
            var newSize = new Size(_virtualDesktops.Sum(d => d.Size.Width), PagerHeight);
            ClientSize = newSize;
            MinimumSize = newSize;
            MaximumSize = newSize;
            ResumeLayout();
        }

        private void GetVirtualDesktopsAndAddMouseHandlers(List<VirtualDesktop> virtualDesktops)
        {
            _currentDesktopCount = WindowsVirtualDesktop.GetInstance().Count;
            Enumerable.Range(0, _currentDesktopCount).ToList().ForEach(d =>
            {
                VirtualDesktop desktop = new VirtualDesktop(d, this, new Point(_virtualDesktops.Sum(i => i.Size.Width), 0));
                MouseUp += desktop.OnMouseUp;
                MouseDown += desktop.OnMouseDown;
                MouseMove += desktop.OnMouseMove;
                DragOver += desktop.OnDragOver;
                DragDrop += desktop.OnDragDrop;
                virtualDesktops.Add(desktop);
            });
        }

        private void OnShown(object sender, EventArgs e)
        {
            // Primary application loop to check global window changes so the miniatures get updated
            Task.Run(async () =>
            {
                while (!Program.ApplicationClosing.IsCancellationRequested)
                {
                    Invoke(new Action(() =>
                    {
                        try
                        {
                            //if (_forceAlwaysOnTop) WindowManager.SetAlwaysOnTop(Handle, _forceAlwaysOnTop);

                            // Change Detection via Hash (calculated on basic parameters of all opened windows)
                            // -> Works now finally as expected because all windows are sorted first to remain in the same state
                            Windows = new ConcurrentBag<Window>(WindowManager.GetOpenWindows());
                            var hash =
                            $"{_activeDesktopIndex}" +
                            $"{Windows.Sum(x => Math.Abs(x.Dimensions.X))}" +
                            $"{Windows.Sum(x => Math.Abs(x.Dimensions.Y))}" +
                            $"{Windows.Sum(x => x.Dimensions.Width)}" +
                            $"{Windows.Sum(x => x.Dimensions.Height)}" +
                            $"{string.Join("", Windows.Select(x => x.IsActive ? 1 : 0))}" +
                            $"{string.Join("", Windows.Select(x => WinAPI.GetForegroundWindow() == x.Handle ? 1 : 0))}" +
                            $"{string.Join("", Windows.Select(x => x.VirtualDesktopIndex))}";

                            if (hash != _windowsHash)
                            {
                                _windowsHash = hash;
                                Invalidate();
                            }
                        }
                        catch { }
                    }));

                    // Refresh rate for thumbnails by default 1, but 100 is also fine, otherwise the
                    // hash calculation and string concatenations happens every ms
                    await Task.Delay(primaryUpdateDelay);
                }
            });

            // Secondary application loop for pinning the app, bringing it to front when hidden by some other window and detect desktop count changes
            // -> only pinned apps are visible on all virtual desktops, which is a requirement for this app 
            Task.Run(async () =>
            {
                while (!Program.ApplicationClosing.IsCancellationRequested)
                {
                    Invoke(new Action(() =>
                    {
                        // Check if window is covered shoulf be fine with every 500ms 
                        if (isCovered())
                        {
                            WindowManager.RestoreWindow(Handle);
                        }

                        if (hasDesktopCountChanged())
                        {
                            ResetVirtualDesktop();
                        }

                        // This should only be the case at start up, right
                        if (_showAppInTaskbar && !_isAppPinned) { 
                        
                            try
                            {
                                // Note: The internal Divided by Zero error happens here
                                WindowsVirtualDesktopManager.GetInstance().PinApplication(Handle);
                                _isAppPinned = true;
                            }
                            catch { }
                        }
                        try
                        {
                            _activeDesktopIndex = WindowsVirtualDesktopManager.GetInstance().FromDesktop(WindowsVirtualDesktop.GetInstance().Current);
                            Windows = new ConcurrentBag<Window>(WindowManager.GetOpenWindows());
                            //Invalidate();
                        }
                        catch { }
                    }));
                    await Task.Delay(secondaryUpdateDelay);
                }
            });
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            IsDraggingWindow = false;
            Cursor = Cursors.Default;
            if ((e.Button & MouseButtons.Right) == MouseButtons.Right)
            {
                ShowContextMenu();
            }
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Middle) == MouseButtons.Middle)
            {
                IsDraggingWindow = true;
                dragOffset = e.Location;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (IsDraggingWindow)
            {
                Cursor = Cursors.SizeAll;
                Location = new Point(e.X + Location.X - dragOffset.X, e.Y + Location.Y - dragOffset.Y);
            }
        }

        private void ShowContextMenu()
        {
            _forceAlwaysOnTop = false;
            ContextMenuStrip menu = new ContextMenuStrip();

            // --- Position related ---
            ToolStripDropDown dropDown = new ToolStripDropDown();
            ToolStripDropDownButton dropDownButton = new ToolStripDropDownButton
            {
                Text = "Position",
                AutoToolTip = false,
                DropDown = dropDown,
                DropDownDirection = ToolStripDropDownDirection.Right
            };

            ToolStripButton buttonRestorePos = new ToolStripButton("Restore", null, (s, ev) =>
            {
                var storedLocation = RegistryAccess.RestoreLocation();
                if (storedLocation != null)
                {
                    Location = storedLocation.Value;
                }
            });

            ToolStripButton buttonSavePos = new ToolStripButton("Save", null, (s, ev) =>
            {
                RegistryAccess.SaveLocation(Location);
            });

            ToolStripButton buttonDefaultPos = new ToolStripButton("Default", null, (s, ev) =>
            {
                Location = getDefaultLocation();
            });
            dropDown.Items.AddRange(new ToolStripItem[] { buttonRestorePos, buttonSavePos, buttonDefaultPos });
            menu.Items.Add(dropDownButton);

            // --- Aditional menu entries ---
            Helpers.AddMenuItem(this, menu,
                new ToolStripMenuItem()
                {
                    Text = "Reset",
                    ToolTipText = "Reinitialize Virtual Desktops"
                },
                () =>
                {
                    ResetVirtualDesktop();
                });

            Helpers.AddMenuItem(this, menu,
                new ToolStripMenuItem()
                {
                    Text = "About"
                },
                () =>
                {
                    MessageBox.Show($"Switchie{Environment.NewLine}v{version}{Environment.NewLine}{Environment.NewLine}Made by darkguy2008", "About");
                    _forceAlwaysOnTop = true;
                });

            Helpers.AddMenuItem(this, menu,
                new ToolStripMenuItem()
                {
                    Text = "Toggle Render Mode"
                },
                () =>
                {
                    WindowRenderMode = WindowRenderMode == RenderMode.Icons ? RenderMode.Thumbnails : RenderMode.Icons;
                    RegistryAccess.saveRenderMode((int)WindowRenderMode);
                    Invalidate();
                });

            Helpers.AddMenuItem(this, menu,
                new ToolStripMenuItem()
                {
                    Text = "Exit"
                },
                () =>
                {
                    Environment.Exit(1);
                });

            menu.Opened += (ss, ee) => _forceAlwaysOnTop = false;
            menu.Show(this, PointToClient(Cursor.Position));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            try
            {
                _virtualDesktops.ForEach(x => x.OnPaint(e));
            }
            catch
            {
                // TODO: Check, if we run into issues when render target is gone
                WindowsVirtualDesktop.Restart();
                WindowsVirtualDesktopManager.Restart();
            }
        }
    }
}
