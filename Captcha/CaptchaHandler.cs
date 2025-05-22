using PuppeteerSharp;

namespace WebScrappingTrades.Captcha
{
    internal class CaptchaHandler
    {
        private readonly string _logPath;
        public CaptchaHandler(string logPath) => _logPath = logPath;

        /// <summary>
        /// Handles the CAPTCHA challenge presented on the specified page.
        /// </summary>
        /// <remarks>This method processes a CAPTCHA challenge using a nine-picture CAPTCHA handler. 
        /// Ensure that the <paramref name="page"/> parameter represents a valid and active page instance.</remarks>
        /// <param name="page">The page instance where the CAPTCHA challenge is displayed. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation of handling the CAPTCHA.</returns>
        internal async Task HandleCaptcha(IPage page)
        {
            CaptchaNinePictures captchaNinePictures = new(_logPath);
            await captchaNinePictures.HandleCaptcha(page);
        }

        /// <summary>
        /// Handles the "move captcha" challenge on the specified web page.
        /// </summary>
        /// <remarks>This method processes a "move captcha" challenge by delegating the task to an
        /// internal handler.  Ensure that the provided <paramref name="page"/> is properly initialized and represents a
        /// valid  web page containing the captcha challenge.</remarks>
        /// <param name="page">The web page where the captcha challenge is to be handled. Cannot be <see langword="null"/>.</param>
        /// <returns></returns>
        internal async Task HandleMoveCaptcha(IPage page)
        {
            CaptchaMovePicture captchaMovePicture = new(_logPath);
            await captchaMovePicture.HandleCaptcha(page);
        }

        /// <summary>
        /// Determines whether a CAPTCHA move element is present on the specified page.
        /// </summary>
        /// <remarks>This method checks for the presence of specific CAPTCHA-related elements on the page
        /// and logs the operation. It returns <see langword="false"/> if no such elements are found or if an exception
        /// occurs during the process.</remarks>
        /// <param name="page">The page to inspect for the presence of a CAPTCHA move element.</param>
        /// <param name="mainScrapping">An instance of <see cref="MainScrapping"/> used for logging operations during the check.</param>
        /// <returns><see langword="true"/> if a CAPTCHA move element is present and visible in the viewport; otherwise, <see
        /// langword="false"/>.</returns>
        internal async Task<bool> IsCaptchaMovePresent(IPage page, MainScrapping mainScrapping)
        {
            try
            {
                Console.WriteLine("IsCaptchaMovePresent");
                await mainScrapping.DoLogAsync(page, "captchaMove");
                var captchaElements = await page.QuerySelectorAllAsync("div[data-bn-type='text'].css-1mpu4lr");
                return captchaElements.Length > 0 && await captchaElements[0].IsIntersectingViewportAsync();
            }
            catch (PuppeteerException)
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether a CAPTCHA nine pictures is present on the specified web page.
        /// </summary>
        /// <remarks>This method checks for the presence of CAPTCHA elements on the page by querying for
        /// elements with the class <c>.bcap-verify-button</c>. It also verifies if the first CAPTCHA element is visible
        /// in the viewport.</remarks>
        /// <param name="page">The web page to check for the presence of a CAPTCHA.</param>
        /// <param name="mainScrapping">An instance of <see cref="MainScrapping"/> used for logging operations during the check.</param>
        /// <returns><see langword="true"/> if a CAPTCHA is detected on the page and is visible in the viewport; otherwise, <see
        /// langword="false"/>.</returns>
        internal async Task<bool> IsCaptchaPresent(IPage page, MainScrapping mainScrapping)
        {
            try
            {
                Console.WriteLine("IsCaptchaPresent");
                await mainScrapping.DoLogAsync(page, $"CaptchaLog");
                var captchaElements = await page.QuerySelectorAllAsync(".bcap-verify-button");
                return captchaElements.Length > 0 && await captchaElements[0].IsIntersectingViewportAsync();
            }
            catch (PuppeteerException)
            {
                return false;
            }
        }
    }
}
