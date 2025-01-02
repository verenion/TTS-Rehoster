using System.Diagnostics;
using Spectre.Console;

namespace TTSRehoster;

public class TtsSaveFile(string path)
{
    public readonly string Path = path;
    
    public readonly string PathHash = HashGenerator.Md5(path);
    
    public List<TtsAsset> Assets = new();

    private readonly string _rawContents = File.ReadAllText(path);

    private HashSet<TtsAsset> _assets = new();

    public IEnumerable<TtsAsset> FindAllAssets()
    {
        _assets = new HashSet<TtsAsset>();

        var regex = new System.Text.RegularExpressions.Regex(@"http(s?)?:\/\/[^\s""']+");

        foreach (System.Text.RegularExpressions.Match match in regex.Matches(_rawContents))
        {
            var url = match.Value;
            
            if (match.Value.Contains("http://cloud-3.steamusercontent.com/"))
            {
                url = match.Value.Replace("http://cloud-3.steamusercontent.com/",
                    "https://steamusercontent-a.akamaihd.net/");
                AnsiConsole.MarkupLineInterpolated($"[yellow]Replaced {match.Value} with {url}[/]");
            }

            var builder = new UriBuilder(url);
            if (!builder.Uri.IsWellFormedOriginalString())
            {
                AnsiConsole.MarkupLineInterpolated($"[red]invalid url: {url}[/]");
            }
            
            if (_assets.Add(new TtsAsset()
                {
                    Url = url,
                    UrlHash = HashGenerator.Md5(url) ?? throw new InvalidOperationException($"Hash could not be computed for url: {url}")
                }))
            {
                Debug.WriteLine(" - found url: {0}", url);
            }
        }

        AnsiConsole.WriteLine("found {0} urls", _assets.Count);

        return _assets;
    }
}