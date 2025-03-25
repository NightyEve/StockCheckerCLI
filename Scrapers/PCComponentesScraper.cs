using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Serilog;
using StockCheckerCLI.Models;

namespace StockCheckerCLI.Scrapers
{
    public class PCComponentesScraper : IScraper
    {
        private readonly string _url;

        public PCComponentesScraper(string url)
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
                
                var response = await httpClient.GetAsync(_url);
                response.EnsureSuccessStatusCode();
                var html = await response.Content.ReadAsStringAsync();


                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var productNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'product-card')]");
                if (productNodes != null)
                {
                    foreach (var productNode in productNodes)
                    {
                        var product = new Product();

                        // Product name
                        var nameNode = productNode.SelectSingleNode(".//h3[contains(@class, 'product-card__title')]");
                        if (nameNode != null)
                            product.Name = WebUtility.HtmlDecode(nameNode.InnerText.Trim());

                        // Product price
                        var priceNode = productNode.SelectSingleNode(".//span[contains(@class, 'product-card__price-container')]");
                        if (priceNode == null)
                            priceNode = productNode.SelectSingleNode(".//span[contains(@data-e2e, 'price-card')]");

                        if (priceNode != null)
                            product.Price = WebUtility.HtmlDecode(priceNode.InnerText.Trim());

                        // Product URL (fallback from JSON-LD or extract from script later)
                        var urlNode = productNode.SelectSingleNode(".//a[contains(@href, '/carte-graphique')]");
                        if (urlNode != null)
                        {
                            var href = urlNode.GetAttributeValue("href", "");
                            product.Url = href.StartsWith("http") ? href : "https://www.pccomponentes.fr" + href;
                        }

                        product.Status = StockStatus.InStock;

                        if (!string.IsNullOrWhiteSpace(product.Name) && !string.IsNullOrWhiteSpace(product.Price))
                        {
                            products.Add(product);
                        }
                    }
                }
                else
                {
                    Log.Warning("No products found on PCComponentes page.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error scraping PCComponentes: {ErrorMessage}", ex.Message);
            }

            return products;
        }
    }
}
