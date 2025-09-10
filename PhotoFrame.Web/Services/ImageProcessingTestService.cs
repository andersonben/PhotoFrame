using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PhotoFrame.Web.Services
{
    public class ImageProcessingTestService
    {
        private readonly ImageProcessingService _imageProcessingService;

        public ImageProcessingTestService(ImageProcessingService imageProcessingService)
        {
            _imageProcessingService = imageProcessingService;
        }

        /// <summary>
        /// Creates a test pattern image to verify the display and processing pipeline
        /// </summary>
        public async Task<string> CreateTestPatternAsync(string outputPath)
        {
            const int width = 1872;
            const int height = 1404;
            
            using var image = new Image<Rgba32>(width, height);
            
            image.Mutate(ctx =>
            {
                // Fill with white background
                ctx.Fill(Color.White);
                
                // Create gradient test pattern
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Create different test patterns in different regions
                        if (y < height / 4)
                        {
                            // Top quarter: horizontal gradient
                            var gray = (byte)(255 * x / width);
                            image[x, y] = new Rgba32(gray, gray, gray, 255);
                        }
                        else if (y < height / 2)
                        {
                            // Second quarter: vertical gradient
                            var gray = (byte)(255 * (y - height / 4) / (height / 4));
                            image[x, y] = new Rgba32(gray, gray, gray, 255);
                        }
                        else if (y < 3 * height / 4)
                        {
                            // Third quarter: checkerboard pattern
                            var checkSize = 50;
                            var isBlack = ((x / checkSize) + (y / checkSize)) % 2 == 0;
                            var gray = isBlack ? (byte)0 : (byte)255;
                            image[x, y] = new Rgba32(gray, gray, gray, 255);
                        }
                        else
                        {
                            // Bottom quarter: 16-level grayscale bars
                            var barWidth = width / 16;
                            var level = Math.Min(15, x / barWidth);
                            var gray = (byte)(level * 255 / 15);
                            image[x, y] = new Rgba32(gray, gray, gray, 255);
                        }
                    }
                }
                
                // Add simple text labels (simplified without font dependency)
                // Note: In production, you might want to add SixLabors.Fonts package for better text rendering
            });
            
            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            
            await image.SaveAsPngAsync(outputPath);
            return outputPath;
        }

        /// <summary>
        /// Analyzes an image to verify it has been properly processed for e-ink display
        /// </summary>
        public async Task<ImageAnalysisResult> AnalyzeProcessedImageAsync(string imagePath)
        {
            using var image = await Image.LoadAsync<Rgba32>(imagePath);
            
            var result = new ImageAnalysisResult
            {
                Width = image.Width,
                Height = image.Height,
                IsCorrectSize = image.Width == 1872 && image.Height == 1404
            };
            
            var colorCounts = new Dictionary<byte, int>();
            var totalPixels = image.Width * image.Height;
            
            // Analyze pixel colors
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    
                    // Check if it's grayscale (R=G=B)
                    if (pixel.R != pixel.G || pixel.G != pixel.B)
                    {
                        result.IsGrayscale = false;
                    }
                    
                    // Count color occurrences
                    var gray = pixel.R;
                    colorCounts[gray] = colorCounts.GetValueOrDefault(gray, 0) + 1;
                }
            }
            
            result.UniqueColors = colorCounts.Keys.ToList();
            result.ColorCount = colorCounts.Count;
            result.Is16Level = result.ColorCount <= 16;
            
            // Check if colors match expected 16-level palette
            var expectedLevels = new byte[] { 0, 17, 34, 51, 68, 85, 102, 119, 136, 153, 170, 187, 204, 221, 238, 255 };
            result.MatchesExpectedPalette = result.UniqueColors.All(c => expectedLevels.Contains(c));
            
            return result;
        }
    }

    public class ImageAnalysisResult
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsCorrectSize { get; set; }
        public bool IsGrayscale { get; set; } = true;
        public int ColorCount { get; set; }
        public bool Is16Level { get; set; }
        public bool MatchesExpectedPalette { get; set; }
        public List<byte> UniqueColors { get; set; } = new();
        
        public bool IsValidForEInk => IsCorrectSize && IsGrayscale && Is16Level && MatchesExpectedPalette;
    }
}