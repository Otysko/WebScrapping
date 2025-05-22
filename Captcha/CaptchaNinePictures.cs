using Amazon.Rekognition.Model;
using Amazon.Rekognition;
using PuppeteerSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace WebScrappingTrades.Captcha
{
    internal class CaptchaNinePictures
    {
        private string _logPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="CaptchaNinePictures"/> class with the specified log file path.
        /// </summary>
        /// <param name="logPath">The file path where logs will be written. Cannot be null or empty.</param>
        public CaptchaNinePictures(string logPath) => _logPath = logPath;

        /// <summary>
        /// Handles CAPTCHA challenges by identifying and interacting with specific elements on the page.
        /// </summary>
        /// <remarks>This method waits for the CAPTCHA-related elements to load, processes the images
        /// associated with the CAPTCHA, determines the indexes of the images to interact with based on the CAPTCHA's
        /// instructions, and performs the necessary clicks to solve the CAPTCHA. Ensure that the <paramref
        /// name="page"/> parameter is not null and represents a valid, active page context.</remarks>
        /// <param name="page">The <see cref="IPage"/> instance representing the web page where the CAPTCHA is displayed.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation of handling the CAPTCHA.</returns>
        internal async Task HandleCaptcha(IPage page)
        {
            Console.WriteLine("Implement CAPTCHA handling logic here.");
            var tagLabelElement = await page.WaitForSelectorAsync("#tagLabel");
            string tagLabelText = await tagLabelElement.EvaluateFunctionAsync<string>("el => el.textContent");
            Console.WriteLine($"Tag Label: {tagLabelText}");
            await ProcessImages(page);
            List<int> indexes = await GetIndexes(tagLabelText);
            Console.WriteLine($"found pictures -> {indexes.Count}");
            foreach (var index in indexes)
            {
                Console.WriteLine($"index: {index}");
            }
            await ClickSpecificImages(page, indexes);
        }

        /// <summary>
        /// Processes images from the specified web page by extracting the background image URL of the first image
        /// element, saving the image to a file, and performing cropping operations.
        /// </summary>
        /// <remarks>This method performs the following steps: <list type="number"> <item>Queries the page
        /// for elements with the class <c>.bcap-image-cell-image</c>.</item> <item>Extracts the background image URL of
        /// the first matching element.</item> <item>Saves the image to a predefined file path.</item> <item>Crops the
        /// saved image and saves the result.</item> </list> Ensure that the provided <paramref name="page"/> is fully
        /// loaded and contains the expected elements before calling this method.</remarks>
        /// <param name="page">The web page from which images will be processed. This parameter cannot be <see langword="null"/>.</param>
        /// <returns></returns>
        private async Task ProcessImages(IPage page)
        {
            var imageElements = await page.QuerySelectorAllAsync(".bcap-image-cell-image");
            var imageUrl = await imageElements[0].EvaluateFunctionAsync<string>("el => window.getComputedStyle(el).backgroundImage");
            imageUrl = imageUrl.Replace("url(\"", "").Replace("\")", "");
            string imagePath = $"{_logPath}image_all.png";
            await SaveImageFromUrl(imageUrl, imagePath);
            CropAndSaveImage(imagePath);
        }

        /// <summary>
        /// Downloads an image from the specified URL and saves it to the specified file path.
        /// </summary>
        /// <remarks>This method performs an asynchronous HTTP GET request to retrieve the image data from
        /// the provided URL. If the request is successful, the image is saved to the specified file path.  Ensure that
        /// the file path is writable and that the application has the necessary permissions to write to the
        /// location.</remarks>
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
        /// Crops the specified image into a 3x3 grid of smaller images and saves each cropped image to disk.
        /// </summary>
        /// <remarks>Each cropped image is saved in the directory specified by the internal log path, with
        /// filenames in the format "cropped_<i>_<j>.png", where <i> and <j> represent the row and column indices of the
        /// cropped section.</remarks>
        /// <param name="imagePath">The file path of the image to be cropped. The image must be in a format supported by ImageSharp.</param>
        private void CropAndSaveImage(string imagePath)
        {
            using Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(imagePath);
            int cropWidth = image.Width / 3;
            int cropHeight = image.Height / 3;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    string croppedPath = $"{_logPath}cropped_{i}_{j}.png";
                    var croppedImage = image.Clone();
                    croppedImage.Mutate(x => x.Crop(new Rectangle(j * cropWidth, i * cropHeight, cropWidth, cropHeight)));
                    croppedImage.Save(croppedPath);
                    Console.WriteLine($"Saved cropped image: {croppedPath}");
                }
            }
        }

        /// <summary>
        /// Asynchronously retrieves a list of indexes corresponding to image paths that match the specified keyword.
        /// </summary>
        /// <remarks>This method uses an external AWS service to determine the indexes of matching image
        /// paths. The returned list may be empty if no matches are found.</remarks>
        /// <param name="keyword">The keyword used to filter and identify relevant image paths.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of integers,  where each
        /// integer is the index of an image path that matches the specified keyword.</returns>
        private static async Task<List<int>> GetIndexes(string keyword)
        {
            string[] imagePaths = [
            "Picture_cropped_0_0.png",
            "Picture_cropped_0_1.png",
            "Picture_cropped_0_2.png",
            "Picture_cropped_1_0.png",
            "Picture_cropped_1_1.png",
            "Picture_cropped_1_2.png",
            "Picture_cropped_2_0.png",
            "Picture_cropped_2_1.png",
            "Picture_cropped_2_2.png"
            ];
            List<int> indexes = await UseAwsForIndexes(imagePaths, keyword);
            return indexes;
        }

        /// <summary>
        /// Analyzes a collection of images using AWS Rekognition to identify those containing a specified keyword.
        /// </summary>
        /// <remarks>This method uses the AWS Rekognition service to detect labels in the provided images.
        /// Labels with a confidence level of at least 75% are considered. If the keyword is "ship", related terms  such
        /// as "boat" and "yacht" are also matched.</remarks>
        /// <param name="imagePaths">An array of file paths to the images to be analyzed. Each path must point to a valid image file.</param>
        /// <param name="keyword">The keyword to search for in the detected labels. Matching is case-insensitive and may include related terms
        /// (e.g., "ship" may match "boat" or "yacht") depending on the keyword.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of zero-based indexes 
        /// corresponding to the images in <paramref name="imagePaths"/> that match the specified <paramref
        /// name="keyword"/>.</returns>
        private static async Task<List<int>> UseAwsForIndexes(string[] imagePaths, string keyword)
        {
            Console.WriteLine("Aws Api start");
            List<int> goodResult = [];
            int i = 0;
            var rekognitionClient = new AmazonRekognitionClient("awsAccessKeyId", "awsSecretAccessKey", Amazon.RegionEndpoint.EUCentral1);
            foreach (var imagePath in imagePaths)
            {
                Console.WriteLine($"{imagePath} ____________________________");
                var imageBytes = File.ReadAllBytes(imagePath);
                var request = new DetectLabelsRequest
                {
                    Image = new Amazon.Rekognition.Model.Image { Bytes = new MemoryStream(imageBytes) },
                    MaxLabels = 10,
                    MinConfidence = 75F
                };
                var response = await rekognitionClient.DetectLabelsAsync(request);
                foreach (var label in response.Labels)
                {
                    if (keyword == "ship"
                        && (label.Name.Contains("boat", StringComparison.CurrentCultureIgnoreCase) || label.Name.Contains("yacht", StringComparison.CurrentCultureIgnoreCase))
                        && !goodResult.Contains(i))
                    {
                        goodResult.Add(i);
                    }
                    if (label.Name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) && !goodResult.Contains(i))
                    {
                        goodResult.Add(i);
                    }
                    Console.WriteLine($"{label.Name}: {label.Confidence}%");
                }
                i++;
            }
            return goodResult;
        }

        /// <summary>
        /// Clicks on specific images within a CAPTCHA interface based on the provided indexes and submits the
        /// selection.
        /// </summary>
        /// <remarks>This method interacts with elements matching the CSS selector
        /// <c>".bcap-image-cell-image"</c>  and clicks on the images at the specified indexes. After clicking the
        /// images, it attempts to  click the verify button identified by the CSS selector <c>".bcap-verify-button"</c>,
        /// if present.  The method includes delays between interactions to mimic human behavior and ensure proper 
        /// execution of the CAPTCHA process.</remarks>
        /// <param name="page">The <see cref="IPage"/> instance representing the web page containing the CAPTCHA.</param>
        /// <param name="indexes">A list of zero-based indexes specifying which images to click.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private static async Task ClickSpecificImages(IPage page, List<int> indexes)
        {
            var imageElements = await page.QuerySelectorAllAsync(".bcap-image-cell-image");
            foreach (int index in indexes)
            {
                if (index < imageElements.Length)
                {
                    await imageElements[index].ClickAsync();
                    Console.WriteLine($"Clicked on image with index: {index}");
                    await Task.Delay(100);
                }
            }
            var verifyButton = await page.QuerySelectorAsync(".bcap-verify-button");
            if (verifyButton != null)
            {
                await verifyButton.ClickAsync();
                Console.WriteLine("Verify button clicked.");
            }
            await Task.Delay(2000);
            Console.WriteLine("Completed CAPTCHA image selection.");
        }
    }
}