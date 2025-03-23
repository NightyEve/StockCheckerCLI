using System.Collections.Generic;
using System.Threading.Tasks;
using StockCheckerCLI.Models;

namespace StockCheckerCLI.Scrapers
{
    public interface IScraper
    {
        Task<List<Product>> ScrapeAsync();
    }
}
