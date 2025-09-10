using System.ComponentModel.DataAnnotations;

namespace PhotoFrame.Data.Entities;

public class Photo
{
    [Key]
    public Guid PhotoId { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string OriginalPath { get; set; } = string.Empty;
    
    [Required]
    public string ProcessedPath { get; set; } = string.Empty;
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastDisplayed { get; set; }
    
    public int DisplayCount { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    
    public long FileSizeBytes { get; set; }
    
    public int OriginalWidth { get; set; }
    
    public int OriginalHeight { get; set; }
}

