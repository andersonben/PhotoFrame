using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoFrame.Data;
using PhotoFrame.Data.Entities;
using PhotoFrame.Web.Services;
using PhotoFrame.Web.Models;

namespace PhotoFrame.Web.Controllers
{
    [Authorize]
    public class PhotosController : Controller
    {
        private readonly PhotoFrameDbContext _context;
        private readonly ImageProcessingService _imageProcessingService;
        private readonly IWebHostEnvironment _environment;

        public PhotosController(PhotoFrameDbContext context, ImageProcessingService imageProcessingService, IWebHostEnvironment environment)
        {
            _context = context;
            _imageProcessingService = imageProcessingService;
            _environment = environment;
        }

        // GET: Photos
        public async Task<IActionResult> Index()
        {
              return _context.Photos != null ? 
                          View(await _context.Photos.ToListAsync()) :
                          Problem("Entity set 'PhotoFrameDbContext.Photos'  is null.");
        }

        // GET: Photos/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.Photos == null)
            {
                return NotFound();
            }

            var photo = await _context.Photos
                .FirstOrDefaultAsync(m => m.PhotoId == id);
            if (photo == null)
            {
                return NotFound();
            }

            return View(photo);
        }

        // GET: Photos/Upload
        public IActionResult Upload()
        {
            return View();
        }

        // POST: Photos/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(PhotoUploadViewModel model)
        {
            if (ModelState.IsValid && model.PhotoFile != null)
            {
                try
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
                    var fileExtension = Path.GetExtension(model.PhotoFile.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("PhotoFile", "Please upload a valid image file (JPG, PNG, BMP, GIF).");
                        return View(model);
                    }

                    // Create directories if they don't exist
                    var originalDir = Path.Combine(_environment.WebRootPath, "photos", "original");
                    var processedDir = Path.Combine(_environment.WebRootPath, "photos", "processed");
                    Directory.CreateDirectory(originalDir);
                    Directory.CreateDirectory(processedDir);

                    // Generate unique filename
                    var fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var originalPath = Path.Combine(originalDir, fileName);
                    var processedPath = Path.Combine(processedDir, Path.ChangeExtension(fileName, ".png"));

                    // Save original file
                    using (var stream = new FileStream(originalPath, FileMode.Create))
                    {
                        await model.PhotoFile.CopyToAsync(stream);
                    }

                    // Get original image dimensions
                    var (width, height) = _imageProcessingService.GetImageDimensions(originalPath);

                    // Process image for e-ink display
                    await _imageProcessingService.ProcessImageAsync(originalPath, processedPath);

                    // Create photo record
                    var photo = new Photo
                    {
                        PhotoId = Guid.NewGuid(),
                        Name = string.IsNullOrEmpty(model.Name) ? Path.GetFileNameWithoutExtension(model.PhotoFile.FileName) : model.Name,
                        OriginalPath = $"/photos/original/{fileName}",
                        ProcessedPath = $"/photos/processed/{Path.ChangeExtension(fileName, ".png")}",
                        UploadedAt = DateTime.UtcNow,
                        FileSizeBytes = model.PhotoFile.Length,
                        OriginalWidth = width,
                        OriginalHeight = height,
                        IsActive = true
                    };

                    _context.Photos.Add(photo);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Photo uploaded and processed successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error processing photo: {ex.Message}");
                }
            }

            return View(model);
        }

        // GET: Photos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Photos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PhotoId,Name,OriginalPath,ProcessedPath")] Photo photo)
        {
            if (ModelState.IsValid)
            {
                photo.PhotoId = Guid.NewGuid();
                photo.UploadedAt = DateTime.UtcNow;
                photo.IsActive = true;
                _context.Add(photo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(photo);
        }

        // GET: Photos/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.Photos == null)
            {
                return NotFound();
            }

            var photo = await _context.Photos.FindAsync(id);
            if (photo == null)
            {
                return NotFound();
            }
            return View(photo);
        }

        // POST: Photos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("PhotoId,Name,Path")] Photo photo)
        {
            if (id != photo.PhotoId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(photo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhotoExists(photo.PhotoId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(photo);
        }

        // GET: Photos/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.Photos == null)
            {
                return NotFound();
            }

            var photo = await _context.Photos
                .FirstOrDefaultAsync(m => m.PhotoId == id);
            if (photo == null)
            {
                return NotFound();
            }

            return View(photo);
        }

        // POST: Photos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.Photos == null)
            {
                return Problem("Entity set 'PhotoFrameDbContext.Photos'  is null.");
            }
            var photo = await _context.Photos.FindAsync(id);
            if (photo != null)
            {
                _context.Photos.Remove(photo);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PhotoExists(Guid id)
        {
          return (_context.Photos?.Any(e => e.PhotoId == id)).GetValueOrDefault();
        }
    }
}
