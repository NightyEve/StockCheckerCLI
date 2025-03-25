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
    public class GrosbillScraper : IScraper
    {
        private readonly string _url;

        public GrosbillScraper(string url)
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

                // Select product containers based on a common class pattern
                var productNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'grb__liste-produit__liste__produit')]");
                if (productNodes != null)
                {
                    foreach (var productNode in productNodes)
                    {
                        // --- Early Stock Check ---
                        // Check the availability block "grb__liste-produit__disponibilite"
                        var dispoNode = productNode.SelectSingleNode(".//div[contains(@class, 'grb__liste-produit__disponibilite')]");
                        if (dispoNode == null)
                        {
                            // Skip this product if no availability information is found.
                            continue;
                        }
                        var noStockNode = dispoNode.SelectSingleNode(".//span[contains(@class, 'prodfiche_nodispo')]");
                        if (noStockNode != null)
                        {
                            // Product is marked as out of stock, skip it.
                            continue;
                        }

                        // --- Continue with extraction only for in-stock products ---
                        var product = new Product();

                        // Extract the product URL
                        var linkNode = productNode.SelectSingleNode(".//a[contains(@href, '/carte-graphique/')]");
                        if (linkNode != null)
                        {
                            var href = linkNode.GetAttributeValue("href", "");
                            product.Url = href.StartsWith("http") ? href : "https://www.grosbill.com" + href;
                        }

                        // Extract the product name
                        var nameNode = productNode.SelectSingleNode(".//div[contains(@class, 'grb__liste-produit__liste__produit__information__libelle')]/p");
                        if (nameNode != null)
                        {
                            product.Name = WebUtility.HtmlDecode(nameNode.InnerText.Trim());
                        }
                        else
                        {
                            // Fallback: use the alt attribute of an image inside an anchor with class "prod_txt_left"
                            var altNameNode = productNode.SelectSingleNode(".//a[contains(@class, 'prod_txt_left')]/img");
                            if (altNameNode != null)
                            {
                                product.Name = WebUtility.HtmlDecode(altNameNode.GetAttributeValue("alt", "").Trim());
                            }
                        }

                        // Extract the price
                        var priceNode = productNode.SelectSingleNode(".//span[contains(@class, 'grb__liste-produit__liste__produit__reference-container__content_prix_produit')]");
                        if (priceNode != null)
                        {
                            product.Price = WebUtility.HtmlDecode(priceNode.InnerText.Trim());
                        }
                        else
                        {
                            // Fallback: try the purchase price container
                            var altPriceNode = productNode.SelectSingleNode(".//div[contains(@class, 'grb__liste-produit__liste__produit__achat__prix')]");
                            product.Price = altPriceNode != null ? WebUtility.HtmlDecode(altPriceNode.InnerText.Trim()) : "Unavailable";
                        }

                        // Only add the product if a valid name and URL were found.
                        if (!string.IsNullOrWhiteSpace(product.Name) && !string.IsNullOrWhiteSpace(product.Url))
                        {
                            product.Status = StockStatus.InStock;
                            products.Add(product);
                        }
                    }
                }
                else
                {
                    Log.Warning("No products found on Grosbill page.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error scraping Grosbill: {ErrorMessage}", ex.Message);
            }
            return products;
        }
    }
}
