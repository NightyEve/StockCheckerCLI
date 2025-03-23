namespace StockCheckerCLI.Models;

public enum StockStatus
{
    InStock,
    Delay,
    Rupture,
    Unknown
}

public class Product
{
    public string Name { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public StockStatus Status { get; set; } = StockStatus.Unknown;
}
