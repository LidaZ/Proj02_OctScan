using OctVisionEngine.ViewModels;

namespace OctVisionEngine.Messages;


public class Message_CloseStoreWindow(Album_ViewModel selectedAlbum)
{
    public Album_ViewModel SelectedAlbum { get; } = selectedAlbum;
}