using AllegroParse;

bool isUser = !args.Contains("--no-console");
bool needParse;
if (isUser)
{
    Console.WriteLine("Urls: Saved (s) / NewParse (n)");
    needParse = Console.ReadLine()?.Trim() == "n";
}
else
{ 
    needParse = true;
}
Console.WriteLine("Start parsing ...");

List<string> urls;
if (needParse)
{
    SiteMapExtracter siteMapExtracter = new SiteMapExtracter();
    urls = await siteMapExtracter.ExtractFromUrls("https://allenett.pl/product-sitemap1.xml","https://allenett.pl/product-sitemap2.xml","https://allenett.pl/product-sitemap3.xml","https://allenett.pl/product-sitemap4.xml");
    if(!isUser) urls.RemoveAll(x=>SaverExtensions.UrlsBlackList.Value.Contains(x));
}
else
{
    urls = SaverExtensions.Urls.Value;
}

int startIndex = 0;
if (isUser)
{
    Console.WriteLine($"Urls: {urls.Count}, Start from?(0-{urls.Count})");
    startIndex = int.Parse(Console.ReadLine()??"0");
}
else
{
    startIndex = 0;
}

ProductParcer productParcer = new ProductParcer();

var responseParsing = await productParcer.Parse(urls.ToArray(),isUser,startIndex);

Console.WriteLine($"Black urls:{responseParsing.BlackListUrls.Count}\nProducts:{responseParsing.Products.Count}");
bool needSave;
if (isUser)
{
    Console.WriteLine("need save ? (y/n)");
    var entered = Console.ReadLine() ?? "";
    needSave = entered.ToLower() == "y";
}
else
{
    needSave = true;
}
if (needSave)
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

    var csvOptions = SaverExtensions.CSVOptions.Value;
    var products = SaverExtensions.Products.Value.Values;
    var filteredProducts = products
        .Where(x => x.CategoriesUrls
            .Any(categoryUrl => csvOptions.CategoriesBlackList
                .Any(categoryUrl
                    .Contains)) 
                    && x.Count >= csvOptions.MinimalProductCount
                    && x.Price >= csvOptions.MinimalPrice)
        .ToList();
    filteredProducts.ForEach(x=>x.Price *= csvOptions.MultiplierPrice);
    
    CSVMaker.GetCSV(filteredProducts);
}
else
{
    Console.WriteLine("Not saved, but you can see last session parsed in Resources/last_parse.txt");
}
