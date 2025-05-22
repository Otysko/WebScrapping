namespace WebScrappingTrades
{
    internal static class Program
    {
        /// <summary>
        /// The entry point of the application.
        /// </summary>
        /// <remarks>Initializes and starts the application by invoking the <see cref="Startup.Start"/>
        /// method. The application runs indefinitely until the <see cref="Startup.run"/> flag is set to
        /// false.</remarks>
        private static void Main()
        {
            Startup startup = new();
            startup.Start();
            Console.WriteLine("Started");
            while (startup.run)
            {
                Thread.Sleep(Timeout.Infinite);
            }
        }
    }
}