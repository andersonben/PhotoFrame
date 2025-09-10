using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using PhotoFrame.Data;
using PhotoFrame.Data.Entities;
using PhotoFrame.Service.Configuration;

namespace PhotoFrame.Service.Services
{
    public class PhotoDisplayService : BackgroundService
    {
        private readonly ILogger<PhotoDisplayService> _logger;
        private readonly EInkDisplayService _displayService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly DisplaySettings _settings;

        public PhotoDisplayService(
            ILogger<PhotoDisplayService> logger,
            EInkDisplayService displayService,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<DisplaySettings> settings)
        {
            _logger = logger;
            _displayService = displayService;
            _serviceScopeFactory = serviceScopeFactory;
            _settings = settings.Value;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Photo Display Service...");
            
            try
            {
                await _displayService.InitializeAsync();
                _logger.LogInformation("Photo Display Service started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start Photo Display Service");
                throw;
            }

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Photo display loop started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DisplayNextPhotoAsync();
                    
                    var displayDuration = await GetDisplayDurationAsync();
                    _logger.LogDebug("Waiting {Duration} seconds before next photo", displayDuration);
                    
                    await Task.Delay(TimeSpan.FromSeconds(displayDuration), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Photo display service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in photo display loop");
                    
                    // Wait a bit before retrying to avoid rapid error loops
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }

        private async Task DisplayNextPhotoAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PhotoFrameDbContext>();

            var photos = await GetActivePhotosAsync(context);
            
            if (!photos.Any())
            {
                _logger.LogWarning("No active photos found to display");
                await _displayService.ClearDisplayAsync();
                return;
            }

            var photoToDisplay = await SelectNextPhotoAsync(context, photos);
            
            if (photoToDisplay == null)
            {
                _logger.LogWarning("No photo selected for display");
                return;
            }

            var processedImagePath = GetFullImagePath(photoToDisplay.ProcessedPath);
            
            if (!File.Exists(processedImagePath))
            {
                _logger.LogWarning("Processed image file not found: {Path}", processedImagePath);
                
                // Mark photo as inactive if file is missing
                photoToDisplay.IsActive = false;
                await context.SaveChangesAsync();
                return;
            }

            _logger.LogInformation("Displaying photo: {Name} ({Path})", photoToDisplay.Name, photoToDisplay.ProcessedPath);
            
            await _displayService.DisplayImageAsync(processedImagePath);
            
            // Update display statistics
            photoToDisplay.LastDisplayed = DateTime.UtcNow;
            photoToDisplay.DisplayCount++;
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Photo displayed successfully: {Name}", photoToDisplay.Name);
        }

        private async Task<List<Photo>> GetActivePhotosAsync(PhotoFrameDbContext context)
        {
            return await context.Photos
                .Where(p => p.IsActive)
                .ToListAsync();
        }

        private async Task<Photo?> SelectNextPhotoAsync(PhotoFrameDbContext context, List<Photo> photos)
        {
            var enableRandomOrder = await GetRandomOrderSettingAsync(context);
            
            if (enableRandomOrder)
            {
                // Select random photo
                var random = new Random();
                return photos[random.Next(photos.Count)];
            }
            else
            {
                // Select least recently displayed photo
                return photos
                    .OrderBy(p => p.LastDisplayed)
                    .ThenBy(p => p.DisplayCount)
                    .First();
            }
        }

        private async Task<int> GetDisplayDurationAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PhotoFrameDbContext>();

            var setting = await context.Settings
                .FirstOrDefaultAsync(s => s.Name == SettingKeys.DisplayDurationSeconds);

            if (setting != null && int.TryParse(setting.Value, out var duration))
            {
                return Math.Max(60, duration); // Minimum 60 seconds
            }

            return 300; // Default 300 seconds
        }

        private async Task<bool> GetRandomOrderSettingAsync(PhotoFrameDbContext context)
        {
            var setting = await context.Settings
                .FirstOrDefaultAsync(s => s.Name == SettingKeys.EnableRandomOrder);

            if (setting != null && bool.TryParse(setting.Value, out var randomOrder))
            {
                return randomOrder;
            }

            return true; // Default to random order
        }

        private string GetFullImagePath(string relativePath)
        {
            // Convert web path to file system path
            var webRootPath = _settings.WebRootPath ?? "/var/www/photoframe";
            return Path.Combine(webRootPath, relativePath.TrimStart('/'));
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Photo Display Service...");
            
            try
            {
                await _displayService.EnterSleepModeAsync();
                _logger.LogInformation("Photo Display Service stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping Photo Display Service");
            }

            await base.StopAsync(cancellationToken);
        }
    }
}