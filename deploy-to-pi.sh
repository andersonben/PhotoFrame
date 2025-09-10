#!/bin/bash

# PhotoFrame Deployment Script for Raspberry Pi
# This script builds and deploys the PhotoFrame application to a Raspberry Pi

set -e

# Configuration
PI_HOST="${PI_HOST:-photoframe.local}"
PI_USER="${PI_USER:-pi}"
DEPLOY_PATH="/home/pi/photoframe"
SERVICE_PATH="/usr/local/bin/photoframe-service"
SYSTEMD_PATH="/etc/systemd/system/photoframe.service"
WEB_PATH="/var/www/photoframe"
DATA_PATH="/var/lib/photoframe"

echo "🚀 Starting PhotoFrame deployment to $PI_USER@$PI_HOST"

# Build the projects
echo "📦 Building PhotoFrame.Service..."
dotnet publish PhotoFrame.Service/PhotoFrame.Service.csproj \
    -c Release \
    -r linux-arm64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -o ./publish/service

echo "📦 Building PhotoFrame.Web..."
dotnet publish PhotoFrame.Web/PhotoFrame.Web.csproj \
    -c Release \
    -r linux-arm64 \
    --self-contained true \
    -o ./publish/web

# Create deployment package
echo "📦 Creating deployment package..."
mkdir -p ./deploy
cp -r ./publish/service/* ./deploy/
cp -r ./publish/web/* ./deploy/web/
cp PhotoFrame.Service/photoframe.service ./deploy/
cp PhotoFrame.Service/appsettings.json ./deploy/

# Create installation script
cat > ./deploy/install.sh << 'EOF'
#!/bin/bash
set -e

echo "🔧 Installing PhotoFrame on Raspberry Pi..."

# Stop existing service if running
sudo systemctl stop photoframe || true

# Create directories
sudo mkdir -p /var/lib/photoframe
sudo mkdir -p /var/www/photoframe
sudo mkdir -p /var/log/photoframe

# Set permissions
sudo chown -R pi:pi /var/lib/photoframe
sudo chown -R pi:pi /var/www/photoframe
sudo chown -R pi:pi /var/log/photoframe

# Install service binary
sudo cp photoframe-service /usr/local/bin/
sudo chmod +x /usr/local/bin/photoframe-service

# Install web application
sudo cp -r web/* /var/www/photoframe/
sudo chown -R www-data:www-data /var/www/photoframe

# Install systemd service
sudo cp photoframe.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable photoframe

# Install configuration
cp appsettings.json /var/lib/photoframe/

# Enable SPI
if ! grep -q "dtparam=spi=on" /boot/config.txt; then
    echo "dtparam=spi=on" | sudo tee -a /boot/config.txt
fi

# Add user to gpio and spi groups
sudo usermod -a -G gpio,spi pi

echo "✅ PhotoFrame installed successfully!"
echo "📝 Please reboot the Pi to enable SPI, then start the service with:"
echo "   sudo systemctl start photoframe"
echo "   sudo systemctl status photoframe"
EOF

chmod +x ./deploy/install.sh

# Copy to Raspberry Pi
echo "📤 Copying files to Raspberry Pi..."
scp -r ./deploy/* $PI_USER@$PI_HOST:$DEPLOY_PATH/

# Run installation
echo "🔧 Running installation on Raspberry Pi..."
ssh $PI_USER@$PI_HOST "cd $DEPLOY_PATH && sudo ./install.sh"

echo "✅ Deployment completed!"
echo ""
echo "🎯 Next steps:"
echo "1. SSH to your Pi: ssh $PI_USER@$PI_HOST"
echo "2. Reboot to enable SPI: sudo reboot"
echo "3. Start the service: sudo systemctl start photoframe"
echo "4. Check status: sudo systemctl status photoframe"
echo "5. View logs: journalctl -u photoframe -f"
echo ""
echo "🌐 Web interface will be available at: http://$PI_HOST:5000"
echo "📁 Upload photos through the web interface"
echo "⚙️  Configure display settings in /var/lib/photoframe/appsettings.json"

# Cleanup
rm -rf ./publish ./deploy

echo "🧹 Cleanup completed"