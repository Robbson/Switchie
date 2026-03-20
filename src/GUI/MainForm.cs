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
using System.Windows.Interop;

namespace Switchie
{
    // Pager main application form
    public class MainForm : Form
    {
        private string version = "1.2.0";

        // --- Internal Application State ---
        private bool _isAppPinned = false;
        private bool _forceAppAlwaysOnTop = false;

        private int _activeDesktopIndex = 0;
        private int _currentDesktopCount = 0;

        private string _windowsHash = string.Empty;
        public ConcurrentBag<Window> Windows = new ConcurrentBag<Window>();
        private readonly List<VirtualDesktop> _virtualDesktops = new List<VirtualDesktop>();

        private Point dragOffset;
        public bool IsDraggingWindow { get; set; }

        private readonly WinEventHook.WinEventDelegate _proc;
        private readonly IntPtr _hook;

        // --- Application Settings: Static ---
        public int BorderSize { get; } = 1;

        // --- Application Settings: Configurable by user ---
        private AppSettings _appSettings;

        public Color DesktopColor { get; set; } = Color.FromArgb(32, 32, 32); // Background inbetween desktops
        public Color BackgroundColor { get; set; } = Color.FromArgb(64, 64, 64);
        public Color WindowColor { get; set; } = Color.FromArgb(255, Color.Gray);
        public Color WindowBorderColor { get; set; } = Color.Silver;

        public Color ActiveWindowColor { get; set; } = Color.FromArgb(255, Color.Silver);
        public Color ActiveWindowBorderColor { get; set; } = Color.White;
        public Color ActiveDesktopBorderColor { get; set; } = Color.LightBlue;

        public enum BorderStyle { Box, Underline };
        public BorderStyle ActiveDesktopBorderStyle { get; set; } = BorderStyle.Box;

        public enum RenderMode { Windows, Icons }
        public RenderMode WindowRenderMode { get; set; } = RenderMode.Windows;

        public int PagerHeight { get; set; } = 40; // Width is calculated from that

        private int primaryUpdateDelay = 200;
        private int secondaryUpdateDelay = 500;

        // This works only when the pinning is manually done by the user after application has started
        // (because auto pinning will always fail)
        private bool _showAppInTaskbar = true;

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

            _appSettings = AppSettingsStore.Load();
            ApplySettings(_appSettings, false);

            BackColor = BackgroundColor;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Icon = new System.Drawing.Icon(
                new MemoryStream(Helpers.GetResourceFromAssembly(typeof(Program), "Switchie.Resources.icon.ico")));

            ShowInTaskbar = _showAppInTaskbar;

            // Collect all virtual desktops and add mouse event listeners to them
            GetVirtualDesktopsAndAddMouseHandlers(_virtualDesktops);

            // Pager size depending on current amount of virtual desktops
            Size = new Size(_virtualDesktops.Sum(x => x.Size.Width), PagerHeight);
            MinimumSize = Size;
            MaximumSize = Size;
            ClientSize = Size;

            var storedLocation = RegistryAccess.RestoreLocation();
            Location = storedLocation ?? GetDefaultLocation();

            ResumeLayout(false);
            Shown += OnShown;
            MouseUp += OnMouseUp;
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
        }

        // Default app window location: Centered and above Taskbar
        private Point GetDefaultLocation() => new Point(
                (Screen.PrimaryScreen.Bounds.Width / 2) - (Size.Width / 2),
                Screen.PrimaryScreen.WorkingArea.Bottom - Size.Height
        );

        // App Window covering detection: Currently only by its center point
        private bool IsCovered()
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

        private bool HasDesktopCountChanged() => _currentDesktopCount != WindowsVirtualDesktop.GetInstance().Count;

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
                            // Probably now required anymore, now that we have covering detection
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

                    // So we don't waste too many CPU cycles
                    await Task.Delay(primaryUpdateDelay);
                }
            });

            // Secondary application loop for pinning the app, bringing it to front when hidden by some other window
            // and detect desktop count changes
            // -> only pinned apps are visible on all virtual desktops, which is a requirement for this app 
            Task.Run(async () =>
            {
                while (!Program.ApplicationClosing.IsCancellationRequested)
                {
                    Invoke(new Action(() =>
                    {
                        // Check if window is covered shoulf be fine with every 500ms 
                        if (IsCovered())
                        {
                            WindowManager.RestoreWindow(Handle);
                        }

                        if (HasDesktopCountChanged())
                        {
                            ResetVirtualDesktop();
                        }

                        // This should only be the case at start up, right
                        if (_showAppInTaskbar && !_isAppPinned)
                        {
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
                            _activeDesktopIndex = WindowsVirtualDesktopManager.GetInstance()
                                .FromDesktop(WindowsVirtualDesktop.GetInstance().Current);
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
            _forceAppAlwaysOnTop = false;
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
                Location = GetDefaultLocation();
            });
            dropDown.Items.AddRange(new ToolStripItem[] { buttonRestorePos, buttonSavePos, buttonDefaultPos });
            menu.Items.Add(dropDownButton);

            // --- Aditional menu entries ---
            Helpers.AddMenuItem(this, menu,
             new ToolStripMenuItem()
             {
                 Text = "Toggle Render Mode"
             },
             () =>
             {
                 WindowRenderMode = WindowRenderMode == RenderMode.Icons ? RenderMode.Windows : RenderMode.Icons;
                 _appSettings.RenderMode = (int)WindowRenderMode;
                 AppSettingsStore.Save(_appSettings);
                 Invalidate();
             });

            Helpers.AddMenuItem(this, menu,
                new ToolStripMenuItem()
                {
                    Text = "Settings"
                },
                () =>
                {
                    OpenSettingsDialog();
                });

            Helpers.AddMenuItem(this, menu,
                new ToolStripMenuItem()
                {
                    Text = "About"
                },
                () =>
                {
                    OpenAboutDialog();
                    _forceAppAlwaysOnTop = true;
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

            menu.Opened += (ss, ee) => _forceAppAlwaysOnTop = false;
            menu.Show(this, PointToClient(Cursor.Position));
        }

        private void OpenSettingsDialog()
        {
            var settingsWindow = new AppSettingsWindow(_appSettings);
            new WindowInteropHelper(settingsWindow) { Owner = Handle };

            bool? result = settingsWindow.ShowDialog();
            if (result == true)
            {
                _appSettings = settingsWindow.Settings;
                ApplySettings(_appSettings, true);
            }
        }

        private void OpenAboutDialog()
        {
            using (var aboutDialog = new Form())
            {
                aboutDialog.Text = "About Switchie";
                aboutDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                aboutDialog.StartPosition = FormStartPosition.CenterParent;
                aboutDialog.MaximizeBox = false;
                aboutDialog.MinimizeBox = false;
                aboutDialog.ShowInTaskbar = false;
                aboutDialog.ClientSize = new Size(420, 340);
                aboutDialog.BackColor = Color.White;

                var iconBox = new PictureBox
                {
                    Size = new Size(96, 96),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Location = new Point((aboutDialog.ClientSize.Width - 96) / 2, 20)
                };

                try
                {
                    using (var iconStream = new MemoryStream(
                        Helpers.GetResourceFromAssembly(typeof(Program), "Switchie.Resources.icon.png")))
                    using (var pngImage = Image.FromStream(iconStream))
                    {
                        iconBox.Image = new Bitmap(pngImage);
                    }
                }
                catch
                {
                    iconBox.Image = Icon?.ToBitmap();
                }

                var appNameLabel = new Label
                {
                    AutoSize = false,
                    Width = aboutDialog.ClientSize.Width,
                    Height = 32,
                    Location = new Point(0, 126),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 16, FontStyle.Bold),
                    Text = "Switchie"
                };

                var versionLabel = new Label
                {
                    AutoSize = false,
                    Width = aboutDialog.ClientSize.Width,
                    Height = 24,
                    Location = new Point(0, 160),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 10, FontStyle.Regular),
                    ForeColor = Color.DimGray,
                    Text = $"Version {version}"
                };

                var authorLabel = new Label
                {
                    AutoSize = false,
                    Width = aboutDialog.ClientSize.Width - 40,
                    Height = 24,
                    Location = new Point(20, 202),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Text = "Main author: darkguy2008"
                };

                var contributorsLabel = new Label
                {
                    AutoSize = false,
                    Width = aboutDialog.ClientSize.Width - 40,
                    Height = 70,
                    Location = new Point(20, 226),
                    TextAlign = ContentAlignment.TopLeft,
                    Font = new Font("Segoe UI", 9, FontStyle.Regular),
                    Text = "Contributors:\n• Robbson" +
                           "\n• DARKGuy (Alemar)"
                };

                var closeButton = new Button
                {
                    Text = "Close",
                    Width = 100,
                    Height = 30,
                    Location = new Point((aboutDialog.ClientSize.Width - 100) / 2, 298),
                    DialogResult = DialogResult.OK
                };

                aboutDialog.AcceptButton = closeButton;
                aboutDialog.CancelButton = closeButton;

                aboutDialog.Controls.Add(iconBox);
                aboutDialog.Controls.Add(appNameLabel);
                aboutDialog.Controls.Add(versionLabel);
                aboutDialog.Controls.Add(authorLabel);
                aboutDialog.Controls.Add(contributorsLabel);
                aboutDialog.Controls.Add(closeButton);

                aboutDialog.ShowDialog(this);
            }
        }

        private void ApplySettings(AppSettings settings, bool persist)
        {
            PagerHeight = settings.PagerHeight;

            BackgroundColor = settings.BackgroundColor;
            DesktopColor = settings.DesktopColor;
            ActiveDesktopBorderColor = settings.ActiveDesktopBorderColor;

            primaryUpdateDelay = settings.PrimaryUpdateDelay;
            secondaryUpdateDelay = settings.SecondaryUpdateDelay;

            WindowRenderMode = settings.RenderMode == 1 ? RenderMode.Icons : RenderMode.Windows;

            BackColor = BackgroundColor;

            if (persist)
            {
                AppSettingsStore.Save(settings);
                ResetVirtualDesktop();
                Invalidate();
            }
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
