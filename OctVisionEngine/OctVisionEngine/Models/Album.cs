using System.Collections;
using iTunesSearch.Library;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace OctVisionEngine.Models;

public class Album
{
    private static iTunesSearchManager s_SearchManager = new();
    
    public string Artist { get; set; }
    public string Title { get; set; }
    public string CoverUrl { get; set; }

    public Album(string artist, string title, string coverUrl)
    {
        Artist = artist;
        Title = title;
        CoverUrl = coverUrl;
    }

    public static async Task<IEnumerable<Album>> SearchAsync(string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Enumerable.Empty<Album>();
        }

        var query = await s_SearchManager.GetAlbumsAsync(searchTerm)
            .ConfigureAwait(false);

        return query.Albums.Select(x =>
            new Album(x.ArtistName, x.CollectionName, x.ArtworkUrl100.Replace("100x100bb", "600x600bb")));
    }

}