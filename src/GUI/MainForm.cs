using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;

namespace Switchie.GUI
{
    // Pager main application form
    public class MainForm : Form
    {
        public ConcurrentBag<Window> Windows = new ConcurrentBag<Window>();

        private readonly string version =
            typeof(MainForm).Assembly.GetName().Version?.ToString(3)
            ?? Application.ProductVersion;

        // --- Internal Application State ---
        private readonly List<VirtualDesktop> _virtualDesktops = new List<VirtualDesktop>();
        private bool _isAppPinned = false;
        private int _activeDesktopIndex = 0;
        private int _currentDesktopCount = 0;
        private string _windowsHash = string.Empty;

        private Point dragOffset;

        public MainForm()
        {
            SuspendLayout();

            // Form properties set to defaults
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
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Icon = new System.Drawing.Icon(
                new MemoryStream(Helpers.GetResourceFromAssembly(typeof(Program), "Switchie.Resources.icon.ico")));

            var storedLocation = RegistryAccess.RestoreLocation();
            Location = storedLocation ?? GetDefaultLocation();

            Options = new AppSettings();
            Options = AppSettingsStore.Load();
            ApplySettings(Options, false);

            // Form properties set to configurable options
            BackColor = Options.DesktopBackgroundColor;
            Opacity = Options.BackgroundOpacity;
            ShowInTaskbar = Options.ShowAppInTaskbar;

            // Collect all virtual desktops and add mouse event listeners to them (uses PagerHeight)
            GetVirtualDesktopsAndAddMouseHandlers(_virtualDesktops);

            // Pager size depending on current amount of virtual desktops
            Size = new Size(_virtualDesktops.Sum(x => x.Size.Width), Options.PagerHeight);
            MinimumSize = Size;
            MaximumSize = Size;
            ClientSize = Size;

            ResumeLayout(false);

            // Event handler registration
            Shown += OnShown;
            MouseUp += OnMouseUp;
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
        }

        public bool IsDraggingWindow { get; set; }

        // Why property?
        public int BorderSize { get; } = 1;

        // Application Options: Configurable by user
        public AppSettings Options { get; private set; }

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

        // Default app window location: Centered and above Taskbar
        private Point GetDefaultLocation() => new Point(
                (Screen.PrimaryScreen.Bounds.Width / 2) - (Size.Width / 2),
                Screen.PrimaryScreen.WorkingArea.Bottom - Size.Height);

        // App Window covering detection: Currently only by its center point
        private bool IsCovered()
        {
            var rect = this.Bounds;
            var point = new WinAPI.POINT
            {
                X = rect.Left + rect.Width / 2,
                Y = rect.Top + rect.Height / 2,
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
            var newSize = new Size(_virtualDesktops.Sum(d => d.Size.Width), Options.PagerHeight);
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
                            // Probably not required anymore, now that we have covering detection
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
                            $"{string.Join(string.Empty, Windows.Select(x => x.IsActive ? 1 : 0))}" +
                            $"{string.Join(string.Empty, Windows.Select(x => WinAPI.GetForegroundWindow() == x.Handle ? 1 : 0))}" +
                            $"{string.Join(string.Empty, Windows.Select(x => x.VirtualDesktopIndex))}";

                            if (hash != _windowsHash)
                            {
                                _windowsHash = hash;
                                Invalidate();
                            }
                        }
                        catch
                        {
                        }
                    }));

                    // So we don't waste too many CPU cycles
                    await Task.Delay(Options.PrimaryUpdateDelay);
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
                            // TODO: Adjust the list length for the individual view settings

                            ResetVirtualDesktop();
                        }

                        // This works only when the pinning is manually done by the user after application has started
                        // (because auto pinning will always fail)

                        // This should only be the case at start up, right
                        if (Options.ShowAppInTaskbar && !_isAppPinned)
                        {
                            try
                            {
                                // Note: The internal Divided by Zero error happens here
                                WindowsVirtualDesktopManager.GetInstance().PinApplication(Handle);
                                _isAppPinned = true;
                            }
                            catch
                            {
                            }
                        }

                        try
                        {
                            _activeDesktopIndex = WindowsVirtualDesktopManager.GetInstance()
                                .FromDesktop(WindowsVirtualDesktop.GetInstance().Current);
                            Windows = new ConcurrentBag<Window>(WindowManager.GetOpenWindows());
                            //Invalidate();
                        }
                        catch
                        {
                        }
                    }));
                    await Task.Delay(Options.SecondaryUpdateDelay);
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
            //_forceAppAlwaysOnTop = false;
            ContextMenuStrip menu = new ContextMenuStrip();

            Helpers.AddMenuItem(this, menu,
                 new ToolStripMenuItem()
                 {
                     Text = "Toggle Desktop Mode",
                     Image = Helpers.CreateGlyphBitmap("↔"),
                 },
                 () =>
                 {
                     //TODO: Individual Mode per desktop
                     Options.DesktopModeDefault = Options.DesktopModeDefault == DesktopMode.MicroTaskbar ? DesktopMode.Preview : DesktopMode.MicroTaskbar;
                     AppSettingsStore.Save(Options);
                     Invalidate();
                 });

            // --- Position related ---
            ToolStripMenuItem positionMenu = new ToolStripMenuItem()
            {
                Text = "Position",
                Image = Helpers.CreateGlyphBitmap("⌖"),
            };

            ToolStripMenuItem buttonRestorePos = new ToolStripMenuItem("Restore", null, (s, ev) =>
            {
                var storedLocation = RegistryAccess.RestoreLocation();
                if (storedLocation != null)
                {
                    Location = storedLocation.Value;
                }
            });

            ToolStripMenuItem buttonSavePos = new ToolStripMenuItem("Save", null, (s, ev) =>
            {
                RegistryAccess.SaveLocation(Location);
            });

            ToolStripMenuItem buttonDefaultPos = new ToolStripMenuItem("Default", null, (s, ev) =>
            {
                Location = GetDefaultLocation();
            });

            positionMenu.DropDownItems.AddRange(new ToolStripItem[] { buttonRestorePos, buttonSavePos, buttonDefaultPos });
            menu.Items.Add(positionMenu);

            // --- Aditional menu entries ---
            Helpers.AddMenuItem(this, menu,
                new ToolStripMenuItem()
                {
                    Text = "Settings",
                    Image = Helpers.CreateGlyphBitmap("⚙"),
                },
                () =>
                {
                    OpenSettingsDialog();
                });

            Helpers.AddMenuItem(this, menu,
                new ToolStripMenuItem()
                {
                    Text = "About",
                    Image = Helpers.CreateGlyphBitmap("ℹ"),
                },
                () =>
                {
                    OpenAboutDialog();
                    //_forceAppAlwaysOnTop = true;
                });

            Helpers.AddMenuItem(this, menu,
                new ToolStripMenuItem()
                {
                    Text = "Exit",
                    Image = Helpers.CreateGlyphBitmap("✕"),
                },
                () =>
                {
                    Environment.Exit(1);
                });

            //menu.Opened += (ss, ee) => _forceAppAlwaysOnTop = false;
            menu.Show(this, PointToClient(Cursor.Position));
        }

        private void OpenSettingsDialog()
        {
            var originalSettings = Options; //AppSettings.Clone(Options);
            var settingsWindow = new AppSettingsWindow(Options);
            new WindowInteropHelper(settingsWindow) { Owner = Handle };

            settingsWindow.PreviewChanged += s => PreviewSettings(s);

            bool? result = settingsWindow.ShowDialog();
            if (result == true)
            {
                Options = settingsWindow.Settings;
                ApplySettings(Options, true); // save and reset to new settings
            }
            else
            {
                Options = originalSettings;
                ApplySettings(Options, false); // restore visuals
            }
        }

        private void PreviewSettings(AppSettings settings)
        {
            ApplySettings(settings, false);

            // Apply MainForm propertes
            ShowInTaskbar = Options.ShowAppInTaskbar;
            BackColor = Options.DesktopBackgroundColor;
            Opacity = Math.Max(0.25, Math.Min(1.0, Options.BackgroundOpacity));

            Invalidate();
        }

        private void ApplySettings(AppSettings settings, bool persist)
        {
            Options = settings;

            // Apply MainForm propertes
            ShowInTaskbar = Options.ShowAppInTaskbar;
            BackColor = Options.DesktopBackgroundColor;
            Opacity = Math.Max(0.25, Math.Min(1.0, Options.BackgroundOpacity));

            Invalidate();

            if (persist)
            {
                AppSettingsStore.Save(settings);
                ResetVirtualDesktop();
            }
        }

        // TODO: Seperate it from the MainForm
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
                    Location = new Point((aboutDialog.ClientSize.Width - 96) / 2, 20),
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
                    Text = "Switchie",
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
                    Text = $"Version {version}",
                };

                var authorLabel = new Label
                {
                    AutoSize = false,
                    Width = aboutDialog.ClientSize.Width - 40,
                    Height = 24,
                    Location = new Point(20, 202),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Text = "Main author: darkguy2008",
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
                           "\n• DARKGuy (Alemar)",
                };

                var closeButton = new Button
                {
                    Text = "Close",
                    Width = 100,
                    Height = 30,
                    Location = new Point((aboutDialog.ClientSize.Width - 100) / 2, 298),
                    DialogResult = DialogResult.OK,
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
    }
}
