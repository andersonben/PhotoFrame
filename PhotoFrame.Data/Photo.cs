using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Sqlite;

namespace PhotoFrame.Data;

public class Photo
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    public string Path { get; set; }

}

