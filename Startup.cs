using System.Timers;
using WebScrappingTrades.Database;
using WebScrappingTrades.Models;


namespace WebScrappingTrades
{
    public class Startup
    {
        private readonly string _connectionString = "your_connection_string";
        public Mq mq;
        public bool run;
        public UserCredentials? userCredentials;
        public bool shouldStartAgain;

        /// <summary>
        /// Starts the main process by initializing necessary components and setting up the environment.
        /// </summary>
        /// <remarks>This method initializes user credentials, starts the message queue, and sets a timer
        /// for periodic operations.  It ensures the system is ready to perform its intended tasks. Call this method to
        /// begin the process.</remarks>
        public void Start()
        {
            run = true;
            userCredentials = new();
            shouldStartAgain = false;
            StartMq();
            SetTimer();
        }

        /// <summary>
        /// Initializes and starts a timer that triggers the <see cref="RunWatcher"/> method at regular intervals.
        /// </summary>
        /// <remarks>The timer is configured to invoke the <see cref="RunWatcher"/> method every 5000
        /// milliseconds (5 seconds).</remarks>
        private void SetTimer()
        {
            var runningWatcher = new System.Timers.Timer(5000);
            runningWatcher.Elapsed += new ElapsedEventHandler(RunWatcher);
            runningWatcher.Start();
        }

        /// <summary>
        /// Handles the elapsed event of a timer to monitor and restart the browser process if necessary.
        /// </summary>
        /// <remarks>This method checks if the browser process should be restarted and initiates the
        /// restart process asynchronously if required. It is intended to be used as a callback for a timer's elapsed
        /// event.</remarks>
        /// <param name="sender">The source of the event, typically the timer that triggered the event.</param>
        /// <param name="e">The event data containing information about the elapsed timer interval.</param>
        private void RunWatcher(object? sender, ElapsedEventArgs e)
        {
            if (shouldStartAgain)
            {
                Console.WriteLine("Starting browser again");
                shouldStartAgain = false;
                StartScrapperAsync();
            }
        }

        /// <summary>
        /// Initializes and starts the message queue for processing messages.
        /// </summary>
        /// <remarks>This method sets up a new instance of the message queue and begins its operation.  It
        /// should be called to ensure the message queue is ready to handle incoming messages.</remarks>
        public void StartMq()
        {
            mq = new Mq(this);
            mq.Start();
        }

        /// <summary>
        /// Retrieves a setting from the database and initiates the scrapper process.
        /// </summary>
        /// <remarks>This method performs two operations: it retrieves a setting from the database using
        /// the provided identifier and then starts the scrapper process. Ensure that the database connection string is
        /// properly configured before calling this method.</remarks>
        /// <param name="value">The identifier used to retrieve the specific setting from the database. Must be a valid integer.</param>
        /// <returns></returns>
        internal async Task GetSettingAsync(int value)
        {
            await DatabaseSetting.GetSettingFromDbAsync(value, this, _connectionString);
            await StartScrapperAsync();
        }

        /// <summary>
        /// Initiates the web scraping process asynchronously.
        /// </summary>
        /// <remarks>This method starts the web scraper and initializes the necessary components for the
        /// scraping operation. Any exceptions encountered during the initialization are caught and logged to the
        /// console.</remarks>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task StartScrapperAsync()
        {
            try
            {
                Console.WriteLine("Starting Scrapper");
                _ = new MainScrapping(this, _connectionString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"start scrap exc: {ex.Message}");
            }
        }
    }
}
