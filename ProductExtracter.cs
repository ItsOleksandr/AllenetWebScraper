using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
namespace AllegroParse;

public class ProductExtracter
{
    private readonly IPage _page;
    private PageGotoOptions _gotoOptions = new PageGotoOptions {Timeout = 10000 , WaitUntil = WaitUntilState.Load};
    public ProductExtracter(IPage page)
    {
        _page = page;
    }
    
    public async Task<ProductInfo> Extract(string url)
    {
        await _page.GotoAsync(url, _gotoOptions);
        var acceptCookieButton = _page.GetByText("Akceptuj wszystko");
        if (await acceptCookieButton.IsVisibleAsync())
        {
            await acceptCookieButton.ClickAsync();
            await Task.Delay(1000);
        }

        var unAuthButton = _page.GetByText("Zaloguj się / Zarejestruj").First;
        if (await unAuthButton.IsVisibleAsync())
        {
            Console.WriteLine("Please authorize!!!!");
            Console.ReadLine();
            await _page.GotoAsync(url, _gotoOptions);
        }
        
        var name = (await _page.Locator("h1.product_title.entry-title").InnerTextAsync()).Trim();
        var ean = (await _page.Locator("span.ean").InnerTextAsync()).Trim();
        string countString = "";
        try
        {
            var notAvailableTask = _page.Locator("form.ct-product-waitlist-form").First
                .WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible, Timeout = 5000 });
            var countRawTask = _page.Locator("p.stock.in-stock").First
                .InnerTextAsync(new LocatorInnerTextOptions() { Timeout = 5000 });
            var first = await Task.WhenAny(notAvailableTask, countRawTask);
            if (!first.IsCompleted) throw new Exception("No complete");
            if (first == countRawTask)
            {
                var countRaw = await countRawTask;
                countString = Regex.Match(countRaw.Trim(), @"\d+").Value;
            }
            else if (first == notAvailableTask)
            {
                throw new InvalidProductException();
            }
            else
            {
                throw new Exception("Unknown error");
            }
        }
        catch (InvalidProductException)
        {
            throw;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            do
            {
                Console.WriteLine("Skip (S) / Delete (D)");
                var entered = Console.ReadLine()?.Trim() ?? "";
                if (entered == "s")
                {
                    throw new ProductAlreadyHandledException();
                }
                else if (entered == "d")
                {
                    throw new InvalidProductException();
                }
            } while (true);
        }

        var price = await _page.Locator("meta[property='product:price:amount']").GetAttributeAsync("content") ?? "";
        
        return new ProductInfo {Price = decimal.Parse(price,CultureInfo.InvariantCulture), Name = name, Count = int.Parse(countString),EAN = ean};
    }
}

public class InvalidProductException : Exception
{
    
}

public class ProductAlreadyHandledException : Exception
{
    
}