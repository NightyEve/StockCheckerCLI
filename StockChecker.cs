using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using StockCheckerCLI.Models;
using StockCheckerCLI.Scrapers;
using StockCheckerCLI.Helpers;

namespace StockCheckerCLI.Core
{
    public class StockChecker
    {
        private readonly IConfiguration _config;

        public StockChecker(IConfiguration config)
        {
            _config = config;
        }

        public async Task<List<Product>> GetInStockProductsAsync()
        {
            var allProducts = new List<Product>();

            var oneFodiscountUrl = _config["ScraperSettings:Onefodiscount:Url"] ?? throw new ArgumentNullException("ScraperSettings:Onefodiscount:Url");
            var grosbillUrl = _config["ScraperSettings:Grosbill:Url"] ?? throw new ArgumentNullException("ScraperSettings:Grosbill:Url");
            var ldlcUrl = _config["ScraperSettings:LDLC:Url"] ?? throw new ArgumentNullException("ScraperSettings:LDLC:Url");
            var pccomponentesUrl = _config["ScraperSettings:PCComponentes:Url"] ?? throw new ArgumentNullException("ScraperSettings:PCComponentes:Url");
            var infomaxUrl = _config["ScraperSettings:InfomaxParis:Url"] ?? throw new ArgumentNullException("ScraperSettings:InfomaxParis:Url");

            var scrapers = new List<IScraper>
            {
                new OneFodiscountScraper(oneFodiscountUrl),
                new GrosbillScraper(grosbillUrl),
                new LDLCWebScraper(ldlcUrl),
                new PCComponentesScraper(pccomponentesUrl),
                new InfomaxParisScraper(infomaxUrl)
            };

            foreach (var scraper in scrapers)
            {
                try
                {
                    var scraped = await scraper.ScrapeAsync();
                    foreach (var product in scraped)
                    {
                        product.PriceValue = PriceHelper.ParsePrice(product.Price);
                    }
                    allProducts.AddRange(scraped);
                }
                catch (Exception ex)
                {
                    Log.Warning("Scraper {ScraperName} failed: {Message}", scraper.GetType().Name, ex.Message);
                }
            }

            return allProducts
                .Where(p => p.Status == StockStatus.InStock && !string.IsNullOrWhiteSpace(p.Url) && p.PriceValue > 1)
                .GroupBy(p => p.Url)
                .Select(g => g.First())
                .OrderBy(p => p.PriceValue)
                .ToList();
        }
    }
}