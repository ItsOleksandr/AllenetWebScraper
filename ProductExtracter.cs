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
            await unAuthButton.ClickAsync();
            await Task.Delay(4000);
            await _page.Locator("#rememberme").SetCheckedAsync(true);
            await _page.GetByText("Zaloguj się").First.ClickAsync();
            await _page.GotoAsync(url, _gotoOptions);
        }

        try
        {
            var name = (await _page.Locator("h1.product_title.entry-title").InnerTextAsync()).Trim();
            var ean = (await _page.Locator("span.ean").InnerTextAsync()).Trim();

            var notAvailableTask = _page.Locator("form.ct-product-waitlist-form").First
                .WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible, Timeout = 5000 });
            var countRawTask = _page.Locator("p.stock.in-stock").First
                .InnerTextAsync(new LocatorInnerTextOptions() { Timeout = 5000 });
            var first = await Task.WhenAny(notAvailableTask, countRawTask);
            string countString = "";
            if (!first.IsCompleted) throw new Exception("No complete");
            if (first == countRawTask)
            {
                var countRaw = await countRawTask;
                countString = Regex.Match(countRaw.Trim(), @"\d+").Value;
            }
            else if (first == notAvailableTask)
            {
                countString = "0";
            }
            else
            {
                throw new Exception("Unknown error");
            }

            var price = await _page.Locator("meta[property='product:price:amount']").GetAttributeAsync("content") ?? "";
            var categoriesLocator = await _page.Locator("span.posted_in a").AllAsync();

            var categoriesUrl = new List<string>();
            foreach (ILocator locator in categoriesLocator)
            {
                var href = await locator.GetAttributeAsync("href");
                if (href == null) continue;
                categoriesUrl.Add(href);
            }

            if (IsBlackListCategory(categoriesUrl)) throw new InvalidProductException("Category is blacklisted");
            return new ProductInfo
            {
                Price = decimal.Parse(price, CultureInfo.InvariantCulture), Name = name, Count = int.Parse(countString),
                EAN = ean, CategoriesUrls = categoriesUrl.ToArray(), Url = url
            };
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
                Console.WriteLine("Skip (S) / Delete (D) / Again (A) / Break (B)");
                var entered = Console.ReadLine()?.Trim() ?? "";
                if (entered == "s")
                {
                    throw new ProductAlreadyHandledException();
                }
                else if (entered == "d")
                {
                    throw new InvalidProductException();
                }
                else if (entered == "a")
                {
                    return await Extract(url);
                }
                else if (entered == "b")
                {
                    throw new ParserException();
                }
            } while (true);
        }
    }
    
    public bool IsBlackListCategory(IEnumerable<string> categoriesUrls)
    {
        foreach (string categoriesUrl in categoriesUrls)
        {
            foreach (string blackListUrl in SaverExtensions.CategoriesBlackList.Value)
            {
                if(categoriesUrl.Contains(blackListUrl)) return true;
            }
        }

        return false;
    }
}

public class InvalidProductException(string message = "") : Exception(message)
{
    
}

public class ProductAlreadyHandledException : Exception
{
    
}

public class ParserException : Exception{}