using System.IO;
using iTunesSearch.Library;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MsBox.Avalonia; 
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using iTunesSearch.Library.Models;


namespace OctVisionEngine.Models;

public class Album // 文件名相同，声明这里是构造函数，用来保证每一次new的时候，强制执行初始化
{
    public Album(string artist, string title, string coverUrl)  // 声明强制初始化（被调用来new一个对象）时，必须传入的参数
    {
        Artist = artist;
        Title = title;
        CoverUrl = coverUrl;
    }
    private static iTunesSearchManager s_SearchManager = new();
    private static HttpClient s_httpClient = new();
    private string CachePath => $"./Cache/{SanitizeFileName(Artist)} - {SanitizeFileName(Title)}";
    public string Artist { get; set; }
    public string Title { get; set; }
    public string CoverUrl { get; set; }
    
    public static async Task<IEnumerable<Album>> SearchAsync(string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        { return Enumerable.Empty<Album>(); }
    
        var query = await s_SearchManager.GetAlbumsAsync(searchTerm)
            .ConfigureAwait(false);

        return query.Albums.Select(x =>
            new Album(x.ArtistName, x.CollectionName, x.ArtworkUrl100.Replace("100x100bb", "600x600bb")));
    }

    private static string SanitizeFileName(string input)
    {
        foreach (var c in System.IO.Path.GetInvalidFileNameChars())
        { input = input.Replace(c, '_'); }
        return input;
    }
    
    public async Task<Stream> LoadCoverBitmapAsync()
    {
        if (File.Exists(CachePath + ".bmp"))
        { return File.OpenRead(CachePath + ".bmp"); }
        else
        {
            var data = await s_httpClient.GetByteArrayAsync(CoverUrl);
            return new MemoryStream(data);
        }
    }

    public Stream SaveCoverBitmapStream()
    { return File.OpenWrite(CachePath + ".bmp"); }
    private static async Task SaveToStreamAsync(Album data, Stream stream)
    { await JsonSerializer.SerializeAsync(stream, data).ConfigureAwait(false); }
    public async Task SaveAsync()
    {
        if (!Directory.Exists("./Cache"))
        { Directory.CreateDirectory("./Cache"); }
        // else
        // {
        //     var messageBox = MessageBoxManager.GetMessageBoxStandard(title: "Warn", text:"Cache not exist.");
        //     await messageBox.ShowAsync();
        // }

        using (var fs = File.OpenWrite(CachePath))
        { await SaveToStreamAsync(this, fs); }
    }

    public static async Task<Album> LoadFromStream(Stream stream)
    { return await (JsonSerializer.DeserializeAsync<Album>(stream).ConfigureAwait(false))!; }

    public static async Task<IEnumerable<Album>> LoadCachedAsync()
    {
        if (!Directory.Exists("./Cache"))
        { Directory.CreateDirectory("./Cache"); }

        var results = new List<Album>();

        foreach (var file in Directory.EnumerateFiles("./Cache"))
        {
            if (!string.IsNullOrWhiteSpace(new DirectoryInfo(file).Extension)) continue;
            await using var fs = File.OpenRead(file);
            results.Add(await Album.LoadFromStream(fs).ConfigureAwait(false));
        }

        return results;
    }

}