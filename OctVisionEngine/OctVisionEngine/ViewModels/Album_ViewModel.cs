using OctVisionEngine.Models;
using Avalonia.Media.Imaging;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OctVisionEngine.ViewModels;

public partial class Album_ViewModel : ViewModelBase
{
    private readonly Album _album;
    [ObservableProperty] public partial Bitmap? Cover { get; private set; }

    public async Task LoadCover()
    {
        await using (var imageStream = await _album.LoadCoverBitmapAsync())
        {
            Cover = await Task.Run(() => Bitmap.DecodeToWidth(imageStream, 400));
        }
    }
    
    public Album_ViewModel(Album album)
    {
        _album = album;
    }

    public string Artist => _album.Artist;

    public string Title => _album.Title;
    
}