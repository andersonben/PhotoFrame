using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;

namespace PhotoFrame.Web.Services
{
    public class ImageProcessingService
    {
        private const int TargetWidth = 1872;
        private const int TargetHeight = 1404;
        
        // 16-level grayscale palette for e-ink display
        private static readonly byte[] GrayscaleLevels = new byte[]
        {
            0, 17, 34, 51, 68, 85, 102, 119, 136, 153, 170, 187, 204, 221, 238, 255
        };

        public async Task<string> ProcessImageAsync(string inputPath, string outputPath)
        {
            using var image = await Image.LoadAsync<Rgba32>(inputPath);
            
            // Resize image to fit within target dimensions while maintaining aspect ratio
            var resizeOptions = new ResizeOptions
            {
                Size = new Size(TargetWidth, TargetHeight),
                Mode = ResizeMode.Max,
                Position = AnchorPositionMode.Center
            };
            
            image.Mutate(x => x.Resize(resizeOptions));
            
            // Create a new image with exact target dimensions and white background
            using var targetImage = new Image<Rgba32>(TargetWidth, TargetHeight, Color.White);
            
            // Calculate position to center the resized image
            var x = (TargetWidth - image.Width) / 2;
            var y = (TargetHeight - image.Height) / 2;
            
            // Draw the resized image onto the target canvas
            targetImage.Mutate(ctx => ctx.DrawImage(image, new Point(x, y), 1f));
            
            // Convert to grayscale and apply dithering
            targetImage.Mutate(x => x.Grayscale());
            
            // Apply Floyd-Steinberg dithering to 16 levels
            ApplyFloydSteinbergDithering(targetImage);
            
            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            
            await targetImage.SaveAsPngAsync(outputPath);
            
            return outputPath;
        }
        
        private void ApplyFloydSteinbergDithering(Image<Rgba32> image)
        {
            var width = image.Width;
            var height = image.Height;
            
            // Create error buffer
            var errors = new float[width, height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = image[x, y];
                    var oldGray = pixel.R; // Since it's grayscale, R=G=B
                    var grayWithError = Math.Max(0, Math.Min(255, oldGray + errors[x, y]));
                    
                    // Find closest grayscale level
                    var newGray = FindClosestGrayscaleLevel((byte)grayWithError);
                    var error = grayWithError - newGray;
                    
                    // Set the new pixel value
                    image[x, y] = new Rgba32(newGray, newGray, newGray, 255);
                    
                    // Distribute error using Floyd-Steinberg coefficients
                    if (x + 1 < width)
                        errors[x + 1, y] += error * 7f / 16f;
                    
                    if (y + 1 < height)
                    {
                        if (x > 0)
                            errors[x - 1, y + 1] += error * 3f / 16f;
                        
                        errors[x, y + 1] += error * 5f / 16f;
                        
                        if (x + 1 < width)
                            errors[x + 1, y + 1] += error * 1f / 16f;
                    }
                }
            }
        }
        
        private byte FindClosestGrayscaleLevel(byte value)
        {
            byte closest = GrayscaleLevels[0];
            int minDiff = Math.Abs(value - closest);
            
            for (int i = 1; i < GrayscaleLevels.Length; i++)
            {
                int diff = Math.Abs(value - GrayscaleLevels[i]);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = GrayscaleLevels[i];
                }
            }
            
            return closest;
        }
        
        public (int width, int height) GetImageDimensions(string imagePath)
        {
            using var image = Image.Identify(imagePath);
            return (image.Width, image.Height);
        }
    }
}