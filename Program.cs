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
            .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var stockChecker = new StockChecker(config);
        var knownProducts = new Dictionary<string, Product>();

        while (true)
        {
            Console.Clear();
            var now = DateTime.Now;
            Console.WriteLine($"[Stock Check] {now:yyyy-MM-dd HH:mm:ss}");

            var inStockProducts = await stockChecker.GetInStockProductsAsync();
            Console.WriteLine($"Total in-stock products found: {inStockProducts.Count}\n");

            var currentKeys = inStockProducts.Select(p => $"{p.Url}|{p.Name}").ToHashSet();
            var knownKeys = knownProducts.Keys.ToHashSet();

            var addedKeys = currentKeys.Except(knownKeys);
            var removedKeys = knownKeys.Except(currentKeys);

            foreach (var removed in removedKeys)
            {
                knownProducts.Remove(removed);
            }

            var newProducts = inStockProducts
                .Where(p => addedKeys.Contains($"{p.Url}|{p.Name}"))
                .ToList();

            if (newProducts.Any())
            {
                Console.Beep();
            }

            foreach (var product in inStockProducts)
            {
                string key = $"{product.Url}|{product.Name}";
                string formattedPrice = product.PriceValue.ToString("F2") + "€";

                // Price color (dark yellow/orange-like)
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("[");
                Console.Write(formattedPrice);
                Console.Write("] ");

                // Name color
                if (addedKeys.Contains(key))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(product.Name);
                    knownProducts[key] = product;
                }
                else
                {
                    Console.ResetColor();
                    Console.Write(product.Name);
                }

                // URL in blue
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write(" | ");
                Console.WriteLine(product.Url);

                Console.ResetColor();
            }

            await Task.Delay(10000);
        }
    }
}