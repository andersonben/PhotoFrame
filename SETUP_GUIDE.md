# PhotoFrame Setup Guide

This guide provides detailed setup instructions for the PhotoFrame project on Raspberry Pi.

## 🔧 Hardware Setup

### Required Components

1. **Raspberry Pi 4B** (4GB+ RAM recommended)
2. **Waveshare 7.8inch e-Paper HAT** (Part: 18504)
3. **MicroSD Card** (32GB+ Class 10)
4. **Power Supply** (Official 5V 3A recommended)
5. **Ethernet cable** or WiFi for network connectivity

### Assembly

1. **Install Raspberry Pi OS**:
   - Use Raspberry Pi Imager
   - Choose "Raspberry Pi OS (64-bit)" 
   - Enable SSH and configure WiFi during imaging

2. **Attach the e-Paper HAT**:
   - Power off the Pi completely
   - Carefully align and press the HAT onto the GPIO pins
   - Ensure all 40 pins are properly connected

3. **Connect the display**:
   - Connect the ribbon cable from HAT to display
   - Note the VCOM value printed on the cable (e.g., -2.17V)
   - Secure the display in your chosen frame/mount

## 🖥️ Raspberry Pi Configuration

### Initial Setup

```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install required packages
sudo apt install -y git curl wget

# Enable SPI interface
sudo raspi-config
# Navigate: Interface Options > SPI > Enable > Finish
# Reboot when prompted
```

### Install .NET 8

```bash
# Download and install .NET 8
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0

# Add to PATH
echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
source ~/.bashrc

# Verify installation
dotnet --version
```

### Configure GPIO Permissions

```bash
# Add user to required groups
sudo usermod -a -G gpio,spi pi

# Create udev rule for GPIO access
sudo tee /etc/udev/rules.d/99-gpio.rules << EOF
KERNEL=="gpiochip*", GROUP="gpio", MODE="0660"
SUBSYSTEM=="spidev", GROUP="spi", MODE="0660"
EOF

# Reload udev rules
sudo udevadm control --reload-rules
sudo udevadm trigger
```

## 📦 Software Installation

### Method 1: Automated Deployment

```bash
# On your development machine
git clone <your-repo-url>
cd PhotoFrame

# Set Pi connection details
export PI_HOST=raspberrypi.local  # or IP address
export PI_USER=pi

# Deploy to Pi
./deploy-to-pi.sh
```

### Method 2: Manual Installation

```bash
# On Raspberry Pi
git clone <your-repo-url>
cd PhotoFrame

# Build projects
dotnet restore
dotnet build

# Publish service for ARM64
dotnet publish PhotoFrame.Service/PhotoFrame.Service.csproj \
  -c Release -r linux-arm64 --self-contained true \
  -p:PublishSingleFile=true -o ./publish/service

# Publish web app
dotnet publish PhotoFrame.Web/PhotoFrame.Web.csproj \
  -c Release -r linux-arm64 --self-contained true \
  -o ./publish/web

# Create directories
sudo mkdir -p /var/lib/photoframe
sudo mkdir -p /var/www/photoframe
sudo chown -R pi:pi /var/lib/photoframe
sudo chown -R www-data:www-data /var/www/photoframe

# Install service
sudo cp ./publish/service/PhotoFrame.Service /usr/local/bin/photoframe-service
sudo chmod +x /usr/local/bin/photoframe-service

# Install web app
sudo cp -r ./publish/web/* /var/www/photoframe/

# Install systemd service
sudo cp PhotoFrame.Service/photoframe.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable photoframe

# Copy configuration
cp PhotoFrame.Service/appsettings.json /var/lib/photoframe/
```

## ⚙️ Configuration

### Display Settings

Edit `/var/lib/photoframe/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "PhotoFrame.Service": "Debug"
    }
  },
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
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/var/lib/photoframe/photos.db"
  }
}
```

### VCOM Calibration

**Important**: Set the correct VCOM value for your display!

1. Find the VCOM value printed on your display's ribbon cable
2. Update the service code or add to configuration:

```csharp
// In WaveshareEInkDisplayService.cs
private float _vcom = -2.17f; // Replace with your display's value
```

### Web Server Configuration (Optional)

For production deployment with Nginx:

```bash
# Install Nginx
sudo apt install nginx -y

# Create Nginx configuration
sudo tee /etc/nginx/sites-available/photoframe << EOF
server {
    listen 80;
    server_name _;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_cache_bypass \$http_upgrade;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }
}
EOF

# Enable site
sudo ln -s /etc/nginx/sites-available/photoframe /etc/nginx/sites-enabled/
sudo rm /etc/nginx/sites-enabled/default
sudo systemctl restart nginx
```

## 🚀 Starting the System

### Start Services

```bash
# Start PhotoFrame service
sudo systemctl start photoframe

# Check status
sudo systemctl status photoframe

# Start web application (if not using Nginx)
cd /var/www/photoframe
sudo -u www-data dotnet PhotoFrame.Web.dll --urls="http://0.0.0.0:5000"
```

### Verify Operation

1. **Check service logs**:
   ```bash
   journalctl -u photoframe -f
   ```

2. **Test SPI communication**:
   ```bash
   ls -la /dev/spi*
   # Should show: /dev/spidev0.0 and /dev/spidev0.1
   ```

3. **Test GPIO access**:
   ```bash
   # Install gpio tools
   sudo apt install gpiod -y
   
   # Test GPIO pins
   gpioinfo | grep -E "(17|24|25|8)"
   ```

4. **Access web interface**:
   - Open browser to `http://your-pi-ip:5000`
   - Upload a test photo
   - Verify processing and display

## 🐛 Troubleshooting

### Common Issues

#### Service Won't Start

```bash
# Check detailed logs
journalctl -u photoframe -n 100 --no-pager

# Common causes:
# 1. SPI not enabled
sudo raspi-config  # Interface Options > SPI > Enable

# 2. Permission issues
sudo chown -R pi:pi /var/lib/photoframe
sudo usermod -a -G gpio,spi pi

# 3. Missing dependencies
sudo apt install -y libgdiplus libc6-dev
```

#### Display Not Updating

```bash
# Check SPI device
ls -la /dev/spi*

# Test SPI communication
sudo apt install spi-tools -y
spi-config -d /dev/spidev0.0 -q

# Check GPIO pins
gpioget gpiochip0 24  # Should return 0 or 1
```

#### Web Interface Issues

```bash
# Check if web app is running
sudo netstat -tlnp | grep :5000

# Check web app logs
sudo journalctl -u photoframe-web -f

# Test database connection
sqlite3 /var/lib/photoframe/photos.db ".tables"
```

#### Image Processing Errors

```bash
# Check ImageSharp dependencies
ldd /usr/local/bin/photoframe-service | grep -i image

# Install missing libraries
sudo apt install -y libgdiplus libharfbuzz0b libfontconfig1
```

### Performance Optimization

#### Memory Usage

```bash
# Monitor memory usage
free -h
sudo systemctl status photoframe

# Reduce memory usage in appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"  // Reduce logging
    }
  }
}
```

#### Display Refresh Rate

```bash
# Adjust display duration in database
sqlite3 /var/lib/photoframe/photos.db
UPDATE Settings SET Value = '60' WHERE Name = 'DisplayDurationSeconds';
.quit
```

### Hardware Diagnostics

#### Test Display Connection

```bash
# Create test script
cat > test_display.py << EOF
#!/usr/bin/env python3
import spidev
import RPi.GPIO as GPIO
import time

# Setup
spi = spidev.SpiDev()
spi.open(0, 0)
spi.max_speed_hz = 12000000

GPIO.setmode(GPIO.BCM)
GPIO.setup(17, GPIO.OUT)  # Reset
GPIO.setup(24, GPIO.IN)   # Busy
GPIO.setup(25, GPIO.OUT)  # DC

# Test reset sequence
GPIO.output(17, GPIO.LOW)
time.sleep(0.1)
GPIO.output(17, GPIO.HIGH)
time.sleep(0.1)

print("Reset complete, busy pin:", GPIO.input(24))

GPIO.cleanup()
spi.close()
EOF

python3 test_display.py
```

## 📊 Monitoring

### System Health

```bash
# Create monitoring script
cat > /home/pi/monitor_photoframe.sh << EOF
#!/bin/bash
echo "=== PhotoFrame Status ==="
echo "Service Status:"
systemctl is-active photoframe

echo "Memory Usage:"
free -h | grep Mem

echo "Disk Usage:"
df -h /var/lib/photoframe

echo "Recent Logs:"
journalctl -u photoframe --since "1 hour ago" --no-pager | tail -10

echo "Photo Count:"
sqlite3 /var/lib/photoframe/photos.db "SELECT COUNT(*) FROM Photos WHERE IsActive = 1;"
EOF

chmod +x /home/pi/monitor_photoframe.sh
```

### Automated Monitoring

```bash
# Add to crontab for regular monitoring
crontab -e

# Add line:
*/30 * * * * /home/pi/monitor_photoframe.sh >> /var/log/photoframe-monitor.log 2>&1
```

## 🔄 Maintenance

### Regular Tasks

```bash
# Weekly maintenance script
cat > /home/pi/maintain_photoframe.sh << EOF
#!/bin/bash
# Clean old logs
sudo journalctl --vacuum-time=7d

# Update system
sudo apt update && sudo apt upgrade -y

# Restart service
sudo systemctl restart photoframe

# Check disk space
df -h /var/lib/photoframe
EOF

chmod +x /home/pi/maintain_photoframe.sh
```

### Backup

```bash
# Backup script
cat > /home/pi/backup_photoframe.sh << EOF
#!/bin/bash
BACKUP_DIR="/home/pi/backups/$(date +%Y%m%d)"
mkdir -p $BACKUP_DIR

# Backup database
cp /var/lib/photoframe/photos.db $BACKUP_DIR/

# Backup photos
cp -r /var/www/photoframe/photos $BACKUP_DIR/

# Backup configuration
cp /var/lib/photoframe/appsettings.json $BACKUP_DIR/

echo "Backup completed: $BACKUP_DIR"
EOF

chmod +x /home/pi/backup_photoframe.sh
```

## 🎯 Advanced Configuration

### Custom Display Modes

Modify `WaveshareEInkDisplayService.cs` to add custom display modes:

```csharp
// Add custom display mode constants
private const ushort DISPLAY_MODE_FAST = 7;  // Custom fast refresh

// Use in DisplayAreaAsync
await DisplayAreaAsync(0, 0, width, height, DISPLAY_MODE_FAST);
```

### Photo Scheduling

Add scheduling logic to `PhotoDisplayService.cs`:

```csharp
// Add time-based photo selection
private async Task<Photo?> SelectPhotoByTimeAsync(List<Photo> photos)
{
    var hour = DateTime.Now.Hour;
    
    // Morning photos (6-12)
    if (hour >= 6 && hour < 12)
        return photos.Where(p => p.Name.Contains("morning")).FirstOrDefault() ?? photos.First();
    
    // Evening photos (18-22)
    if (hour >= 18 && hour < 22)
        return photos.Where(p => p.Name.Contains("evening")).FirstOrDefault() ?? photos.First();
    
    return photos.First();
}
```

### Remote Management

Set up SSH key authentication for remote management:

```bash
# On your development machine
ssh-keygen -t rsa -b 4096 -C "photoframe-admin"
ssh-copy-id pi@your-pi-hostname

# Now you can deploy without password
./deploy-to-pi.sh
```

---

This completes the comprehensive setup guide for the PhotoFrame project. Follow these instructions carefully, and you'll have a fully functional e-ink photo frame running on your Raspberry Pi!