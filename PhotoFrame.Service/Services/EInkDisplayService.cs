using System.Device.Gpio;
using System.Device.Spi;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhotoFrame.Service.Configuration;

namespace PhotoFrame.Service.Services
{
    public class EInkDisplayService : IDisposable
    {
        private readonly ILogger<EInkDisplayService> _logger;
        private readonly DisplaySettings _settings;
        private GpioController? _gpio;
        private SpiDevice? _spiDevice;
        private bool _isInitialized = false;

        // IT8951 Commands
        private const ushort IT8951_TCON_SYS_RUN = 0x0001;
        private const ushort IT8951_TCON_STANDBY = 0x0002;
        private const ushort IT8951_TCON_SLEEP = 0x0003;
        private const ushort IT8951_TCON_REG_RD = 0x0010;
        private const ushort IT8951_TCON_REG_WR = 0x0011;
        private const ushort IT8951_TCON_MEM_BST_RD_T = 0x0012;
        private const ushort IT8951_TCON_MEM_BST_RD_S = 0x0013;
        private const ushort IT8951_TCON_MEM_BST_WR = 0x0014;
        private const ushort IT8951_TCON_MEM_BST_END = 0x0015;
        private const ushort IT8951_TCON_LD_IMG = 0x0020;
        private const ushort IT8951_TCON_LD_IMG_AREA = 0x0021;
        private const ushort IT8951_TCON_LD_IMG_END = 0x0022;

        public EInkDisplayService(ILogger<EInkDisplayService> logger, IOptions<DisplaySettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing E-Ink display...");

                // Initialize GPIO
                _gpio = new GpioController();
                
                // Setup pins
                _gpio.OpenPin(_settings.ResetPin, PinMode.Output);
                _gpio.OpenPin(_settings.DataCommandPin, PinMode.Output);
                _gpio.OpenPin(_settings.ChipSelectPin, PinMode.Output);
                _gpio.OpenPin(_settings.BusyPin, PinMode.Input);

                // Initialize SPI
                var spiSettings = new SpiConnectionSettings(0, _settings.ChipSelectPin)
                {
                    ClockFrequency = 12_000_000, // 12MHz
                    Mode = SpiMode.Mode0
                };

                _spiDevice = SpiDevice.Create(spiSettings);

                // Reset the display
                await ResetDisplayAsync();

                // Initialize IT8951
                await InitializeIT8951Async();

                _isInitialized = true;
                _logger.LogInformation("E-Ink display initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize E-Ink display");
                throw;
            }
        }

        private async Task ResetDisplayAsync()
        {
            _logger.LogDebug("Resetting display...");
            
            _gpio!.Write(_settings.ResetPin, PinValue.Low);
            await Task.Delay(100);
            _gpio.Write(_settings.ResetPin, PinValue.High);
            await Task.Delay(100);
            
            // Wait for busy pin to go high then low
            await WaitForBusyAsync();
        }

        private async Task InitializeIT8951Async()
        {
            _logger.LogDebug("Initializing IT8951 controller...");
            
            // Send system run command
            await SendCommandAsync(IT8951_TCON_SYS_RUN);
            await WaitForBusyAsync();
            
            _logger.LogDebug("IT8951 controller initialized");
        }

        private async Task WaitForBusyAsync()
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
            throw new TimeoutException("Display busy timeout");
        }

        private async Task SendCommandAsync(ushort command)
        {
            _gpio!.Write(_settings.DataCommandPin, PinValue.Low); // Command mode
            _gpio.Write(_settings.ChipSelectPin, PinValue.Low);
            
            var commandBytes = BitConverter.GetBytes(command);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(commandBytes);
            }
            
            _spiDevice!.Write(commandBytes);
            
            _gpio.Write(_settings.ChipSelectPin, PinValue.High);
            await Task.Delay(1);
        }

        private async Task SendDataAsync(byte[] data)
        {
            _gpio!.Write(_settings.DataCommandPin, PinValue.High); // Data mode
            _gpio.Write(_settings.ChipSelectPin, PinValue.Low);
            
            _spiDevice!.Write(data);
            
            _gpio.Write(_settings.ChipSelectPin, PinValue.High);
            await Task.Delay(1);
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

                // Convert image to byte array
                var imageData = new byte[image.Width * image.Height];
                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        var pixel = image[x, y];
                        imageData[y * image.Width + x] = pixel.PackedValue;
                    }
                }

                // Load image to display buffer
                await LoadImageToDisplayAsync(imageData, image.Width, image.Height);
                
                // Refresh display
                await RefreshDisplayAsync();

                _logger.LogInformation("Image displayed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to display image: {ImagePath}", imagePath);
                throw;
            }
        }

        private async Task LoadImageToDisplayAsync(byte[] imageData, int width, int height)
        {
            // Send load image command
            await SendCommandAsync(IT8951_TCON_LD_IMG);
            
            // Send image parameters
            var parameters = new byte[8];
            BitConverter.GetBytes((ushort)0).CopyTo(parameters, 0); // X
            BitConverter.GetBytes((ushort)0).CopyTo(parameters, 2); // Y
            BitConverter.GetBytes((ushort)width).CopyTo(parameters, 4); // Width
            BitConverter.GetBytes((ushort)height).CopyTo(parameters, 6); // Height
            
            await SendDataAsync(parameters);
            
            // Send image data in chunks
            const int chunkSize = 4096;
            for (int i = 0; i < imageData.Length; i += chunkSize)
            {
                var chunk = new byte[Math.Min(chunkSize, imageData.Length - i)];
                Array.Copy(imageData, i, chunk, 0, chunk.Length);
                await SendDataAsync(chunk);
            }
            
            // End image load
            await SendCommandAsync(IT8951_TCON_LD_IMG_END);
            await WaitForBusyAsync();
        }

        private async Task RefreshDisplayAsync()
        {
            _logger.LogDebug("Refreshing display...");
            
            // Send display refresh command (this is simplified - actual implementation would depend on specific IT8951 commands)
            await SendCommandAsync(0x0024); // Display refresh command
            await WaitForBusyAsync();
            
            _logger.LogDebug("Display refreshed");
        }

        public async Task ClearDisplayAsync()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Display not initialized");
            }

            _logger.LogInformation("Clearing display...");
            
            // Create white image
            var whiteImage = new byte[_settings.ScreenWidth * _settings.ScreenHeight];
            Array.Fill(whiteImage, (byte)255);
            
            await LoadImageToDisplayAsync(whiteImage, _settings.ScreenWidth, _settings.ScreenHeight);
            await RefreshDisplayAsync();
            
            _logger.LogInformation("Display cleared");
        }

        public async Task EnterSleepModeAsync()
        {
            if (!_isInitialized) return;

            _logger.LogInformation("Entering sleep mode...");
            await SendCommandAsync(IT8951_TCON_SLEEP);
            await WaitForBusyAsync();
        }

        public void Dispose()
        {
            try
            {
                EnterSleepModeAsync().Wait(5000);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error entering sleep mode during disposal");
            }

            _spiDevice?.Dispose();
            _gpio?.Dispose();
            _isInitialized = false;
        }
    }
}