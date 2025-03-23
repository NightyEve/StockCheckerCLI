using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using StockCheckerCLI.Models;
using StockCheckerCLI.Scrapers;

class Program
{
    static async Task Main(string[] args)
    {
        // Initialize configuration
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        // Initialize Serilog logger
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Get URLs from configuration
        var oneFodiscountUrl = config["ScraperSettings:OneFodiscountUrl"] ?? throw new ArgumentNullException("ScraperSettings:Url is not defined in appsettings.json");
        var grosbillUrl = config["ScraperSettings:GrosbillUrl"] ?? throw new ArgumentNullException("ScraperSettings:Url is not defined in appsettings.json");

        // Scrape 1fodiscount
        IScraper oneFodiscountScraper = new OneFodiscountScraper(oneFodiscountUrl);
        List<Product> oneFodiscountProducts = await oneFodiscountScraper.ScrapeAsync();

        // Scrape Grosbill
        IScraper grosbillScraper = new GrosbillScraper(grosbillUrl);
        List<Product> grosbillProducts = await grosbillScraper.ScrapeAsync();

        // Combine products from both sites
        var allProducts = new List<Product>();
        allProducts.AddRange(oneFodiscountProducts);
        allProducts.AddRange(grosbillProducts);

        // Display only in-stock products
        var inStockProducts = allProducts.FindAll(p => p.Status == StockStatus.InStock);
        if (inStockProducts.Count > 0)
        {
            Log.Information("In-stock products:");
            foreach (var product in inStockProducts)
            {
                Log.Information("[IN STOCK] {Name} - {Price} | {Url}", product.Name, product.Price, product.Url);
            }
        }
        else
        {
            Log.Information("No in-stock products found at the moment.");
        }

        Log.CloseAndFlush();
    }
}
