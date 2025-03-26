using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Serilog;
using StockCheckerCLI.Models;

namespace StockCheckerCLI.Scrapers
{
    public class OneFodiscountScraper : IScraper
    {
        private readonly string _url;

        public OneFodiscountScraper(string url)
        {
            _url = url;
        }

        public async Task<List<Product>> ScrapeAsync()
        {
            var products = new List<Product>();

            try
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0");
                httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fr,fr-FR;q=0.8,en-US;q=0.6,en;q=0.4,ru;q=0.2");
                httpClient.DefaultRequestHeaders.Add("DNT", "1");
                httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
                httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
                httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
                httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
                httpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
                httpClient.DefaultRequestHeaders.Add("Priority", "u=0, i");
                
                var response = await httpClient.GetStringAsync(_url);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(response);

                // Select the product nodes using the specific XPath for 1fodiscount
                var productNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'product-tile_buybox')]");
                
                if (productNodes != null)
                {
                    foreach (var productNode in productNodes)
                    {
                        // --- Early Stock Check ---
                        var stockDiv = productNode.SelectSingleNode(".//div[contains(@class, 'product-tile_stock')]");
                        if (stockDiv == null)
                            continue;
                        var stockClass = stockDiv.GetAttributeValue("class", "");
                        // Process only products with the "-inStock" indicator
                        if (!stockClass.Contains("-inStock"))
                            continue;

                        // --- Continue extraction for in-stock products ---
                        var product = new Product();
                        product.Status = StockStatus.InStock;

                        // Extract product URL and Name from the parent tile
                        var parentTile = productNode.ParentNode;
                        var linkNode = parentTile.SelectSingleNode(".//a[contains(@class, 'title')]");
                        if (linkNode != null)
                        {
                            var productUrl = linkNode.GetAttributeValue("href", "");
                            product.Url = $"https://www.1fodiscount.com{productUrl}";
                            product.Name = WebUtility.HtmlDecode(linkNode.InnerText.Trim());
                        }

                        // Extract and decode the product price
                        var priceNode = productNode.SelectSingleNode(".//div[contains(@class, 'product-tile_buybox_offers_offer_price')]");
                        product.Price = priceNode != null ? WebUtility.HtmlDecode(priceNode.InnerText.Trim()) : "Unavailable";

                        // Only add the product if a valid name and URL were found
                        if (!string.IsNullOrWhiteSpace(product.Name) && !string.IsNullOrWhiteSpace(product.Url))
                        {
                            products.Add(product);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error scraping 1fodiscount: {ErrorMessage}", ex.Message);
            }
            
            return products;
        }
    }
}
