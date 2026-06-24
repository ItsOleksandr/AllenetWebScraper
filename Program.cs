using AllegroParse;

var urls = SaverExtensions.Urls.Value;
string url = urls[1];
Console.WriteLine(url);

using HttpClient client = new();
string html = await client.GetStringAsync(url);
HtmlExtracter htmlExtracter = new HtmlExtracter();
var result = htmlExtracter.Extract(html);
result.Url = url;
Console.WriteLine($"ProductInfo:\n Name: {result.Name}\n  Price: {result.Price}\n Count: {result.Count}\n EAN: {result.EAN}");

