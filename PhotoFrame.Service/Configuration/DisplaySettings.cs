namespace PhotoFrame.Service.Configuration
{
    public class DisplaySettings
    {
        public const string SectionName = "DisplaySettings";

        public int ScreenWidth { get; set; } = 1872;
        public int ScreenHeight { get; set; } = 1404;
        public string SpiDevice { get; set; } = "/dev/spidev0.0";
        public int ResetPin { get; set; } = 17;
        public int DataCommandPin { get; set; } = 25;
        public int ChipSelectPin { get; set; } = 8;
        public int BusyPin { get; set; } = 24;
        public string? WebRootPath { get; set; }
        public string DatabasePath { get; set; } = "/var/lib/photoframe/photos.db";
    }
}