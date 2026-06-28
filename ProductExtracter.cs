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
    
    public async Task<ProductInfo> Extract(string url,bool isUserStarts)
    {
        await _page.GotoAsync(url, _gotoOptions);
        var acceptCookieButton = _page.GetByText("Akceptuj wszystko");
        if (await acceptCookieButton.IsVisibleAsync())
        {
            await acceptCookieButton.ClickAsync();
            await Task.Delay(1000);
        }

        await LogIn(url);
        

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
            if (isUserStarts)
            {
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
                        return await Extract(url,isUserStarts);
                    }
                    else if (entered == "b")
                    {
                        throw new ParserException();
                    }
                } while (true);
            }
            else
            {
                throw new ProductAlreadyHandledException();
            }
        }
    }

    private async Task LogIn(string productUrl)
    {
        for (int i = 0; i < 3; i++)
        {
            var unAuthButton = _page.GetByText("Zaloguj się / Zarejestruj").First;
            if (await unAuthButton.IsVisibleAsync())
            {
                await unAuthButton.ClickAsync();
                await Task.Delay(4000);

                var pressOptions = new LocatorPressSequentiallyOptions() { Delay = 200 };
                var userName = _page.Locator("#username");
                await userName.ClearAsync();
                await userName.PressSequentiallyAsync(SaverExtensions.Creaditails.Value.Login, pressOptions);
                var password = _page.Locator("#password");
                await password.ClearAsync();
                await password.PressSequentiallyAsync(SaverExtensions.Creaditails.Value.Password, pressOptions);

                await _page.Locator("#rememberme").SetCheckedAsync(true);

                await CaptchaSolve();
                
                await _page.GetByText("Zaloguj się").First.ClickAsync();
                await Task.Delay(2000);
                await _page.GotoAsync(productUrl, _gotoOptions);
                await Task.Delay(2000);
                if (!await unAuthButton.IsVisibleAsync()) return;
            }
            else
            {
                return;
            }
        }

        throw new MemberAccessException("Can`t log in");
    }

    private async Task CaptchaSolve()
    {
        var frame = _page.Frames.FirstOrDefault(x => x.Url.Contains("cloudflare.com"));
        
        if (frame != null)
        {
            var captcha = frame.Locator("body");
            if (await captcha.IsVisibleAsync())
            {
                await MoveMouseInsideElement(captcha, _page);
            }
            
            LocatorWaitForOptions options = new LocatorWaitForOptions() { State = WaitForSelectorState.Visible, Timeout = 10000};
            var waitForSuccess = frame.Locator("circle").WaitForAsync(options);
            var waitForInput = frame.Locator("input[type='checkbox']").WaitForAsync(options);
            
            var firstFinished = await Task.WhenAny(waitForInput, waitForSuccess);
            if (firstFinished == waitForSuccess)
            {
                return;
            }
            else
            {
                await waitForInput;
                await frame.Locator("input[type='checkbox']").ClickAsync();
                await Task.Delay(2000);
            }
            
            
        }
    }

    private async Task MoveMouseInsideElement(ILocator element, IPage page)
    {
        var box = await element.BoundingBoxAsync();
        if (box is null) return;

        var rnd = new Random();
        for (int i = 0; i < 5; i++)
        {
            float x = box.X + (float)rnd.NextDouble() * box.Width;
            float y = box.Y + (float)rnd.NextDouble() * box.Height;
            await page.Mouse.MoveAsync(x, y, new MouseMoveOptions { Steps = 10 });
            await page.WaitForTimeoutAsync(100);
        }
    }
}

public class InvalidProductException(string message = "") : Exception(message)
{
    
}

public class ProductAlreadyHandledException : Exception
{
    
}

public class ParserException : Exception{}