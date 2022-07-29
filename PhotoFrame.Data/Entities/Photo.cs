using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Sqlite;

namespace PhotoFrame.Data.Entities;

public class Photo
{
    [Key]
    public Guid PhotoId { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    public string Path { get; set; }

}

