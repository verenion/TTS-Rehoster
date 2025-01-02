// See https://aka.ms/new-console-template for more information

using TTSRehoster.Converters;

namespace TTSRehoster;

internal class Program
{
    private static readonly ISaveFileConverter Converter = new SaveToLocalUpscaledConverter();
    
    static void Main(string[] args)
    {
        Console.WriteLine("Initialising...");

        var path = Directory.GetCurrentDirectory() + "/saves";
        
        Directory.CreateDirectory("assets");
        Directory.CreateDirectory("cache");
        Directory.CreateDirectory("saves");
    
        Console.WriteLine("Reading files in {0}...", path);
        foreach (var file in Directory.GetFiles(path))
        {
            if (!file.EndsWith(".json")) continue;

            TtsSaveFile ttsSaveFile = new TtsSaveFile(file);
            
            Converter.Convert(ttsSaveFile);
        }
    }
}