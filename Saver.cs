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
            Value = default!;
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
    public static Saver<string[]> Urls = new Saver<string[]>("urls.txt");
}