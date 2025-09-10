using Microsoft.EntityFrameworkCore;
using PhotoFrame.Data.Entities;

namespace PhotoFrame.Data
{
    public class PhotoFrameDbContext : DbContext
    {
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Setting> Settings { get; set; }

        private string DbPath { get; }

        public PhotoFrameDbContext()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DbPath = Path.Join(path, "photos.db");
        }

        public PhotoFrameDbContext(DbContextOptions<PhotoFrameDbContext> options)
            : base(options)
        {
            if (options == null)
            {
                var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                DbPath = Path.Join(path, "photos.db");
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
            {
                options.UseSqlite($"Data Source={DbPath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed default settings
            var defaultSettings = new[]
            {
                new Setting
                {
                    SettingId = Guid.NewGuid(),
                    Name = SettingKeys.DisplayDurationSeconds,
                    Value = "30",
                    UpdatedAt = DateTime.UtcNow
                },
                new Setting
                {
                    SettingId = Guid.NewGuid(),
                    Name = SettingKeys.PhotoDirectory,
                    Value = "/home/pi/photos/original",
                    UpdatedAt = DateTime.UtcNow
                },
                new Setting
                {
                    SettingId = Guid.NewGuid(),
                    Name = SettingKeys.ProcessedPhotoDirectory,
                    Value = "/home/pi/photos/processed",
                    UpdatedAt = DateTime.UtcNow
                },
                new Setting
                {
                    SettingId = Guid.NewGuid(),
                    Name = SettingKeys.EnableRandomOrder,
                    Value = "true",
                    UpdatedAt = DateTime.UtcNow
                },
                new Setting
                {
                    SettingId = Guid.NewGuid(),
                    Name = SettingKeys.ScreenWidth,
                    Value = "1872",
                    UpdatedAt = DateTime.UtcNow
                },
                new Setting
                {
                    SettingId = Guid.NewGuid(),
                    Name = SettingKeys.ScreenHeight,
                    Value = "1404",
                    UpdatedAt = DateTime.UtcNow
                },
                new Setting
                {
                    SettingId = Guid.NewGuid(),
                    Name = SettingKeys.SpiDevice,
                    Value = "/dev/spidev0.0",
                    UpdatedAt = DateTime.UtcNow
                },
                new Setting
                {
                    SettingId = Guid.NewGuid(),
                    Name = SettingKeys.ResetPin,
                    Value = "17",
                    UpdatedAt = DateTime.UtcNow
                },
                new Setting
                {
                    SettingId = Guid.NewGuid(),
                    Name = SettingKeys.DataCommandPin,
                    Value = "25",
                    UpdatedAt = DateTime.UtcNow
                },
                new Setting
                {
                    SettingId = Guid.NewGuid(),
                    Name = SettingKeys.ChipSelectPin,
                    Value = "8",
                    UpdatedAt = DateTime.UtcNow
                },
                new Setting
                {
                    SettingId = Guid.NewGuid(),
                    Name = SettingKeys.BusyPin,
                    Value = "24",
                    UpdatedAt = DateTime.UtcNow
                }
            };

            modelBuilder.Entity<Setting>().HasData(defaultSettings);
        }
    }
}

