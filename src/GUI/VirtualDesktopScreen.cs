using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
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

                // Order by process is similar to order in taskbar, as long as the user doesn't move them.
                // For windows with the same process, keep a deterministic order by z-order.
                windows = windowsEnumerated
                    .OrderBy(x => x.ProcessID)
                    .ThenBy(x => x.ZOrder)
                    .ToArray();

                /*foreach (var item in windows)
                {
                    Debug.WriteLine(item.ProcessID + ": " + item.Handle + " z" + item.ZOrder);
                }*/
            }
            if (windows.Length == 0) return;

            int iconPaddingX = Form.IconPaddingX;
            int iconPaddingY = Form.IconPaddingY;
            int border = Form.PaddingSize;

            // Reduce available content space by border
            int width = Size.Width - border * 2;
            int height = Size.Height - border * 2;

            // Parition the screen equally for the Icons
            int hSpace = width / windows.Length;
            int lineBreak = windows.Length;

            // Support a second line of icons if we run out of space horizontally
            if (hSpace < windows[0].Icon.Width + iconPaddingX)
            {
                lineBreak = windows.Length / 2 + 1;
                hSpace = width / lineBreak;
            }

            int wcounter = 0;
            int wnum = windows.Length - 1;
            foreach (var wnd in windows)
            {
                Color fillColor = wnd.IsActive ? Form.ActiveWindowColor : Form.WindowColor;
                Color borderColor = wnd.IsActive ? Form.ActiveWindowBorderColor : Form.WindowBorderColor;

                if (Screen.FromHandle(wnd.Handle).DeviceName == AttachedScreen.DeviceName)
                {
                    if (Form.WindowRenderMode == MainForm.RenderMode.Icons)
                    {
                        int yPos;
                        int xPos = border / 2 + Location.X + wcounter * hSpace;

                        if (lineBreak == windows.Length)
                        {
                            yPos = border / 2 + height / 2 - wnd.Icon.Height / 2;
                        }
                        else
                        {
                            if (wcounter < lineBreak)
                            {
                                yPos = border / 2 + height / 4 - (wnd.Icon.Height + iconPaddingY) / 2;
                            }
                            else
                            {
                                xPos -= lineBreak * hSpace;
                                yPos = border / 2 + height / 4 + wnd.Icon.Height + iconPaddingY;
                            }
                        }

                        var areaYPadding = iconPaddingY >= 0 ? iconPaddingY : 0;

                        var selectionArea = new Rectangle(
                            xPos,
                            yPos - areaYPadding,
                            hSpace,
                            wnd.Icon.Height + areaYPadding * 2);

                        WindowAreas[wnd.Handle] = selectionArea;

                        if (wnd.IsActive)
                        {
                            g.FillRectangle(new SolidBrush(fillColor), selectionArea);
                        }

                        g.DrawImage(wnd.Icon, new Point(selectionArea.X + hSpace / 2 - wnd.Icon.Width / 2, yPos));
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
                    wcounter++;
                }
                else
                    continue;
            }
        }
    }
}
