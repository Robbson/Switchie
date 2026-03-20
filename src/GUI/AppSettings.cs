using System.Drawing;

namespace Switchie
{
    public class AppSettings
    {
        public int RenderMode { get; set; } = 0;
        public int DesktopBorderStyle { get; set; } = 0;
        public int PagerHeight { get; set; } = 40;

        public Color BackgroundColor { get; set; } = Color.FromArgb(64, 64, 64);

        public Color DesktopBorderColor { get; set; } = Color.FromArgb(32, 32, 32);
        public Color ActiveDesktopBorderColor { get; set; } = Color.LightBlue;

        public Color WindowColor { get; set; } = Color.FromArgb(255, Color.Gray);
        public Color ActiveWindowColor { get; set; } = Color.FromArgb(255, Color.Silver);
        public Color WindowBorderColor { get; set; } = Color.Silver;
        public Color ActiveWindowBorderColor { get; set; } = Color.White;

        public int PrimaryUpdateDelay { get; set; } = 200;
        public int SecondaryUpdateDelay { get; set; } = 500;
    }
}

