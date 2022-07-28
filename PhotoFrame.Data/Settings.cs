using System;
using System.ComponentModel.DataAnnotations;

namespace PhotoFrame.Data
{
    public class Settings
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Vale { get; set; }

        public Settings()
        {
        }
    }
}

