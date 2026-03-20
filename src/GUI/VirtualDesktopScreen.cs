using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Switchie
{
    public class VirtualDesktopScreen
    {
        public Size Size { get; set; }
        public MainForm Form { get; set; }
        public Point Location { get; set; }
        public Screen AttachedScreen { get; set; }
        public VirtualDesktop VirtualDesktop { get; set; }
        public Dictionary<IntPtr, Rectangle> WindowAreas { get; set; } = new Dictionary<IntPtr, Rectangle>();

        private Point MousePosition { get => Control.MousePosition; }
        private Window ActiveWindow { get => Form.Windows.SingleOrDefault(x => x.IsActive); }

        public void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            Window[] windows;

            // For windows render mode, all windows have to be sorted in z-order to draw from back to front
            if (Form.WindowRenderMode == MainForm.RenderMode.Windows)
            {
                windows = Form.Windows.Where(x => x.VirtualDesktopIndex == VirtualDesktop.VirtualDesktopIndex).OrderBy(x => x.ZOrder).ToArray();
            }

            // Otherwise order should stay the same (using process id)
            else
            {
                var windowsEnumerated = Form.Windows.Where(x => x.VirtualDesktopIndex == VirtualDesktop.VirtualDesktopIndex);

                // Order by process is similar to order is taskbar, as long as the user doesn't move them
                windows = windowsEnumerated.OrderBy(x => x.ProcessID).ToArray();
            }

            if (windows.Length == 0) return;

            // Parition the screen equally for the Icons
            int hSpace = Size.Width / windows.Length;
            
            if(hSpace < windows[0].Icon.Width)
            {
                Debug.WriteLine("Not enough Space!");
            }
            
            //int vSpace = Size.Height / windows
            int wnum = windows.Length - 1;
            foreach (var wnd in windows)
            {
                Color fillColor = wnd.IsActive ? Form.ActiveWindowColor : Form.WindowColor;
                Color borderColor = wnd.IsActive ? Form.ActiveWindowBorderColor : Form.WindowBorderColor;

                if (Screen.FromHandle(wnd.Handle).DeviceName == AttachedScreen.DeviceName)
                {
                    if (Form.WindowRenderMode == MainForm.RenderMode.Icons)
                    {
                        var yPos = (Size.Height / 2) - wnd.Icon.Height / 2;
                        var area = new Rectangle(Location.X + wnum * hSpace, yPos-2, hSpace, wnd.Icon.Height+4);
                        WindowAreas[wnd.Handle] = area;

                        if (wnd.IsActive)
                        {
                            // Rectangle for the selected icon, currently fills to whole height
                            g.FillRectangle(new SolidBrush(fillColor), area);
                        }

                        g.DrawImage(wnd.Icon, new Point(area.X + hSpace / 2 - wnd.Icon.Width / 2, yPos));
                    }
                    else
                    {
                        var x = wnd.Dimensions.X;
                        var y = wnd.Dimensions.Y;
                        x -= AttachedScreen.Bounds.Left;
                        y -= AttachedScreen.Bounds.Top;
                        var area = new Rectangle(x, y, wnd.Dimensions.Width - Form.BorderSize, wnd.Dimensions.Height - Form.BorderSize);

                        // Scale rectangles down to the thumbnail's desired size
                        var ar = Helpers.AspectRatioResize(new Size(AttachedScreen.Bounds.Width, AttachedScreen.Bounds.Height), 0, Form.PagerHeight);
                        float percentageWidth = (float)ar.Width * 100 / AttachedScreen.Bounds.Width;
                        float percentageHeight = (float)ar.Height * 100 / AttachedScreen.Bounds.Height;

                        area.X = (int)(area.X * (percentageWidth / 100));
                        area.Y = (int)(area.Y * (percentageHeight / 100));
                        area.Width = (int)(area.Width * (percentageWidth / 100));
                        area.Height = (int)(area.Height * (percentageWidth / 100));

                        area.X += Location.X;
                        area.Y += Location.Y;
                        WindowAreas[wnd.Handle] = area;

                        // Window rectangle fill
                        g.FillRectangle(
                            new SolidBrush(fillColor),
                            new Rectangle(area.X, area.Y, area.Width - (Form.BorderSize), area.Height - (Form.BorderSize)));

                        // Window icon
                        var oldBounds = e.Graphics.ClipBounds;
                        e.Graphics.Clip = new Region(area);
                        g.DrawImage(wnd.Icon, new Point(
                            (area.X + area.Width / 2) - wnd.Icon.Width / 2,
                            (area.Y + area.Height / 2) - wnd.Icon.Height / 2
                        ));
                        e.Graphics.Clip = new Region(oldBounds);

                        // Window border
                        g.DrawRectangle(
                            new Pen(new SolidBrush(borderColor), Form.BorderSize),
                            new Rectangle(area.X, area.Y, area.Width - (Form.BorderSize), area.Height - (Form.BorderSize)));
                    }
                    wnum--;
                }
                else
                    continue;
            }
        }
    }
}
