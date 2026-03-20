using System.Drawing;

namespace Switchie
{
    public class AppSettings
    {
        public int RenderMode { get; set; } = 0;
       
        public int PagerHeight { get; set; } = 40;
        
        public Color DesktopColor { get; set; } = Color.FromArgb(32, 32, 32);
        public Color BackgroundColor { get; set; } = Color.FromArgb(64, 64, 64);
        public Color ActiveDesktopBorderColor { get; set; } = Color.LightBlue;

        public int PrimaryUpdateDelay { get; set; } = 200;
        public int SecondaryUpdateDelay { get; set; } = 500;
    }
}

