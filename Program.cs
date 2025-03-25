using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using StockCheckerCLI.Models;
using StockCheckerCLI.Scrapers;
using StockCheckerCLI.Helpers;

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
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Retrieve URLs from configuration
        var oneFodiscountUrl = config["ScraperSettings:Onefodiscount:Url"] ?? throw new ArgumentNullException("ScraperSettings:Url is not defined in appsettings.json");
        var grosbillUrl = config["ScraperSettings:Grosbill:Url"] ?? throw new ArgumentNullException("ScraperSettings:Url is not defined in appsettings.json");
        var ldlcUrl = config["ScraperSettings:LDLC:Url"] ?? throw new ArgumentNullException("ScraperSettings:Url is not defined in appsettings.json");
        var pccomponentesUrl = config["ScraperSettings:PCComponentes:Url"] ?? throw new ArgumentNullException("ScraperSettings:Url is not defined in appsettings.json");
        var infomaxUrl = config["ScraperSettings:InfomaxParis:Url"] ?? throw new ArgumentNullException("ScraperSettings:Url is not defined in appsettings.json");


        // Scrape 1fodiscount
        IScraper oneFodiscountScraper = new OneFodiscountScraper(oneFodiscountUrl);
        List<Product> oneFodiscountProducts = await oneFodiscountScraper.ScrapeAsync();

        // Scrape Grosbill
        IScraper grosbillScraper = new GrosbillScraper(grosbillUrl);
        List<Product> grosbillProducts = await grosbillScraper.ScrapeAsync();

        // Scrape LDLC
        IScraper ldlcScraper = new LDLCWebScraper(ldlcUrl);
        List<Product> ldlcProducts = await ldlcScraper.ScrapeAsync();

        // Scrape PCComponentes
        IScraper pccomponentesScraper = new PCComponentesScraper(pccomponentesUrl);
        List<Product> pccomponentesProducts = await pccomponentesScraper.ScrapeAsync();
        
        // Scrape Infomax Paris
        IScraper infomaxScraper = new InfomaxParisScraper(infomaxUrl);
        List<Product> infomaxProducts = await infomaxScraper.ScrapeAsync();
        
        // Combine products from both sites
        var allProducts = new List<Product>();
        allProducts.AddRange(oneFodiscountProducts);
        allProducts.AddRange(grosbillProducts);
        allProducts.AddRange(ldlcProducts);
        allProducts.AddRange(pccomponentesProducts);
        allProducts.AddRange(infomaxProducts);

        // Update PriceValue property for each product using the helper method
        foreach (var product in allProducts)
        {
            product.PriceValue = PriceHelper.ParsePrice(product.Price);
        }

        // Filter in-stock products and sort them by PriceValue (lowest to highest)
        var inStockProducts = allProducts.FindAll(p => p.Status == StockStatus.InStock);
        inStockProducts.Sort((p1, p2) => p1.PriceValue.CompareTo(p2.PriceValue));

        // Display sorted in-stock products
        if (inStockProducts.Count > 0)
        {
            Log.Information("In-stock products (sorted by price):");
            foreach (var product in inStockProducts)
            {
                //Log.Information("[IN STOCK] {Name} - {Price} | {Url}", product.Name, product.Price, product.Url);
                string formattedPrice = product.PriceValue.ToString("F2") + "€";
                Log.Information("[IN STOCK] {PriceFormatted} - {Name} | {Url}", formattedPrice, product.Name, product.Url);
            }
        }
        else
        {
            Log.Information("No in-stock products found at the moment.");
        }

        Log.CloseAndFlush();
    }
}
