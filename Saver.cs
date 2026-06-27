using System.Text.Json;

namespace AllegroParse;

public class Saver<T> where T : class
{
    public T Value { get; set; } 

    public string _filePath { get; }

    public Saver(string fileName)
    {
        _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", fileName);
        try
        {
            Value = Read();
        }
        catch (Exception e) when(e is JsonException or FileNotFoundException)
        {
            Value = Activator.CreateInstance<T>();
        }
    }
    
    public void Write()
    {
        var content = JsonSerializer.Serialize(Value);
        File.WriteAllText(_filePath, content);
    }

    public T Read()
    {
        var content = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<T>(content) ?? throw new JsonException("Invalid JSON");
    }
}

public static class SaverExtensions
{
    public static readonly Saver<List<string>> Urls = new Saver<List<string>>("urls.txt");
    public static readonly Saver<List<string>> UrlsBlackList = new Saver<List<string>>("urls_black_list.txt");
    public static readonly Saver<Dictionary<string,ProductInfo>> Products = new Saver<Dictionary<string,ProductInfo>>("products_dictionary.txt");
    public static readonly Saver<CSVOptions> CSVOptions = new Saver<CSVOptions>("csv_options.txt");
    
    
}