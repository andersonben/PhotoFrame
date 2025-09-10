using System.Device.Gpio;
using System.Device.Spi;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhotoFrame.Service.Configuration;

namespace PhotoFrame.Service.Services
{
    /// <summary>
    /// E-Ink display service specifically for Waveshare 7.8inch e-Paper HAT with IT8951 controller
    /// Based on the Waveshare documentation and IT8951 specifications
    /// </summary>
    public class WaveshareEInkDisplayService : IDisposable
    {
        private readonly ILogger<WaveshareEInkDisplayService> _logger;
        private readonly DisplaySettings _settings;
        private GpioController? _gpio;
        private SpiDevice? _spiDevice;
        private bool _isInitialized = false;
        private float _vcom = -2.0f; // Default VCOM value, should be set from device label

        // IT8951 Commands
        private const ushort CMD_SYS_RUN = 0x0001;
        private const ushort CMD_STANDBY = 0x0002;
        private const ushort CMD_SLEEP = 0x0003;
        private const ushort CMD_REG_RD = 0x0010;
        private const ushort CMD_REG_WR = 0x0011;
        private const ushort CMD_MEM_BST_RD_T = 0x0012;
        private const ushort CMD_MEM_BST_RD_S = 0x0013;
        private const ushort CMD_MEM_BST_WR = 0x0014;
        private const ushort CMD_MEM_BST_END = 0x0015;
        private const ushort CMD_LD_IMG = 0x0020;
        private const ushort CMD_LD_IMG_AREA = 0x0021;
        private const ushort CMD_LD_IMG_END = 0x0022;

        // Display modes
        private const ushort DISPLAY_MODE_INIT = 0;
        private const ushort DISPLAY_MODE_DU = 1;
        private const ushort DISPLAY_MODE_GC16 = 2;
        private const ushort DISPLAY_MODE_GL16 = 3;
        private const ushort DISPLAY_MODE_GLR16 = 4;
        private const ushort DISPLAY_MODE_GLD16 = 5;
        private const ushort DISPLAY_MODE_A2 = 6;

        // Registers
        private const ushort REG_SYS_BASE = 0x0000;
        private const ushort REG_I80CPCR = 0x0004;
        private const ushort REG_MCSR = 0x0200;
        private const ushort REG_LISAR = 0x0208;

        public WaveshareEInkDisplayService(ILogger<WaveshareEInkDisplayService> logger, IOptions<DisplaySettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing Waveshare 7.8inch E-Ink display with IT8951 controller...");

                // Initialize GPIO
                _gpio = new GpioController();
                
                // Setup pins according to Waveshare HAT wiring
                _gpio.OpenPin(_settings.ResetPin, PinMode.Output);
                _gpio.OpenPin(_settings.DataCommandPin, PinMode.Output);
                _gpio.OpenPin(_settings.ChipSelectPin, PinMode.Output);
                _gpio.OpenPin(_settings.BusyPin, PinMode.Input);

                // Initialize SPI with IT8951 specifications
                var spiSettings = new SpiConnectionSettings(0, 0) // Use CE0
                {
                    ClockFrequency = 12_000_000, // 12MHz as recommended by IT8951 datasheet
                    Mode = SpiMode.Mode0,
                    ChipSelectLineActiveState = PinValue.Low,
                    DataFlow = DataFlow.MsbFirst
                };

                _spiDevice = SpiDevice.Create(spiSettings);

                // Hardware reset sequence
                await HardwareResetAsync();

                // Initialize IT8951 controller
                await InitializeControllerAsync();

                // Set VCOM (this should be read from the display label)
                await SetVComAsync(_vcom);

                _isInitialized = true;
                _logger.LogInformation("E-Ink display initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize E-Ink display");
                throw;
            }
        }

        private async Task HardwareResetAsync()
        {
            _logger.LogDebug("Performing hardware reset...");
            
            _gpio!.Write(_settings.ResetPin, PinValue.Low);
            await Task.Delay(100);
            _gpio.Write(_settings.ResetPin, PinValue.High);
            await Task.Delay(100);
            
            // Wait for ready signal
            await WaitForReadyAsync();
        }

        private async Task InitializeControllerAsync()
        {
            _logger.LogDebug("Initializing IT8951 controller...");
            
            // Send system run command
            await WriteCommandAsync(CMD_SYS_RUN);
            await WaitForReadyAsync();
            
            // Read device info
            var deviceInfo = await ReadDeviceInfoAsync();
            _logger.LogInformation("Device Info - Width: {Width}, Height: {Height}, Address: 0x{Address:X8}", 
                deviceInfo.Width, deviceInfo.Height, deviceInfo.MemoryAddress);
            
            _logger.LogDebug("IT8951 controller initialized");
        }

        private async Task<DeviceInfo> ReadDeviceInfoAsync()
        {
            // This is a simplified version - actual implementation would read device registers
            return new DeviceInfo
            {
                Width = _settings.ScreenWidth,
                Height = _settings.ScreenHeight,
                MemoryAddress = 0x001236E0 // Typical IT8951 memory address
            };
        }

        private async Task SetVComAsync(float vcom)
        {
            _logger.LogInformation("Setting VCOM to {VCom}V", vcom);
            
            // Convert VCOM to register value (simplified)
            var vcomValue = (ushort)Math.Abs(vcom * 1000);
            
            await WriteRegisterAsync(0x0000, vcomValue); // Simplified register write
            await WaitForReadyAsync();
        }

        private async Task WaitForReadyAsync()
        {
            var timeout = DateTime.Now.AddSeconds(10);
            while (DateTime.Now < timeout)
            {
                if (_gpio!.Read(_settings.BusyPin) == PinValue.Low)
                {
                    return;
                }
                await Task.Delay(10);
            }
            throw new TimeoutException("Display ready timeout");
        }

        private async Task WriteCommandAsync(ushort command)
        {
            var preamble = new byte[] { 0x60, 0x00 }; // IT8951 command preamble
            var commandBytes = BitConverter.GetBytes(command);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(commandBytes);
            }

            _gpio!.Write(_settings.ChipSelectPin, PinValue.Low);
            _spiDevice!.Write(preamble);
            _spiDevice.Write(commandBytes);
            _gpio.Write(_settings.ChipSelectPin, PinValue.High);
            
            await Task.Delay(1);
        }

        private async Task WriteDataAsync(byte[] data)
        {
            var preamble = new byte[] { 0x00, 0x00 }; // IT8951 data preamble

            _gpio!.Write(_settings.ChipSelectPin, PinValue.Low);
            _spiDevice!.Write(preamble);
            _spiDevice.Write(data);
            _gpio.Write(_settings.ChipSelectPin, PinValue.High);
            
            await Task.Delay(1);
        }

        private async Task WriteRegisterAsync(ushort register, ushort value)
        {
            await WriteCommandAsync(CMD_REG_WR);
            await WriteDataAsync(BitConverter.GetBytes(register));
            await WriteDataAsync(BitConverter.GetBytes(value));
        }

        public async Task DisplayImageAsync(string imagePath)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Display not initialized");
            }

            try
            {
                _logger.LogInformation("Displaying image: {ImagePath}", imagePath);

                using var image = await Image.LoadAsync<L8>(imagePath);
                
                if (image.Width != _settings.ScreenWidth || image.Height != _settings.ScreenHeight)
                {
                    _logger.LogWarning("Image dimensions ({Width}x{Height}) don't match screen dimensions ({ScreenWidth}x{ScreenHeight})", 
                        image.Width, image.Height, _settings.ScreenWidth, _settings.ScreenHeight);
                }

                // Load image to display memory
                await LoadImageAsync(image);
                
                // Display the image using GC16 mode for best quality
                await DisplayAreaAsync(0, 0, image.Width, image.Height, DISPLAY_MODE_GC16);

                _logger.LogInformation("Image displayed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to display image: {ImagePath}", imagePath);
                throw;
            }
        }

        private async Task LoadImageAsync(Image<L8> image)
        {
            _logger.LogDebug("Loading image to display memory...");

            // Send load image command
            await WriteCommandAsync(CMD_LD_IMG);
            
            // Send image area parameters
            var areaData = new byte[8];
            BitConverter.GetBytes((ushort)0).CopyTo(areaData, 0); // X
            BitConverter.GetBytes((ushort)0).CopyTo(areaData, 2); // Y
            BitConverter.GetBytes((ushort)image.Width).CopyTo(areaData, 4); // Width
            BitConverter.GetBytes((ushort)image.Height).CopyTo(areaData, 6); // Height
            
            await WriteDataAsync(areaData);
            
            // Convert image to byte array and send in chunks
            var imageData = new byte[image.Width * image.Height];
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    imageData[y * image.Width + x] = image[x, y].PackedValue;
                }
            }

            // Send image data in chunks to avoid SPI buffer overflow
            const int chunkSize = 4096;
            for (int i = 0; i < imageData.Length; i += chunkSize)
            {
                var chunkLength = Math.Min(chunkSize, imageData.Length - i);
                var chunk = new byte[chunkLength];
                Array.Copy(imageData, i, chunk, 0, chunkLength);
                await WriteDataAsync(chunk);
            }
            
            // End image load
            await WriteCommandAsync(CMD_LD_IMG_END);
            await WaitForReadyAsync();
        }

        private async Task DisplayAreaAsync(int x, int y, int width, int height, ushort mode)
        {
            _logger.LogDebug("Displaying area ({X},{Y}) {Width}x{Height} with mode {Mode}", x, y, width, height, mode);

            await WriteCommandAsync(CMD_LD_IMG_AREA);
            
            var displayData = new byte[10];
            BitConverter.GetBytes((ushort)x).CopyTo(displayData, 0);
            BitConverter.GetBytes((ushort)y).CopyTo(displayData, 2);
            BitConverter.GetBytes((ushort)width).CopyTo(displayData, 4);
            BitConverter.GetBytes((ushort)height).CopyTo(displayData, 6);
            BitConverter.GetBytes(mode).CopyTo(displayData, 8);
            
            await WriteDataAsync(displayData);
            await WaitForReadyAsync();
        }

        public async Task ClearDisplayAsync()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Display not initialized");
            }

            _logger.LogInformation("Clearing display...");
            
            // Create white image
            using var whiteImage = new Image<L8>(_settings.ScreenWidth, _settings.ScreenHeight);
            whiteImage.Mutate(ctx => ctx.Fill(Color.White));
            
            await LoadImageAsync(whiteImage);
            await DisplayAreaAsync(0, 0, _settings.ScreenWidth, _settings.ScreenHeight, DISPLAY_MODE_INIT);
            
            _logger.LogInformation("Display cleared");
        }

        public async Task EnterSleepModeAsync()
        {
            if (!_isInitialized) return;

            _logger.LogInformation("Entering sleep mode...");
            await WriteCommandAsync(CMD_SLEEP);
            await WaitForReadyAsync();
        }

        public void Dispose()
        {
            try
            {
                if (_isInitialized)
                {
                    EnterSleepModeAsync().Wait(5000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error entering sleep mode during disposal");
            }

            _spiDevice?.Dispose();
            _gpio?.Dispose();
            _isInitialized = false;
        }

        private class DeviceInfo
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public uint MemoryAddress { get; set; }
        }
    }
}