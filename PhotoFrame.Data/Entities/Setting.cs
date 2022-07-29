using System;
using System.ComponentModel.DataAnnotations;

namespace PhotoFrame.Data.Entities
{
    public class Setting
    {
        [Key]
        public Guid SettingId { get; set; }
        [Required, MaxLength(50)]
        public string Name { get; set; }
        [Required, MaxLength(255)]
        public string Value { get; set; }

    }
}

