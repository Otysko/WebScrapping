using Newtonsoft.Json;
using PuppeteerSharp;
using WebScrappingTrades.Captcha;
using WebScrappingTrades.Database;
using WebScrappingTrades.Models;

namespace WebScrappingTrades
{
    internal class MainScrapping
    {
        private readonly string baseUrl;
        private readonly string loginUrl;
        public string logPath = "Picture_";
        private readonly Startup startup;
        public string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainScrapping"/> class with the specified startup configuration
        /// and database connection string.
        /// </summary>
        /// <remarks>This constructor sets up the base URL and login URL for Binance operations and starts
        /// the main asynchronous task.</remarks>
        /// <param name="startup">The startup configuration used to initialize the application.</param>
        /// <param name="_connectionString">The connection string for the database.</param>
        public MainScrapping(Startup startup, string _connectionString)
        {
            baseUrl = "https://www.binance.com/en/futures-activity/leaderboard/user/um?encryptedUid=";
            loginUrl = "https://accounts.binance.com/en/login/";
            this.startup = startup;
            this._connectionString = _connectionString;
            _ = Task.Run(() => MainTaskAsync());
        }

        /// <summary>
        /// Executes the main asynchronous task for managing browser automation and processing queued messages.
        /// </summary>
        /// <remarks>This method performs the following operations: <list type="bullet"> <item>
        /// <description>Downloads and launches a supported browser (Chrome) with specified options.</description>
        /// </item> <item> <description>Logs into the application using the provided page instance.</description>
        /// </item> <item> <description>Processes messages from a queue in a loop, delegating each message to a new
        /// browser tab for processing.</description> </item> </list> If the login fails or an exception occurs, the
        /// application is flagged to restart.</remarks>
        /// <returns></returns>
        private async Task MainTaskAsync()
        {
            try
            {
                Console.WriteLine($"Starting downloading browser at {DateTime.Now:HH:mm:ss:ffff}");
                await new BrowserFetcher(SupportedBrowser.Chrome).DownloadAsync();
                Console.WriteLine($"Downloading completed at {DateTime.Now:HH:mm:ss:ffff}");
                var launchOptions = new LaunchOptions
                {
                    Headless = false, // = false for testing
                    DefaultViewport = new ViewPortOptions { Width = 1920, Height = 1080, IsMobile = false },
                    Browser = SupportedBrowser.Chrome,
                    //Args = ["--no-sandbox", "--disable-setuid-sandbox"]  // comment out for testing
                };
                using var browser = await Puppeteer.LaunchAsync(launchOptions);

                using var page = await browser.NewPageAsync();
                Console.WriteLine($"Starting Puppeteer at {DateTime.Now:HH:mm:ss:ffff}");
                bool loggedIn = await LogIn(page);

                if (loggedIn)
                {
                    while (startup.run)
                    {
                        var tasks = new List<Task>();
                        while (startup.mq._messageQueue.TryDequeue(out var queuedMessage))
                        {
                            var traderValue = JsonConvert.DeserializeObject<TraderValues>(queuedMessage);
                            tasks.Add(ProcessTraderInNewTab(traderValue, page));
                        }

                        if (tasks.Count != 0)
                        {
                            await Task.WhenAll(tasks);
                            startup.mq.SendData("Scrapper ready", "/testing/JobController/ActualJob/");
                            await Task.Delay(2000);
                        }
                        else
                        {
                            await Task.Delay(100);
                        }
                    }
                }
                else
                {
                    startup.shouldStartAgain = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception 0x01: {ex.Message}");
                startup.shouldStartAgain = true;
            }
        }

        /// <summary>
        /// Attempts to log in to the specified page using the provided user credentials.
        /// </summary>
        /// <remarks>This method navigates to the login page, enters the user's email and password, and handles additional
        /// challenges such as CAPTCHA or two-factor authentication (2FA) if required. If the system detects automation or fails
        /// to handle a CAPTCHA, the application may terminate.</remarks>
        /// <param name="page">The page instance representing the browser context where the login process will be performed.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the login process
        /// completes successfully; otherwise, <see langword="false"/> if an exception occurs or the login fails.</returns>
        private async Task<bool> LogIn(IPage page)
        {
            try
            {
                await page.GoToAsync(loginUrl);
                await DoLogAsync(page, $"Login0");
                await Task.Delay(2500);
                await page.TypeAsync(".bn-textField-input", startup.userCredentials.email);
                await page.ClickAsync(".bn-button__primary");
                await DoLogAsync(page, $"Login1");
                if (await RecognizedAutomation(page))
                {
                    Console.WriteLine("Bot recognized - closing app");
                    Environment.Exit(0);
                }
                else if (await new CaptchaHandler(logPath).IsCaptchaPresent(page, this))
                {
                    Console.WriteLine("CAPTCHA detected, handling CAPTCHA...");
                    await new CaptchaHandler(logPath).HandleCaptcha(page);
                }
                else if (await new CaptchaHandler(logPath).IsCaptchaMovePresent(page, this))
                {
                    Console.WriteLine("CAPTCHA move detected, handling CAPTCHA...");
                    await new CaptchaHandler(logPath).HandleMoveCaptcha(page);
                }
                await DoLogAsync(page, $"Login2");
                // Input password
                await page.WaitForSelectorAsync(".bn-textField-input");
                await DoLogAsync(page, $"LoginX");
                await Task.Delay(8000);
                await page.TypeAsync(".bn-textField-input", startup.userCredentials.password);
                await page.ClickAsync(".bn-button__primary");
                await DoLogAsync(page, $"Login3");
                // Handle 2FA if required
                string code = "";
                while (code == "")
                {
                    code = await GmailApi.GetCodeFromMail(startup.userCredentials);
                }
                await page.TypeAsync(".bn-textField-input", code);
                Console.WriteLine("Written code from email");
                await DoLogAsync(page, $"Login4");
                await page.WaitForSelectorAsync("button.bn-button.bn-button__primary.data-size-large.w-full.mb-\\[16px\\]");
                await Task.Delay(10000);
                await DoLogAsync(page, $"Login5");
                Console.WriteLine("Should be logged in");
                startup.mq.SendData("Scrapper ready", "/testing/JobController/ActualJob/");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception 0x02: {ex.Message}{Environment.NewLine}{ex}");
                return false;
            }
        }

        /// <summary>
        /// Determines whether an automation-related popup is displayed on the specified page.
        /// </summary>
        /// <remarks>This method introduces a delay of 10 seconds before checking for the popup. It
        /// searches for elements matching the selector <c>"div[data-bn-type='text'].css-1izbvz9"</c> and verifies
        /// whether the first matching element is visible in the viewport.</remarks>
        /// <param name="page">The page to check for the presence of an automation-related popup.</param>
        /// <returns><see langword="true"/> if an automation-related popup is detected and visible in the viewport; otherwise,
        /// <see langword="false"/>.</returns>
        private async Task<bool> RecognizedAutomation(IPage page)
        {
            try
            {
                await Task.Delay(10000);
                Console.WriteLine("Checking for automation popup");
                var captchaElements = await page.QuerySelectorAllAsync("div[data-bn-type='text'].css-1izbvz9");
                return captchaElements.Length > 0 && await captchaElements[0].IsIntersectingViewportAsync();
            }
            catch (PuppeteerException)
            {
                Console.WriteLine("Exc - Automation is not detected.");
                return false;
            }
        }

        /// <summary>
        /// Processes a trader's data in a new browser tab.
        /// </summary>
        /// <remarks>This method performs the following operations: <list type="bullet">
        /// <item><description>Navigates to the trader's page using the provided <paramref
        /// name="page"/>.</description></item> <item><description>Logs the processing activity for the
        /// trader.</description></item> <item><description>Extracts coin data and determines if pagination is
        /// present.</description></item> <item><description>Sends the extracted data to the appropriate message queue
        /// based on whether trades are open or closed.</description></item> <item><description>Updates the trader's
        /// information in the database if additional values are retrieved from the page.</description></item> </list>
        /// If an exception occurs during processing, it is logged to the console.</remarks>
        /// <param name="traderValues">The trader-specific values, including client name and client value. This parameter cannot be null.</param>
        /// <param name="page">The browser page instance used to navigate and extract data. This parameter cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task ProcessTraderInNewTab(TraderValues? traderValues, IPage page)
        {
            try
            {
                await page.GoToAsync($"{baseUrl}{traderValues.ClientValue}");
                await DoLogAsync(page, $"Processing trader {traderValues.ClientName}");
                await page.WaitForSelectorAsync(".t-headline5.text-PrimaryText", new WaitForSelectorOptions
                {
                    Timeout = 10000
                });
                List<CoinData> traderCoinData = [];
                await Task.Delay(1000);
                await GetCoinDatas(page, traderValues.ClientName, traderCoinData);
                await IsPaginationPresent(page, traderValues.ClientName, traderCoinData);
                await DoLogAsync(page, $"Processing trader2 {traderValues.ClientName}");
                if (traderCoinData.Count > 0)
                {
                    string message = JsonConvert.SerializeObject(traderCoinData);
                    startup.mq.SendData(message, "/testing/Announcer/OpenTrades/");
                }
                else
                {
                    CoinDataClosed coinDataClosed = new() { Trader = traderValues.ClientName };
                    string message = JsonConvert.SerializeObject(coinDataClosed);
                    startup.mq.SendData(message, "/testing/Announcer/ClosedTrades/");
                }
                var values = await GetValuesFromPage(page);
                if (values != null)
                {
                    DatabaseUpdateTradersInfo.UpdateValues(values, traderValues.ClientName, _connectionString);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in ProcessTraderInNewTab for {traderValues.ClientName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts and populates coin trading data for a specific trader from the provided web page.
        /// </summary>
        /// <remarks>This method queries the web page for specific HTML elements that contain coin trading
        /// information, such as coin name, leverage, size, entry price, mark price, time, profit and loss (PNL), return
        /// on investment (ROI), and trade direction (buy or sell). The extracted data is encapsulated in <see
        /// cref="CoinData"/> objects and added to the provided list.</remarks>
        /// <param name="page">The web page instance representing the trader's data. This is used to query and extract coin-related
        /// information.</param>
        /// <param name="trader">The name of the trader whose coin data is being retrieved. This value is assigned to each <see
        /// cref="CoinData"/> object.</param>
        /// <param name="traderCoinData">A list to which the extracted <see cref="CoinData"/> objects will be added. Each object represents a coin's
        /// trading details.</param>
        /// <returns>A task that represents the asynchronous operation of extracting and populating coin trading data.</returns>
        private static async Task GetCoinDatas(IPage page, string trader, List<CoinData> traderCoinData)
        {
            var rows = await page.QuerySelectorAllAsync("tr[data-row-key]");
            foreach (var row in rows)
            {
                var coinData = new CoinData { Trader = trader };
                var coinNameElement = await row.QuerySelectorAsync("div.name.t-subtitle1");
                if (coinNameElement != null)
                {
                    coinData.CoinName = await coinNameElement.EvaluateFunctionAsync<string>("el => el.textContent");
                }
                var leverageElement = await row.QuerySelectorAsync("div.tag-list div.tag:nth-child(2)");
                if (leverageElement != null)
                {
                    coinData.Leverage = await leverageElement.EvaluateFunctionAsync<string>("el => el.textContent");
                }
                var sizeElement = await row.QuerySelectorAsync("td:nth-child(2)");
                if (sizeElement != null)
                {
                    coinData.Size = await sizeElement.EvaluateFunctionAsync<string>("el => el.textContent");
                }
                var entryPriceElement = await row.QuerySelectorAsync("td:nth-child(3)");
                if (entryPriceElement != null)
                {
                    coinData.EntryPrice = await entryPriceElement.EvaluateFunctionAsync<string>("el => el.textContent");
                }
                var markPriceElement = await row.QuerySelectorAsync("td:nth-child(4)");
                if (markPriceElement != null)
                {
                    coinData.MarkPrice = await markPriceElement.EvaluateFunctionAsync<string>("el => el.textContent");
                }
                var timeElement = await row.QuerySelectorAsync("td:nth-child(5)");
                if (timeElement != null)
                {
                    coinData.Time = await timeElement.EvaluateFunctionAsync<string>("el => el.textContent");
                }
                var pnlElement = await row.QuerySelectorAsync("div.flex span:nth-child(1)");
                if (pnlElement != null)
                {
                    coinData.PNL = await pnlElement.EvaluateFunctionAsync<string>("el => el.textContent");
                }
                var roiElement = await row.QuerySelectorAsync("div.flex span:nth-child(2)");
                if (roiElement != null)
                {
                    coinData.ROI = await roiElement.EvaluateFunctionAsync<string>("el => el.textContent");
                }
                var buySellElement = await row.QuerySelectorAsync("div.dir-name > div");
                if (buySellElement != null)
                {
                    string classAttribute = await buySellElement.EvaluateFunctionAsync<string>("el => el.getAttribute('class')");
                    if (classAttribute.Contains("bg-Buy"))
                    {
                        coinData.Direction = 0;
                    }
                    else if (classAttribute.Contains("bg-Sell"))
                    {
                        coinData.Direction = 1;
                    }
                }
                traderCoinData.Add(coinData);
            }
        }

        /// <summary>
        /// Recursively checks for the presence of a pagination button on the page and processes additional pages if
        /// available.
        /// </summary>
        /// <remarks>This method identifies the "Next" pagination button on the page and determines if it
        /// is enabled. If the button is enabled, it clicks the button to navigate to the next page, processes the data
        /// on that page, and continues the pagination check recursively.</remarks>
        /// <param name="page">The current page being analyzed for pagination.</param>
        /// <param name="trader">The identifier for the trader whose data is being processed.</param>
        /// <param name="traderCoinData">A list to store coin data associated with the trader. This list is updated as additional pages are
        /// processed.</param>
        /// <returns></returns>
        private static async Task IsPaginationPresent(IPage page, string trader, List<CoinData> traderCoinData)
        {
            try
            {
                var nextButton = await page.QuerySelectorAsync("div.bn-pagination-next");
                if (nextButton != null)
                {
                    var isDisabled = await page.EvaluateFunctionAsync<string>("button => button.getAttribute('aria-disabled')", nextButton);
                    if (isDisabled == "false")
                    {
                        await HandleCookieConsent(page);
                        await nextButton.ClickAsync();
                        await GetCoinDatas(page, trader, traderCoinData);
                        await IsPaginationPresent(page, trader, traderCoinData);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in pagination check: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the cookie consent dialog on a web page by attempting to locate and interact with the consent
        /// elements.
        /// </summary>
        /// <remarks>This method searches for a cookie consent container and attempts to click the reject
        /// button if it is found. It introduces a delay after clicking the button to ensure the action is processed. If
        /// the consent container or reject button is not found, the method completes without performing any action. Any
        /// exceptions encountered during the process are logged to the console.</remarks>
        /// <param name="page">The <see cref="IPage"/> instance representing the web page where the cookie consent dialog is to be handled.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        private static async Task HandleCookieConsent(IPage page)
        {
            try
            {
                var consentContainer = await page.QuerySelectorAsync("#onetrust-group-container");
                if (consentContainer != null)
                {
                    var rejectButton = await page.QuerySelectorAsync("#onetrust-accept-btn-handler");
                    if (rejectButton != null)
                    {
                        await rejectButton.ClickAsync();
                        await Task.Delay(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling cookie consent: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts text content from all elements matching a specific CSS selector on the provided page.
        /// </summary>
        /// <remarks>This method queries the page for elements matching the CSS selector
        /// <c>"div.t-headline5 > span"</c>.  It retrieves the text content of each matched element and returns them as
        /// an array of strings.  If no elements are found, or if an exception occurs during the operation, the method
        /// logs the issue  and returns an array containing <c>"--"</c>.</remarks>
        /// <param name="page">The page instance to query for elements. Must not be <see langword="null"/>.</param>
        /// <returns>An array of strings containing the text content of the matched elements.  If no elements are found, or if an
        /// error occurs, the array will contain a single element with the value <c>"--"</c>.</returns>
        private static async Task<string[]> GetValuesFromPage(IPage page)
        {
            try
            {
                var roiElements = await page.QuerySelectorAllAsync("div.t-headline5 > span");
                if (roiElements.Length == 0)
                {
                    Console.WriteLine("Couldn't find ROI elements.");
                    return ["--"];
                }
                var roiValues = new List<string>();
                foreach (var element in roiElements)
                {
                    var text = await page.EvaluateFunctionAsync<string>("el => el.textContent", element);
                    roiValues.Add(text);
                }
                return [.. roiValues];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while getting values from page: {ex.Message}");
                return ["--"];
            }
        }

        /// <summary>
        /// Captures a screenshot of the specified page and saves its HTML content to a log file.
        /// </summary>
        /// <remarks>This method ensures that a "logs" directory exists in the current working directory. 
        /// The screenshot and HTML content of the page are saved in the "logs" directory with a file name  based on the
        /// provided <paramref name="v"/> parameter.</remarks>
        /// <param name="page">The page from which the screenshot and HTML content will be captured.</param>
        /// <param name="v">A string used to uniquely identify the log file name.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task DoLogAsync(IPage page, string v)
        {
            if (!Directory.Exists("logs"))
            {
                Directory.CreateDirectory("logs");
            }
            await page.ScreenshotAsync(Path.Combine("logs", $"{logPath}{v}.html"));
            string pageSource = await page.GetContentAsync();
            string logFilePath = Path.Combine("logs", $"{logPath}{v}.html");
            await File.WriteAllTextAsync(logFilePath, pageSource);
        }
    }
}
