using AllegroParse;
using Microsoft.Playwright;

var products = SaverExtensions.Products.Value;
Console.WriteLine($"Already handled: {products.Count}");
Console.WriteLine($"With ean: {products.Count(x => x.EAN != "—")}");

return;

var urls = SaverExtensions.Urls.Value;
var playwright = await Playwright.CreateAsync();
var browser = await playwright.Chromium.LaunchPersistentContextAsync(Path.Combine(Directory.GetCurrentDirectory(),"Resources","PlaywrightData"), new BrowserTypeLaunchPersistentContextOptions()
{
    Headless = false,
    Args = new[] { "--disable-blink-features=AutomationControlled", "--no-sandbox", "--disable-extensions", }
});
var page = await browser.NewPageAsync();

ProductExtracter extracter = new ProductExtracter(page);
SaverExtensions.Products.Value.Capacity = urls.Length;
int startIndex = SaverExtensions.Products.Value.Count;

for(int i = startIndex-1; i < urls.Length; i++)
{
    var url = urls[i];
    ProductInfo product;
    try
    { 
        product = await extracter.Extract(url);
    }
    catch (InvalidProductException)
    {
        var urlsFile = SaverExtensions.Urls.Value;
        var list = urlsFile.ToList();
        list.Remove(url);
        SaverExtensions.Urls.Value = list.ToArray();
        SaverExtensions.Urls.Write();
        Console.WriteLine($"Invalid product: {url}");
        continue;
    }
    catch(ProductAlreadyHandledException)
    {
        continue;
    }
    product.Url = url;
    SaverExtensions.Products.Value.Add(product);
    SaverExtensions.Products.Write();
    Console.WriteLine($"Url: {product.Url}\nHandled {i} / {urls.Length}\n");
}
await browser.CloseAsync();