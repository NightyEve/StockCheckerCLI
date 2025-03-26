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
        int previousLineCount = 0;
        Console.Clear();

        while (true)
        {
            Console.SetCursorPosition(0, 0);
            var now = DateTime.Now;
            Console.WriteLine($"[Stock Check] {now:yyyy-MM-dd HH:mm:ss}                ");

            var inStockProducts = await stockChecker.GetInStockProductsAsync();
            Console.WriteLine($"Total in-stock products found: {inStockProducts.Count}                \n");

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

            if (newProducts.Any(p => p.PriceValue < 3000))
            {
                _ = Task.Run(() =>
                {
                    Console.Beep(659, 125); // E5
                    Console.Beep(659, 125); // E5
                    Thread.Sleep(125);
                    Console.Beep(659, 125); // E5
                    Thread.Sleep(167);
                    Console.Beep(523, 125); // C5
                    Console.Beep(659, 125); // E5
                    Thread.Sleep(125);
                    Console.Beep(784, 125); // G5
                    Thread.Sleep(375);
                    Console.Beep(392, 125); // G4
                });
            }

            int lineCount = 2;
            foreach (var product in inStockProducts)
            {
                string key = $"{product.Url}|{product.Name}";
                string formattedPrice = product.PriceValue.ToString("F2") + "€";

                // Price color (dark yellow/orange-like)
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                // Clear the current line before writing
                Console.Write("\r" + new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, Console.CursorTop);

                // Then write the new content
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
                lineCount++;
            }

            // Clear any extra lines from previous render
            for (int i = lineCount; i < previousLineCount; i++)
            {
                Console.WriteLine(new string(' ', Console.WindowWidth));
            }
            previousLineCount = lineCount;

            var random = new Random();
            await Task.Delay(random.Next(8000, 15000));
        }
    }
}
