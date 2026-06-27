using AllegroParse;


Console.WriteLine("Urls: Saved (s) / NewParse (n)");
bool needParse = Console.ReadLine()?.Trim() == "n";
List<string> urls;
if (needParse)
{
    SiteMapExtracter siteMapExtracter = new SiteMapExtracter();
    urls = await siteMapExtracter.ExtractFromUrls("https://allenett.pl/product-sitemap1.xml","https://allenett.pl/product-sitemap2.xml","https://allenett.pl/product-sitemap3.xml","https://allenett.pl/product-sitemap4.xml");
}
else
{
    urls = SaverExtensions.Urls.Value;
}
Console.WriteLine($"Urls: {urls.Count}, Start from?(0-{urls.Count})");
int startIndex = int.Parse(Console.ReadLine()??"0");

ProductParcer productParcer = new ProductParcer();

var responseParsing = await productParcer.Parse(urls.ToArray(),startIndex);
Console.WriteLine($"Black urls:{responseParsing.BlackListUrls.Count}\nProducts:{responseParsing.Products.Count}");
Console.WriteLine("need save ? (y/n)");
var entered = Console.ReadLine() ?? "";
if (entered.ToLower() == "y")
{
    foreach (var product in responseParsing.Products.Values)
    {
        SaverExtensions.Products.Value[product.Url] = product;
    }
    SaverExtensions.Products.Write();
    
    var blackList = SaverExtensions.UrlsBlackList.Value;
    blackList.AddRange(responseParsing.BlackListUrls);
    blackList = blackList.Distinct().ToList();
    SaverExtensions.UrlsBlackList.Value = blackList;
    SaverExtensions.UrlsBlackList.Write();
    
    urls.RemoveAll(x => responseParsing.BlackListUrls.Contains(x));
    SaverExtensions.Urls.Value = urls;
    SaverExtensions.Urls.Write();
}
else
{
    
}
