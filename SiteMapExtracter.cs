using System.Xml.Linq;
namespace AllegroParse;

public class SiteMapExtracter
{
    public async Task<string[]> ExtractFromUrls(params string[] urls)
    {
        List<string> mappedUrls = new List<string>();
        foreach (var url in urls)
        {
            mappedUrls.AddRange(await ExtractFromUrl(url));
        }
        return mappedUrls.ToArray();
    }
    
    public async Task<string[]> ExtractFromUrl(string url)
    {
        using HttpClient client = new HttpClient();
        var xml = await client.GetStringAsync(url);
        XDocument doc = XDocument.Parse(xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urls = doc.Descendants(ns + "url").Select(x => x.Element(ns + "loc")!.Value).ToArray();
        return urls;
    }
}