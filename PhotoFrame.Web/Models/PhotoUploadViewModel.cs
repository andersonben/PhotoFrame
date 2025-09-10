using System.ComponentModel.DataAnnotations;

namespace PhotoFrame.Web.Models
{
    public class PhotoUploadViewModel
    {
        [Required(ErrorMessage = "Please select a photo to upload.")]
        [Display(Name = "Photo File")]
        public IFormFile PhotoFile { get; set; } = null!;

        [Display(Name = "Photo Name")]
        [StringLength(100, ErrorMessage = "Photo name cannot exceed 100 characters.")]
        public string? Name { get; set; }
    }
}