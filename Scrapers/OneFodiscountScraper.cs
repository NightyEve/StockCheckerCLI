using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
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
                var response = await httpClient.GetStringAsync(_url);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(response);

                // Select the product nodes using the specific XPath or class selector for 1fodiscount
                var productNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'product-tile_buybox')]");
                
                if (productNodes != null)
                {
                    foreach (var productNode in productNodes)
                    {
                        var product = new Product();

                        // Determine the stock status based on CSS classes
                        var stockDiv = productNode.SelectSingleNode(".//div[contains(@class, 'product-tile_stock')]");
                        if (stockDiv != null)
                        {
                            var stockClass = stockDiv.GetAttributeValue("class", "");

                            if (stockClass.Contains("-inStock"))
                                product.Status = StockStatus.InStock;
                            else if (stockClass.Contains("-delay"))
                                product.Status = StockStatus.Delay;
                            else if (stockClass.Contains("-rupture"))
                                product.Status = StockStatus.Rupture;
                            else
                                product.Status = StockStatus.Unknown;

                            // Extract product URL and name from the parent tile
                            var parentTile = productNode.ParentNode;
                            var linkNode = parentTile.SelectSingleNode(".//a[contains(@class, 'title')]");
                            if (linkNode != null)
                            {
                                var productUrl = linkNode.GetAttributeValue("href", "");
                                product.Url = $"https://www.1fodiscount.com{productUrl}";

                                // Decode HTML entities in the product name
                                product.Name = WebUtility.HtmlDecode(linkNode.InnerText.Trim());
                            }

                            // Extract and decode the product price
                            var priceNode = productNode.SelectSingleNode(".//div[contains(@class, 'product-tile_buybox_offers_offer_price')]");
                            product.Price = priceNode != null ? WebUtility.HtmlDecode(priceNode.InnerText.Trim()) : "Unavailable";

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
