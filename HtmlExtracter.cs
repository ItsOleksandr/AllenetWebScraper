using HtmlAgilityPack;
namespace AllegroParse;

public class HtmlExtracter
{
    public ProductInfo Extract(string html)
    {
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(html);
        var name = doc.DocumentNode.SelectSingleNode("//h1[@class='product_title entry-title']").InnerText;
        var ean = doc.DocumentNode.SelectSingleNode("//span[@class='ean']").InnerText.Trim();
        var price = doc.DocumentNode.SelectSingleNode("//span[@class='woocommerce-Price-amount amount']").InnerText.Replace("zł netto","").Trim();
        var count = doc.DocumentNode.SelectSingleNode("//p[@class='stock in-stock']").InnerText.Trim();
        return new ProductInfo {Price = decimal.Parse(price), Name = name, Count = int.Parse(count),EAN = ean};
    }
}

public class ProductInfo
{
    public string Url { get; set; }
    public decimal Price { get; set; }
    public string Name { get; set; }
    public int Count { get; set; }
    public string EAN { get; set; }
}