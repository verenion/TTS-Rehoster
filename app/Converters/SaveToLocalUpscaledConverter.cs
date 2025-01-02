using System.Diagnostics;
using System.Security.Authentication;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Spectre.Console;
using Size = SixLabors.ImageSharp.Size;

namespace TTSRehoster.Converters;

using DownloadTask = Task<TtsAsset>;

public class SaveToLocalUpscaledConverter : ISaveFileConverter
{
    static SaveToLocalUpscaledConverter()
    {
        Directory.CreateDirectory(LocalCachePath);
    }

    private const string LocalCachePath = "assets";

    public void Convert(TtsSaveFile file)
    {
        var progress = AnsiConsole.Progress();
        progress.RefreshRate = TimeSpan.FromMilliseconds(100);

        progress
            .Columns(new SpinnerColumn(), new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn())
            .Start(ctx =>
            {
                AnsiConsole.MarkupLine("Processing [bold green]{0}[/]...", file.Path);
                var assets = file.FindAllAssets().ToList();

                // Define tasks
                var downloading = ctx.AddTask("Downloading...", true, assets.Count);
                var upscaling = ctx.AddTask("Upscaling...", false, assets.Count);
                var postprocessing = ctx.AddTask("Post Processing...", false, assets.Count);

                while (!ctx.IsFinished)
                {
                    // Simulate some work
                    DownloadInParallel(file, assets, downloading).Wait();
                    
                    var downloaded = file.Assets.Where(asset => asset is { Downloaded: true, MimeType: "image/png" or "image/jpeg" }).ToList();
                    upscaling.MaxValue = downloaded.Count;
                    postprocessing.MaxValue = downloaded.Count;
                    upscaling.StartTask();
                    
                    Parallel.ForEach(downloaded, new ParallelOptions { MaxDegreeOfParallelism = 5 }, asset =>
                    {
                        // get original image data
                        var imageInfo = Image.Identify(asset.LocalPath);
                        var originalSize = imageInfo.Size;
                        
                        ProcessStartInfo processStartInfo = new ProcessStartInfo
                        {
                            FileName = @".\Upscaling\realesrgan-ncnn-vulkan.exe",
                            Arguments = $"-i \"{asset.LocalPath}\" -o \"{asset.LocalPath}\" -s 4",
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };

                        AnsiConsole.MarkupLine("Upscaling [bold green]{0}[/]", asset.LocalPath);
                        
                        var process = Process.Start(processStartInfo);
                        process?.WaitForExit();
                        upscaling.Increment(1);
                        process.Kill();
                        
                        imageInfo = Image.Identify(asset.LocalPath);
                        
                        // for anything smaller than 2048x2048, simply continue
                        if (imageInfo is { Width: < 10000, Height: < 10000, })
                        {
                            postprocessing.Increment(1);
                            
                            return;
                        }
                        
                        Image image;
                        using (FileStream stream = new FileStream(asset.LocalPath, FileMode.Open, FileAccess.Read,
                                   FileShare.Read))
                        {
                            image = Image.Load(stream);
                            image.Mutate(x => x.Resize(new ResizeOptions()
                            {
                                Size = originalSize
                            }));
                        }
                        
                        
                        image.Save(asset.LocalPath);
                        postprocessing.Increment(1);
                    });
                    

                    var saveFile = File.ReadAllText(file.Path);
                    foreach (TtsAsset asset in downloaded)
                    {
                        saveFile = saveFile.Replace(asset.Url, @"file:///" + asset.LocalPath.Replace(@"\", @"\\"));
                    }
                    
                    // overwrite save file
                    File.WriteAllText(file.Path, saveFile );
                    
                    // dump data
                    using (StreamWriter sw = File.CreateText($"cache/{file.PathHash}.json"))
                    {
                        sw.Write(JsonSerializer.Serialize(file, new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            IncludeFields = true
                        }));
                    }
                }
            });
    }
    
    private async Task DownloadInParallel(TtsSaveFile file, List<TtsAsset> assets, ProgressTask downloadContext)
    {
        List<DownloadTask> downloadTasks = new List<DownloadTask>(assets.Count);
        
        HttpClientHandler handler = new HttpClientHandler()
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
            UseCookies = false,
            UseProxy = false,
            AllowAutoRedirect = true,
            SslProtocols = SslProtocols.None
        };
        HttpClient client = new HttpClient(handler);

        foreach (var asset in assets)
        {
            Console.WriteLine($"Downloading {asset.Url}");
            downloadTasks.Add(Task.Run(() => Download(asset)));
        }

        await foreach (var downloadTask in Task.WhenEach(downloadTasks))
        {
            if (downloadTask.IsCompletedSuccessfully)
            {
                file.Assets.Add(downloadTask.Result);
            }
            else
            {
                Console.WriteLine("Error downloading {0}", downloadTask.Result.Url);
            }
        }

        await Task.WhenAll(downloadTasks).ConfigureAwait(false);
        
        return;

        async DownloadTask Download(TtsAsset asset)
        {
            var response = await client.GetAsync(asset.Url).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                // Retry with https, as from the looks of it, the game is doing something funky to http requests
                
                var builder = new UriBuilder(response.RequestMessage.RequestUri.OriginalString);
                builder.Scheme = "https";
                builder.Port = 443;
                AnsiConsole.WriteLine("Error downloading {0}. Response: {1}. Retrying {2}", asset.Url, response.StatusCode, builder.ToString());
                response = await client.GetAsync(builder.ToString()).ConfigureAwait(false);

                // finally, fail
                if (!response.IsSuccessStatusCode)
                {
                    AnsiConsole.WriteLine("Error downloading {0}. Response: {1}", builder.ToString(), response.StatusCode);
                    downloadContext.Increment(1);
                    asset.Downloaded = false;
                    return asset;
                }
            }
            
            asset.MimeType = response.Content.Headers.ContentType?.MediaType;
            string filename = $"{LocalCachePath}\\{asset.UrlHash}{asset.Extension}";
            await using (var fileStream =
                         new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fileStream).ConfigureAwait(false);
            }

            downloadContext.Increment(1);

            asset.LocalPath = filename;
            asset.Downloaded = true;

            return asset;
        }
    }
}