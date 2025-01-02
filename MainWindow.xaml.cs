using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace tts_rehoster;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    public static string ComputeSha256Hash(string rawData)
    {
        // Create a SHA256 object
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // Compute the hash - returns byte array
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            // Convert byte array to a string
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2")); // Converts to hexadecimal representation
            }
            return builder.ToString();
        }
    }
    
    // open save.json as json
    private async void LoadSave(object sender, RoutedEventArgs e)
    {
        StreamReader sr = new StreamReader("save.json");
        string json = sr.ReadToEnd();
        sr.Close();
       // Debug.WriteLine(json);
        
        // Find all URLs in the JSON string and add them to a list
        Dictionary<string, string> urls = new Dictionary<string, string>();
        var regex = new System.Text.RegularExpressions.Regex(@"https?:\/\/[^\s""']+");

        foreach (System.Text.RegularExpressions.Match match in regex.Matches(json))
        {
            if (urls.TryAdd(match.Value, ComputeSha256Hash(match.Value)))
            {
                //Trace.WriteLine(match.Value);                
            }
        }

    
        // Download each URL to a file
        
        HttpClient webClient = new HttpClient();
        List<Task<HttpResponseMessage>> requests = new List<Task<HttpResponseMessage>>();

        foreach (KeyValuePair<string, string> kvp in urls)
        {
            Directory.CreateDirectory("downloads"); // Create downloads folder if it doesn't exist'
            
            requests.Add(webClient.GetAsync(kvp.Key));
        }

        await foreach (var response in Task.WhenEach(requests))
        {
            var content = response.Result;
            
            
            if (!content.IsSuccessStatusCode)
                continue;
            
            string fileName = "downloads/" + content.GetHashCode(); // Customize file name and extension as needed
            
            using var fileStream = new FileStream(fileName, FileMode.Create);
            content.Content.CopyToAsync(fileStream).Wait();
            Trace.WriteLine($"Downloaded to {fileName}");
        }
    }
}