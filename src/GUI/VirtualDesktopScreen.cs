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
            windows = Form.Windows.Where(x => x.VirtualDesktopIndex == VirtualDesktop.VirtualDesktopIndex).OrderBy(x => x.ZOrder).ToArray();

            /*
            if (Form.WindowRenderMode == MainForm.RenderMode.Thumbnails)
            {
                
            }
            else
            {
                windows = Form.Windows.Where(x => x.VirtualDesktopIndex == VirtualDesktop.VirtualDesktopIndex).OrderByDescending(x => x.ZOrder).ToArray();
            }*/

            int hSpace = Size.Width / windows.Length;
            int vSpace = Size.Height / windows.Length;

            // Renders the thumbnails of all windows for all desktops from back to front
            // -> TODO: Different rendering modes, like when all windows are maximized (on smaller screens typical)
            /*
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(w.Handle, ref placement);
            */

            int wnum = windows.Length - 1;
            foreach (var w in windows)
            {
                Color fillColor = w.IsActive ? Form.ActiveWindowColor : Form.WindowColor;
                Color borderColor = w.IsActive ? Form.ActiveWindowBorderColor : Form.WindowBorderColor;

                if (Screen.FromHandle(w.Handle).DeviceName == AttachedScreen.DeviceName)
                {
                    if (Form.WindowRenderMode == MainForm.RenderMode.Icons)
                    {
                        // TODO: Die Window-Select Methode arbeitet noch nicht mit diesem Modus!

                        // Window rectangle fill
                        /*
                        g.FillRectangle(
                            new SolidBrush(fillColor),
                            new Rectangle(Location.X + wnum * hSpace, 0, hSpace, Size.Height));*/

                        g.DrawImage(w.Icon, new Point(
                            Location.X + wnum * hSpace + hSpace / 2 - w.Icon.Width / 2,
                            (Size.Height / 2) - w.Icon.Height / 2
                        ));
                    }
                    else
                    {
                        var x = w.Dimensions.X;
                        var y = w.Dimensions.Y;
                        x -= AttachedScreen.Bounds.Left;
                        y -= AttachedScreen.Bounds.Top;
                        var area = new Rectangle(x, y, w.Dimensions.Width - Form.BorderSize, w.Dimensions.Height - Form.BorderSize);

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
                        WindowAreas[w.Handle] = area;

                        // Window rectangle fill
                        g.FillRectangle(
                            new SolidBrush(fillColor),
                            new Rectangle(area.X, area.Y, area.Width - (Form.BorderSize), area.Height - (Form.BorderSize)));

                        // Window icon
                        var oldBounds = e.Graphics.ClipBounds;
                        e.Graphics.Clip = new Region(area);
                        g.DrawImage(w.Icon, new Point(
                            (area.X + area.Width / 2) - w.Icon.Width / 2,
                            (area.Y + area.Height / 2) - w.Icon.Height / 2
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