using OctVisionEngine.Models;


namespace OctVisionEngine.ViewModels;

public class Album_ViewModel : ViewModelBase
{
    private readonly Album _album;

    public Album_ViewModel(Album album)
    {
        _album = album;
    }

    public string Artist => _album.Artist;

    public string Title => _album.Title;
    
}