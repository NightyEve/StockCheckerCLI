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
    public class InfomaxParisScraper : IScraper
    {
        private readonly string _url;

        public InfomaxParisScraper(string url)
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

                var productNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'product-container')]");

                if (productNodes != null)
                {
                    foreach (var productNode in productNodes)
                    {
                        var product = new Product();

                        // Product name
                        var nameNode = productNode.SelectSingleNode(".//h5[contains(@class, 'product-name')]/a");
                        if (nameNode != null)
                        {
                            product.Name = WebUtility.HtmlDecode(nameNode.InnerText.Trim());

                            var href = nameNode.GetAttributeValue("href", "");
                            product.Url = href.StartsWith("http") ? href : "https://infomaxparis.com" + href;
                        }

                        // Product price
                        var priceNode = productNode.SelectSingleNode(".//span[contains(@class, 'price')]");
                        if (priceNode != null)
                        {
                            product.Price = WebUtility.HtmlDecode(priceNode.InnerText.Trim());
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
                    Log.Warning("No products found on InfomaxParis page.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error scraping InfomaxParis: {ErrorMessage}", ex.Message);
            }

            return products;
        }
    }
}