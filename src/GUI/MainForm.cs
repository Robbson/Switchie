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
    public class MainForm : Form
    {
        private Point dragOffset;
        private bool _isAppPinned = false;
        private int _activeDesktopIndex = 0;
        private bool _forceAlwaysOnTop = false;
        private string _windowsHash = string.Empty;

        private List<VirtualDesktop> _virtualDesktops = new List<VirtualDesktop>();

        private int updateDelay = 200;

        public int BorderSize { get; set; } = 1;
        public int PagerHeight { get; set; } = 40;
        public bool IsDraggingWindow { get; set; }
        public int VirtualDesktopSpacing { get; set; } = 4;

        public Color BackgroundColor { get; set; } = Color.FromArgb(64, 64, 64);

        public Color DesktopColor { get; set; } = Color.FromArgb(32, 32, 32); // Background inbetween desktops

        public Color WindowColor { get; set; } = Color.FromArgb(255, Color.Gray);

        public Color WindowBorderColor { get; set; } = Color.Silver;

        public Color ActiveWindowColor { get; set; } = Color.FromArgb(255, Color.Silver);

        public Color ActiveWindowBorderColor { get; set; } = Color.White;

        public Color ActiveDesktopBorderColor { get; set; } = Color.LightBlue;

        public ConcurrentBag<Window> Windows = new ConcurrentBag<Window>();

        private WinEventHook.WinEventDelegate _proc;
        private IntPtr _hook;

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
            ClientSize = new System.Drawing.Size(1, 1);
            MinimumSize = new System.Drawing.Size(1, 1);

            ControlBox = false;
            MaximizeBox = false;
            MinimizeBox = false;
            TopMost = true;
            AllowDrop = true;

            BackColor = BackgroundColor;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Icon = new System.Drawing.Icon(new MemoryStream(Helpers.GetResourceFromAssembly(typeof(Program), "Switchie.Resources.icon.ico")));

            // Collect all virtual desktops and add mouse event listeners to them
            _virtualDesktops = GetVirtualDesktopsAndAddMouseHandlers();

            // Pager size depending on current amount of virtual desktops
            // -> TODO: This doesn't get updated when amount of desktops has changed
            Size = new Size(_virtualDesktops.Sum(x => x.Size.Width), PagerHeight);
            MinimumSize = Size;
            MaximumSize = Size;
            ClientSize = Size;

            var storedLocation = Utilties.GetLocationFromRegistry();
            if (storedLocation.HasValue)
            {
                Location = storedLocation.Value;
            }
            else
            {
                // Default: Centered and above Taskbar
                // -> Preffered: 98, Screen.PrimaryScreen.WorkingArea.Bottom
                // -> There should be some defaults options in the settings
                Location = new System.Drawing.Point(
                    (Screen.PrimaryScreen.Bounds.Width / 2) - (Size.Width / 2),
                    Screen.PrimaryScreen.WorkingArea.Bottom - Size.Height
                );
            }

            ResumeLayout(false);
            Shown += OnShown;
            MouseUp += OnMouseUp;
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;

            _proc = WinEventCallback;
            /*_hook = WinEventHook.SetWinEventHook(
                WinEventHook.EVENT_SYSTEM_FOREGROUND,
                WinEventHook.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, _proc, 0, 0,
                WinEventHook.WINEVENT_OUTOFCONTEXT);*/
        }

        private void WinEventCallback(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime)
        {
            var className = new StringBuilder(256);
            IntPtr nRet = WinAPI.GetClassName(hwnd, className, className.Capacity);
            Debug.WriteLine("Hook called for " + className);

            // Optional: eigenes Fenster ignorieren
            if (hwnd == Handle)
                return;

            /*
            if (className.ToString() == "Shell_TrayWnd")
            {
                Task.Delay(50).ContinueWith(_ =>
                {
                    if (!IsDisposed)
                    {
                        BeginInvoke((Action)(() => RestoreWindow()));
                    }
                });
            }*/
        }

        private void RestoreWindow()
        {
            WinAPI.ShowWindowAsync(Handle, WinAPI.SW_RESTORE);
            WinAPI.ShowWindowAsync(Handle, WinAPI.SW_SHOWNOACTIVATE);
            WinAPI.SetWindowPos(Handle, WinAPI.HWND_TOPMOST,
                0, 0, 0, 0,
                WinAPI.SWP_NOMOVE | WinAPI.SWP_NOSIZE | WinAPI.SWP_NOACTIVATE);
        }

        private void ResetVirtualDesktop()
        {
            //SuspendLayout();

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
            _virtualDesktops = GetVirtualDesktopsAndAddMouseHandlers();

            // TODO: Change recognized but no pager size difference?
            Debug.WriteLine(_virtualDesktops.Count);

            Size = new Size(_virtualDesktops.Sum(x => x.Size.Width), PagerHeight * 2);
            MinimumSize = Size;
            MaximumSize = Size;
            ClientSize = Size;

            //base.Size = Size;
            //Invalidate();
            //ResumeLayout(false);
        }

        private List<VirtualDesktop> GetVirtualDesktopsAndAddMouseHandlers()
        {
            var virtualDesktops = new List<VirtualDesktop>();
            Enumerable.Range(0, WindowsVirtualDesktop.GetInstance().Count).ToList().ForEach(x =>
            {
                VirtualDesktop desktop = new VirtualDesktop(x, this, new Point(_virtualDesktops.Sum(y => y.Size.Width), 0));
                MouseUp += desktop.OnMouseUp;
                MouseDown += desktop.OnMouseDown;
                MouseMove += desktop.OnMouseMove;
                DragOver += desktop.OnDragOver;
                DragDrop += desktop.OnDragDrop;
                virtualDesktops.Add(desktop);
            });
            return virtualDesktops;
        }

        private void OnShown(object sender, EventArgs e)
        {
            // There are two asyncly started loops running as the main loop of the application. Why two?
            Task.Run(async () =>
            {
                // Update application state and always bring the Window to front
                while (!Program.ApplicationClosing.IsCancellationRequested)
                {
                    Invoke(new Action(() =>
                    {
                        try
                        {
                            if (_forceAlwaysOnTop) WindowManager.SetAlwaysOnTop(Handle, _forceAlwaysOnTop);

                            // Change Detection via Hash (calculated on basic parameters of all opened windows)
                            // -> Works now finally as expected because all windows are sorted first to remain in the same 
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
                    await Task.Delay(updateDelay); // TODO: Refresh Rate einstellbar machen
                }
            });

            Task.Run(async () =>
            {
                // App windows can be pinned (in active windows overview) so they are presented on all virtual desktops
                // -> But why forcing here it?
                while (!Program.ApplicationClosing.IsCancellationRequested)
                {
                    Invoke(new Action(() =>
                    {
                        if (!_isAppPinned)
                        {
                            // TODO: Research what it does
                            Debug.WriteLine("App Not Pinned");
                            try
                            {
                                // Divided by Zero error here
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
                    await Task.Delay(500); // was 50, 500 also ok when Invalidate() wieder aktiv gesetzt wird
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

            ToolStripDropDown dropDown = new ToolStripDropDown();
            ToolStripDropDownButton dropDownButton = new ToolStripDropDownButton
            {
                Text = "Position",
                AutoToolTip = false,
                DropDown = dropDown,
                DropDownDirection = ToolStripDropDownDirection.Right
            };

            ToolStripButton buttonRestore = new ToolStripButton("Restore", null, (s, ev) =>
            {
                var storedLocation = Utilties.GetLocationFromRegistry();
                if (storedLocation != null)
                {
                    Location = storedLocation.Value;
                }
            });

            ToolStripButton buttonSave = new ToolStripButton("Save", null, (s, ev) =>
            {
                Utilties.SaveLocationToRegistry(Location);
            });

            dropDown.Items.AddRange(new ToolStripItem[] { buttonRestore, buttonSave });
            menu.Items.Add(dropDownButton);

            Helpers.AddMenuItem(this, menu,
                new ToolStripMenuItem()
                {
                    Text = "Reset",
                    ToolTipText = "Reinitialize Virtual Desktops"
                },
                () =>
                {
                    // Reinitialize Virtual Desktops on redraw failure... but doesn't work?
                    ResetVirtualDesktop();

                    // !: Wenn es nicht in der Taskbar ist, dann erscheint es auch nicht automatisch bei Fensterwechsel!
                    // -> Aber dennoch im primären Screen, von daher könnte man es möglicherweise auch nach Desktopwechsel forcieren
                    //base.ShowInTaskbar = false;
                    //base.Visible = true;

                    // Hide ist nicht das Problem
                    /*
                    base.Hide();
                    await Task.Delay(1000);
                    base.Show();
                    */
                });

            Helpers.AddMenuItem(this, menu,
                new ToolStripMenuItem()
                {
                    Text = "About"
                },
                () =>
                {
                    MessageBox.Show(
                        $"Switchie{Environment.NewLine}v1.1.5{Environment.NewLine}{Environment.NewLine}Made by darkguy2008", "About"
                    );
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
            //Debug.WriteLine("P");

            // OK aber flackrig, an erster Stelle noch am besten, nach ForEach am schlechtesten
            // -> führt im Output jedoch zu massiv: Exception thrown: 'System.Runtime.InteropServices.COMException' in Switchie.exe
            // -> wen wundert's, OnPaint wird ja auch ständig aufgerufen, ich brauche es jedoch nur einmal!
            //if (ShowInTaskbar) ShowInTaskbar = false;

            base.OnPaint(e);

            try
            {
                _virtualDesktops.ForEach(x => x.OnPaint(e));
                //base.Visible = true;
                //base.Show();
                //Opacity = 100; // So wird wenigstens dieser Zoom Effekt unterdrückt

            }
            catch
            {
                // Prüfen, ob hier bei Rendertarget Verlust ein Problem auftritt
                // -> Vielleicht könnte man das per hide forcieren
                WindowsVirtualDesktop.Restart();
                WindowsVirtualDesktopManager.Restart();
            }
        }
    }
}
