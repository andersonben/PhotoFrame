# PhotoFrame - Raspberry Pi E-Ink Photo Display

A complete .NET 8 solution for displaying photos on a Waveshare 7.8inch e-Paper HAT with IT8951 controller on Raspberry Pi.

## 🎯 Features

- **Web-based photo management** - Upload and manage photos through a modern web interface
- **Automatic image processing** - Converts photos to 16-level grayscale with Floyd-Steinberg dithering
- **E-ink display optimization** - Optimized for 1872×1404 resolution e-ink displays
- **Linux daemon service** - Runs as a systemd service for automatic startup
- **Configurable slideshow** - Adjustable display duration and random/sequential ordering
- **Photo statistics** - Track display count and last shown date

## 🏗️ Architecture

The solution consists of three main projects:

### PhotoFrame.Data
- **Purpose**: Shared database context using SQLite
- **Features**: 
  - Photo metadata storage
  - Configuration settings
  - Display statistics tracking
- **Database**: SQLite with Entity Framework Core

### PhotoFrame.Web
- **Purpose**: Administrative web application for photo management
- **Features**:
  - Photo upload with drag-and-drop
  - Automatic image processing and optimization
  - Photo gallery with thumbnails
  - Settings management
  - Real-time processing feedback
- **Technology**: ASP.NET Core MVC with Bootstrap UI

### PhotoFrame.Service
- **Purpose**: Linux daemon for displaying photos on e-ink screen
- **Features**:
  - Continuous photo slideshow
  - Hardware abstraction for IT8951 controller
  - Configurable display modes and timing
  - Automatic error recovery
- **Technology**: .NET Generic Host with systemd integration

## 🔧 Hardware Requirements

- **Raspberry Pi 4B** (recommended) or compatible SBC
- **Waveshare 7.8inch e-Paper HAT** with IT8951 controller
- **MicroSD card** (32GB+ recommended)
- **Power supply** (official Raspberry Pi power supply recommended)

### Wiring

The Waveshare HAT connects directly to the Raspberry Pi GPIO pins:

| HAT Pin | Pi Pin | Function |
|---------|--------|----------|
| VCC     | 3.3V   | Power    |
| GND     | GND    | Ground   |
| DIN     | GPIO10 | SPI MOSI |
| CLK     | GPIO11 | SPI SCLK |
| CS      | GPIO8  | SPI CE0  |
| DC      | GPIO25 | Data/Command |
| RST     | GPIO17 | Reset    |
| BUSY    | GPIO24 | Busy     |

## 🚀 Installation

### Prerequisites

1. **Raspberry Pi OS** (64-bit recommended)
2. **.NET 8 Runtime** for ARM64
3. **SPI enabled** in raspi-config
4. **GPIO permissions** for the pi user

### Quick Installation

1. **Clone the repository**:
   ```bash
   git clone <repository-url>
   cd PhotoFrame
   ```

2. **Run the deployment script**:
   ```bash
   # Set your Pi's hostname/IP
   export PI_HOST=your-pi-hostname.local
   export PI_USER=pi
   
   # Deploy to Pi
   ./deploy-to-pi.sh
   ```

3. **Reboot the Pi** to enable SPI:
   ```bash
   ssh pi@your-pi-hostname.local
   sudo reboot
   ```

4. **Start the service**:
   ```bash
   sudo systemctl start photoframe
   sudo systemctl status photoframe
   ```

## 📖 Usage

### Web Interface

1. **Access the web interface**: `http://your-pi-hostname:5000`
2. **Upload photos**: Click "Upload Photo" and select image files
3. **Monitor processing**: Watch real-time processing feedback
4. **Manage photos**: View, edit, or delete photos in the gallery
5. **Configure settings**: Adjust display duration and ordering

### Service Management

```bash
# Start the service
sudo systemctl start photoframe

# Stop the service
sudo systemctl stop photoframe

# Check status
sudo systemctl status photoframe

# View logs
journalctl -u photoframe -f

# Enable auto-start
sudo systemctl enable photoframe
```

## ⚙️ Configuration

### Display Settings

Edit `/var/lib/photoframe/appsettings.json`:

```json
{
  "DisplaySettings": {
    "ScreenWidth": 1872,
    "ScreenHeight": 1404,
    "SpiDevice": "/dev/spidev0.0",
    "ResetPin": 17,
    "DataCommandPin": 25,
    "ChipSelectPin": 8,
    "BusyPin": 24,
    "WebRootPath": "/var/www/photoframe",
    "DatabasePath": "/var/lib/photoframe/photos.db"
  }
}
```

## 🔍 Image Processing

The system automatically processes uploaded photos:

1. **Resize**: Maintains aspect ratio, fits within 1872×1404
2. **Background**: Adds white background for letterboxing
3. **Grayscale**: Converts to grayscale
4. **Dithering**: Applies Floyd-Steinberg dithering
5. **Quantization**: Reduces to 16 grayscale levels optimized for e-ink

### Supported Formats

- **Input**: JPG, PNG, BMP, GIF
- **Output**: PNG (optimized for e-ink)

## 🐛 Troubleshooting

### Common Issues

1. **SPI not enabled**:
   ```bash
   sudo raspi-config
   # Interface Options > SPI > Enable
   sudo reboot
   ```

2. **Permission denied on GPIO**:
   ```bash
   sudo usermod -a -G gpio,spi pi
   ```

3. **Service won't start**:
   ```bash
   journalctl -u photoframe -n 50
   # Check logs for specific errors
   ```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

**Happy photo framing! 📸🖼️**