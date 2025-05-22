namespace WebScrappingTrades.Models
{
    public class CoinData
    {
        public string? CoinName { get; set; }
        public string? Size { get; set; }
        public string? EntryPrice { get; set; }
        public string? MarkPrice { get; set; }
        public string? Time { get; set; }
        public string? PNL { get; set; }
        public string? ROI { get; set; }
        public string? Leverage { get; set; }
        public int Direction { get; set; }
        public string? Trader { get; set; }
    }
}