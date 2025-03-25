using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Serilog;
using StockCheckerCLI.Models;
using StockCheckerCLI.Core;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var stockChecker = new StockChecker(config);
        var knownProducts = new Dictionary<string, Product>();

        while (true)
        {
            var inStockProducts = await stockChecker.GetInStockProductsAsync();
            var currentUrls = new HashSet<string>(inStockProducts.Select(p => p.Url));
            var knownUrls = new HashSet<string>(knownProducts.Keys);

            var addedUrls = currentUrls.Except(knownUrls);
            var removedUrls = knownUrls.Except(currentUrls);

            foreach (var removed in removedUrls)
            {
                knownProducts.Remove(removed);
            }

            var newProducts = inStockProducts.Where(p => addedUrls.Contains(p.Url)).ToList();

            if (newProducts.Any())
            {
                Console.Beep();
                Log.Information("New in-stock products:");
                foreach (var product in newProducts)
                {
                    string formattedPrice = product.PriceValue.ToString("F2") + "€";
                    Log.Information("[NEW] {PriceFormatted} - {Name} | {Url}", formattedPrice, product.Name, product.Url);
                    knownProducts[product.Url] = product;
                }
            }

            await Task.Delay(10000);
        }
    }
}
