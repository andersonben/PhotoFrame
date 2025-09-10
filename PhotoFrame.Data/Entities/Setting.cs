using System.ComponentModel.DataAnnotations;

namespace PhotoFrame.Data.Entities
{
    public class Setting
    {
        [Key]
        public Guid SettingId { get; set; }
        
        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        
        [Required, MaxLength(255)]
        public string Value { get; set; } = string.Empty;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public static class SettingKeys
    {
        public const string DisplayDurationSeconds = "DisplayDurationSeconds";
        public const string PhotoDirectory = "PhotoDirectory";
        public const string ProcessedPhotoDirectory = "ProcessedPhotoDirectory";
        public const string EnableRandomOrder = "EnableRandomOrder";
        public const string ScreenWidth = "ScreenWidth";
        public const string ScreenHeight = "ScreenHeight";
        public const string SpiDevice = "SpiDevice";
        public const string ResetPin = "ResetPin";
        public const string DataCommandPin = "DataCommandPin";
        public const string ChipSelectPin = "ChipSelectPin";
        public const string BusyPin = "BusyPin";
    }
}

