using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Serilog;
using StockCheckerCLI.Models;

namespace StockCheckerCLI.Scrapers
{
    public class LDLCWebScraper : IScraper
    {
        private readonly string _url;

        public LDLCWebScraper(string url)
        {
            _url = url;
        }

        public async Task<List<Product>> ScrapeAsync()
        {
            var products = new List<Product>();

            try
            {
                var httpClient = new HttpClient();
                // Get the entire raw HTML content
                var rawHtml = await httpClient.GetStringAsync(_url);

                // Load the raw HTML into an HtmlDocument for static extraction
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(rawHtml);

                // Select product containers; we assume each product is the parent of the "pic" element.
                var productContainers = htmlDoc.DocumentNode.SelectNodes("//div[@class='pic']/parent::*");
                if (productContainers != null)
                {
                    foreach (var container in productContainers)
                    {
                        // First, check the stock status
                        var infoWrap = container.SelectSingleNode(".//div[contains(@class, 'wrap-stock')]");
                        bool inStock = false;
                        if (infoWrap != null)
                        {
                            var stockWebNode = infoWrap.SelectSingleNode(".//div[contains(@class, 'stock-web')]");
                            if (stockWebNode != null)
                            {
                                var stockText = WebUtility.HtmlDecode(stockWebNode.InnerText.Trim());
                                if (stockText.IndexOf("Rupture", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    stockText.IndexOf("Indisponible", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    // Skip this product as it's not in stock
                                    continue;
                                }
                                else
                                {
                                    inStock = true;
                                }
                            }
                        }

                        // If we reach here, we assume the product is in stock
                        var product = new Product();
                        product.Status = inStock ? StockStatus.InStock : StockStatus.Unknown;

                        // Extract the offer ID (used later for inline script extraction)
                        var offerId = container.GetAttributeValue("data-offer-id", string.Empty);

                        // Extract product URL and Name from the "dsp-cell-right" section
                        var infoNode = container.SelectSingleNode(".//div[contains(@class, 'dsp-cell-right')]");
                        if (infoNode != null)
                        {
                            var linkNode = infoNode.SelectSingleNode(".//div[contains(@class, 'pdt-desc')]/h3/a");
                            if (linkNode != null)
                            {
                                var href = linkNode.GetAttributeValue("href", "");
                                product.Url = href.StartsWith("http") ? href : "https://www.ldlc.com" + href;
                                product.Name = WebUtility.HtmlDecode(linkNode.InnerText.Trim());
                            }
                        }

                        // Try to extract the price from the basket element
                        var basketNode = container.SelectSingleNode(".//div[contains(@class, 'basket')]");
                        if (basketNode != null)
                        {
                            var priceNode = basketNode.SelectSingleNode(".//div[contains(@class, 'price')]/div[contains(@class, 'price')]");
                            if (priceNode != null)
                            {
                                product.Price = WebUtility.HtmlDecode(priceNode.InnerText.Trim());
                            }
                        }

                        // If price is still empty, attempt extraction from the inline JavaScript using the offer ID
                        if (string.IsNullOrWhiteSpace(product.Price) && !string.IsNullOrWhiteSpace(offerId))
                        {
                            product.Price = ExtractPriceFromScript(rawHtml, offerId) ?? "Unavailable";
                        }

                        // Only add the product if a valid Name and URL were found.
                        if (!string.IsNullOrWhiteSpace(product.Name) && !string.IsNullOrWhiteSpace(product.Url))
                        {
                            products.Add(product);
                        }
                    }
                }
                else
                {
                    Log.Warning("No product containers found on LDLC page.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error scraping LDLC: {ErrorMessage}", ex.Message);
            }

            return products;
        }

        // Private helper method to extract the price from the inline JavaScript in the raw HTML
        private string ExtractPriceFromScript(string rawHtml, string offerId)
        {
            // Build a regex pattern that finds the inline script for the product with the given offerId.
            // Pattern: [id*="pdt"][data-offer-id="OFFER_ID"] ... .querySelector('.price').outerHTML = '...';
            string pattern = $@"\[id\*=""pdt""\]\[data-offer-id=""{Regex.Escape(offerId)}""\].*?\.querySelector\('\.price'\)\.outerHTML\s*=\s*'(?<priceHtml>.*?)';";
            var match = Regex.Match(rawHtml, pattern, RegexOptions.Singleline);
            if (match.Success)
            {
                var priceHtml = match.Groups["priceHtml"].Value;
                // Extract the inner text from the nested <div class="price"> tag
                string innerPattern = @"<div\s+class=""price"">(?<priceText>.*?)<\/div>";
                var innerMatch = Regex.Match(priceHtml, innerPattern, RegexOptions.Singleline);
                if (innerMatch.Success)
                {
                    var priceText = innerMatch.Groups["priceText"].Value;
                    // Remove any remaining HTML tags (e.g., <sup>)
                    priceText = Regex.Replace(priceText, "<.*?>", string.Empty);
                    return WebUtility.HtmlDecode(priceText.Trim());
                }
                else
                {
                    // Fallback: remove all HTML tags from priceHtml directly
                    string cleaned = Regex.Replace(priceHtml, "<.*?>", string.Empty);
                    return WebUtility.HtmlDecode(cleaned.Trim());
                }
            }
            return string.Empty;
        }
    }
}
