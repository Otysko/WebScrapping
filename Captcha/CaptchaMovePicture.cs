using PuppeteerSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace WebScrappingTrades.Captcha
{
    internal class CaptchaMovePicture
    {
        private readonly string puzzleImg;
        private readonly string backgroundImg;
        private readonly string fullImg;
        private int _topBoundary;

        /// <summary>
        /// Initializes a new instance of the <see cref="CaptchaMovePicture"/> class with the specified log path.
        /// </summary>
        /// <remarks>The constructor initializes the file paths for the captcha images based on the
        /// provided log path. Ensure that the <paramref name="logPath"/> ends with a directory separator if required by
        /// the file system.</remarks>
        /// <param name="logPath">The directory path where the captcha-related image files will be stored.  This path is used to construct
        /// file paths for the puzzle piece, background image, and full image.</param>
        public CaptchaMovePicture(string logPath)
        {
            puzzleImg = $"{logPath}puzzlePiece.png";
            backgroundImg = $"{logPath}backgroundImage.png";
            fullImg = $"{logPath}fullImage.png";
            _topBoundary = 0;
        }

        /// <summary>
        /// Handles the CAPTCHA-solving process for the specified web page.
        /// </summary>
        /// <remarks>This method orchestrates the steps required to solve a CAPTCHA, including processing
        /// images,  calculating the distance to move, and performing the necessary actions to complete the CAPTCHA. It
        /// includes a delay to account for timing requirements during the CAPTCHA-solving process.</remarks>
        /// <param name="page">The web page where the CAPTCHA is located. This parameter cannot be null.</param>
        /// <returns></returns>
        internal async Task HandleCaptcha(IPage page)
        {
            try
            {
                Console.WriteLine($"Implement CAPTCHA moving logic here.");
                await ProcessImages(page);
                int distanceToMove = CalculateDistance();
                Console.WriteLine($"Distance to move main function: {distanceToMove}");
                await MoveToSolveCaptchaAsync(page, distanceToMove);
                await Task.Delay(60000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"exc move captcha: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Processes an image element on the specified web page by extracting its background image URL, saving the
        /// image, and splitting it into smaller parts.
        /// </summary>
        /// <remarks>This method locates an image element with the CSS class "css-v2gpbh" on the provided
        /// page, extracts its background image URL, saves the image to a specified location, and then splits the saved
        /// image into smaller parts. Ensure that the page is fully loaded before calling this method.</remarks>
        /// <param name="page">The web page containing the image element to process. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task ProcessImages(IPage page)
        {
            var imageElement = await page.QuerySelectorAsync(".css-v2gpbh");
            var imageUrl = await imageElement.EvaluateFunctionAsync<string>("el => window.getComputedStyle(el).backgroundImage");
            imageUrl = imageUrl.Replace("url(\"", "").Replace("\")", "");
            await SaveImageFromUrl(imageUrl, fullImg);
            SplitImage();
        }

        /// <summary>
        /// Downloads an image from the specified URL and saves it to the specified file path.
        /// </summary>
        /// <remarks>This method performs an HTTP GET request to retrieve the image data from the
        /// specified URL and writes the data to the specified file path. Ensure that the <paramref name="filePath"/> is
        /// writable and that the application has the necessary permissions to access the file system.</remarks>
        /// <param name="imageUrl">The URL of the image to download. Must be a valid, accessible URL.</param>
        /// <param name="filePath">The full file path, including the file name, where the image will be saved. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private static async Task SaveImageFromUrl(string imageUrl, string filePath)
        {
            using HttpClient client = new();
            var response = await client.GetAsync(imageUrl);
            if (response.IsSuccessStatusCode)
            {
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(filePath, imageBytes);
            }
        }

        /// <summary>
        /// Splits the source image into separate components: a puzzle piece and a background image.
        /// </summary>
        /// <remarks>This method processes the source image by cropping specific regions to create two
        /// separate images: a puzzle piece and a background image. The cropped images are saved to predefined file
        /// paths.</remarks>
        public void SplitImage()
        {
            using Image<Rgba32> fullImage = Image.Load<Rgba32>(fullImg);
            Rectangle puzzlePieceRect = new(0, 0, 60, fullImage.Height);
            var puzzlePiece = fullImage.Clone();
            puzzlePiece.Mutate(x => x.Crop(puzzlePieceRect));
            puzzlePiece.Save(puzzleImg);
            _topBoundary = GetTopBoundary();
            Rectangle puzzlePieceRect2 = new(0, _topBoundary, puzzlePiece.Width, 70);
            var puzzlePiece2 = puzzlePiece.Clone();
            puzzlePiece2.Mutate(x => x.Crop(puzzlePieceRect2));
            puzzlePiece2.Save(puzzleImg);
            Rectangle backgroundRect = new(60, 0, fullImage.Width - 60, fullImage.Height);
            var backgroundImage = fullImage.Clone();
            backgroundImage.Mutate(x => x.Crop(backgroundRect));
            backgroundImage.Save(backgroundImg);
            Console.WriteLine("Puzzle piece and background images have been saved.");
        }


        /// <summary>
        /// Determines the vertical position of the topmost non-transparent pixel in the image.
        /// </summary>
        /// <remarks>This method processes the image row by row, starting from the top, to locate the
        /// first non-transparent pixel. It assumes the image is loaded from the <c>puzzleImg</c> field.</remarks>
        /// <returns>The zero-based y-coordinate of the first row containing a non-transparent pixel. If the image is fully
        /// transparent, returns 0.</returns>
        private int GetTopBoundary()
        {
            using Image<Rgba32> fullImage = Image.Load<Rgba32>(puzzleImg);
            bool foundNonTransparentPixel = false;
            int topBoundary = 0;
            for (int y = 0; y < fullImage.Height; y++)
            {
                for (int x = 0; x < fullImage.Width; x++)
                {
                    Rgba32 pixel = fullImage[x, y];
                    if (pixel.A > 0)
                    {
                        topBoundary = y;
                        foundNonTransparentPixel = true;
                        break;
                    }
                }
                if (foundNonTransparentPixel)
                {
                    break;
                }
            }
            return topBoundary;
        }

        /// <summary>
        /// Calculates the distance to move a puzzle piece to match a specific area within a background image.
        /// </summary>
        /// <remarks>This method processes the background image and the puzzle piece image to determine
        /// the required movement distance. The calculation is performed within a defined search area of the background
        /// image.</remarks>
        /// <returns>The distance, in pixels, that the puzzle piece needs to be moved to align with the target area.</returns>
        private int CalculateDistance()
        {
            using Image<Rgba32> fullImage = Image.Load<Rgba32>(backgroundImg);
            using Image<Rgba32> puzzlePiece = Image.Load<Rgba32>(puzzleImg);
            var searchArea = new Rectangle(0, _topBoundary, fullImage.Width, 70);
            int distanceToMove = TemplateMatch(fullImage, puzzlePiece, searchArea);
            Console.WriteLine($"Distance to move: {distanceToMove}px");
            return distanceToMove;
        }

        /// <summary>
        /// Finds the horizontal position within a specified search area of an image where a given puzzle piece best
        /// matches.
        /// </summary>
        /// <remarks>The method evaluates the similarity between the puzzle piece and regions of the
        /// cropped search area, sliding the puzzle piece horizontally. The position with the highest similarity score
        /// is returned.</remarks>
        /// <param name="fullImage">The full image in which the search is performed. This image will be cropped to the specified <paramref
        /// name="searchArea"/>.</param>
        /// <param name="puzzlePiece">The puzzle piece image to match against the cropped region of the full image. Its dimensions must be smaller
        /// than or equal to the dimensions of the <paramref name="searchArea"/>.</param>
        /// <param name="searchArea">The rectangular area within the full image where the search for the best match will be conducted.</param>
        /// <returns>The horizontal position (X-coordinate) within the <paramref name="searchArea"/> where the puzzle piece best
        /// matches.</returns>
        private static int TemplateMatch(Image<Rgba32> fullImage, Image<Rgba32> puzzlePiece, Rectangle searchArea)
        {
            fullImage.Mutate(x => x.Crop(searchArea));
            int bestX = 0;
            double bestScore = double.MinValue;
            for (int x = 0; x <= searchArea.Width - puzzlePiece.Width; x++)
            {
                var region = fullImage.Clone(ctx => ctx.Crop(new Rectangle(x, 0, puzzlePiece.Width, puzzlePiece.Height)));
                double score = CalculateSimilarity(region, puzzlePiece);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestX = x;
                }
            }
            return bestX;
        }

        /// <summary>
        /// Calculates the similarity score between two images based on their pixel values.
        /// </summary>
        /// <remarks>The similarity score is calculated by comparing the red channel of each pixel in the
        /// two images. The method assumes that both images have the same dimensions. Ensure that the input images are
        /// preprocessed to meet this requirement before calling the method.</remarks>
        /// <param name="img1">The first image to compare. Must have the same dimensions as <paramref name="img2"/>.</param>
        /// <param name="img2">The second image to compare. Must have the same dimensions as <paramref name="img1"/>.</param>
        /// <returns>A double value representing the similarity score between the two images.  Higher values indicate greater
        /// similarity.</returns>
        private static double CalculateSimilarity(Image<Rgba32> img1, Image<Rgba32> img2)
        {
            double similarity = 0.0;
            for (int y = 0; y < img1.Height; y++)
            {
                for (int x = 0; x < img1.Width; x++)
                {
                    var pixel1 = img1[x, y];
                    var pixel2 = img2[x, y];
                    similarity += 1.0 - Math.Abs(pixel1.R - pixel2.R) / 255.0; 
                }
            }
            return similarity;
        }

        /// <summary>
        /// Simulates a mouse drag operation to solve a CAPTCHA by moving a slider element.
        /// </summary>
        /// <remarks>This method locates the CAPTCHA slider element on the page, calculates the starting
        /// position for the mouse, and performs a drag-and-drop operation to move the slider by the specified distance.
        /// The method assumes the slider element is identified by the CSS selector ".css-p72bjc".</remarks>
        /// <param name="page">The <see cref="IPage"/> instance representing the browser page where the CAPTCHA is located.</param>
        /// <param name="distanceToMove">The distance, in pixels, to move the slider horizontally.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private static async Task MoveToSolveCaptchaAsync(IPage page, int distanceToMove)
        {
            var slider = await page.QuerySelectorAsync(".css-p72bjc");
            var boundingBox = await slider.BoundingBoxAsync();
            if (boundingBox != null)
            {
                var startX = boundingBox.X + 8;
                var startY = boundingBox.Y + boundingBox.Height / 2;
                await page.Mouse.MoveAsync(startX, startY);
                await page.Mouse.DownAsync();
                await page.Mouse.MoveAsync(startX + distanceToMove, startY, new PuppeteerSharp.Input.MoveOptions { Steps = 20 });
                await page.Mouse.UpAsync();
            }
        }
    }
}
