using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Switchie
{
    // Represents a virtual desktop mini view in the pager
    public class VirtualDesktop
    {
        public MainForm Form { get; set; }
        public Size Size { get; set; }
        public Point Location { get; set; }

        public int VirtualDesktopIndex { get; set; }
        private readonly List<VirtualDesktopScreen> _screens = new List<VirtualDesktopScreen>();
        public bool IsCurrentActiveDesktop
        {
            get => WindowsVirtualDesktopManager.GetInstance().FromDesktop(WindowsVirtualDesktop.GetInstance().Current) == VirtualDesktopIndex;
        }

        private DragDropData _dragDropData;
        private Rectangle dragBoxFromMouseDown;

        private bool IsInsideBounds(Point p) => IsInsideBounds(p.X, p.Y);

        private bool IsInsideBounds(int x, int y)
            => x >= Location.X && x < (Location.X + Size.Width) && y >= Location.Y && y < (Location.Y + Size.Height);

        public VirtualDesktop(int virtualDesktopIndex, MainForm form, Point location)
        {
            Form = form;
            Location = location;
            VirtualDesktopIndex = virtualDesktopIndex;

            _screens.Clear();

            // Note: A virtual desktop includes a complete set of screens, if available
            foreach (var screen in Screen.AllScreens.OrderBy(x => x.Bounds.Left).ThenBy(x => x.Bounds.Top))
                _screens.Add(new VirtualDesktopScreen()
                {
                    Form = Form,
                    VirtualDesktop = this,
                    AttachedScreen = screen,
                    Location = new Point(location.X + _screens.Sum(x => x.Size.Width), location.Y),
                    Size = Helpers.AspectRatioResize(new Size(screen.Bounds.Width, screen.Bounds.Height), 0, Form.PagerHeight)
                });
            Size = new Size(_screens.Sum(x => x.Size.Width), Form.PagerHeight);
        }

        private Window GetWindowUnderCursor(Point mousePosition)
        {
            var coord = Form.PointToClient(mousePosition);

            // For windows render mode, we search across all screens for the clicked window in z-order from front to back,
            // otherwise order should stay the same (using process id)
            // -> TODO: this doesn't work reliably for the Icons mode when there are more windows of the same application
            //    because they all share the same process id
            var windows = Form.WindowRenderMode == MainForm.RenderMode.Windows 
                ? Form.Windows.Where(x => x.VirtualDesktopIndex == VirtualDesktopIndex).OrderByDescending(x => x.ZOrder)
                : Form.Windows.Where(w => w.VirtualDesktopIndex == VirtualDesktopIndex).OrderBy(w => w.ProcessID);

            foreach (var w in windows)
            {
                if (_screens.Any(x => x.WindowAreas.ContainsKey(w.Handle) && x.WindowAreas[w.Handle].Contains(mousePosition)))
                    if (_screens.Select(x => x.AttachedScreen.DeviceName).Contains(Screen.FromHandle(w.Handle).DeviceName))
                    {
                        return w;
                    }
            }
            return null;
        }

        public void OnPaint(PaintEventArgs e)
        {
            _screens.ForEach(x => x.OnPaint(e));

            Graphics g = e.Graphics;
            Color desktopBorderColor = IsCurrentActiveDesktop ? Form.ActiveDesktopBorderColor : Form.DesktopBorderColor;

            if (Form.DesktopBorderStyle == MainForm.BorderStyle.Box)
            {
                // Draw a border around the active desktop
                g.DrawRectangle(
                    new Pen(new SolidBrush(desktopBorderColor), Form.BorderSize),
                    new Rectangle(Location.X, Location.Y, Size.Width - (Form.BorderSize), Size.Height - (Form.BorderSize))
                );
            }
            else
            {
                // Just underline the active desktop
                // -> TODO: Doesn't work if there is no window on the desktop
                g.FillRectangle(
                    new SolidBrush(desktopBorderColor),
                    new Rectangle(Location.X, Size.Height - 1, Size.Width, Size.Height)
             );
            }
        }

        public void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (!IsInsideBounds(e.X, e.Y)) return;
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                WindowsVirtualDesktop.GetInstance().FromIndex(VirtualDesktopIndex).MakeVisible();
                Form.Invalidate();
            }
        }

        public void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (!IsInsideBounds(e.X, e.Y)) return;
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                var w = GetWindowUnderCursor(e.Location);
                if (w != null)
                {
                    // Bring a window into front by clicking on its miniature / icon
                    // -> only when it's already the active desktop, which makes desktop switches smoother
                    //    without touching the last window z-order
                    if (IsCurrentActiveDesktop)
                    {
                        WinAPI.SetForegroundWindow(w.Handle);
                    }

                    _dragDropData = new DragDropData()
                    {
                        OriginDesktopIndex = VirtualDesktopIndex,
                        DraggedWindow = w
                    };
                    Size dragSize = SystemInformation.DragSize;
                    dragBoxFromMouseDown = new Rectangle(
                        new Point(e.X - (dragSize.Width / 2), e.Y - (dragSize.Height / 2)), dragSize);
                }
                else
                    dragBoxFromMouseDown = Rectangle.Empty;
            }
        }

        public void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsInsideBounds(e.X, e.Y)) return;
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
                if (dragBoxFromMouseDown != Rectangle.Empty && !dragBoxFromMouseDown.Contains(e.X, e.Y))
                    Form.DoDragDrop(_dragDropData, DragDropEffects.Move);
        }

        public void OnDragDrop(object sender, DragEventArgs e)
        {
            if (!IsInsideBounds(Form.PointToClient(new Point(e.X, e.Y)))) return;
            if (e.Data.GetDataPresent(typeof(DragDropData)))
            {
                var ddd = (DragDropData)e.Data.GetData(typeof(DragDropData));
                if (e.Effect == DragDropEffects.Move)
                {
                    WindowsVirtualDesktop.GetInstance().FromIndex(VirtualDesktopIndex).MoveWindow(ddd.DraggedWindow.Handle);
                    Form.Invalidate();
                }
            }
        }

        public void OnDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(DragDropData)))
            {
                e.Effect = DragDropEffects.None;
                return;
            }
            if ((e.AllowedEffect & DragDropEffects.Move) == DragDropEffects.Move)
                e.Effect = DragDropEffects.Move;
        }
    }
}
