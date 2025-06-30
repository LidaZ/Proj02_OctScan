using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using OctVisionEngine.Messages;

namespace OctVisionEngine.ViewModels;

public partial class StoreViewModel : ObservableObject
{
    // [ObservableProperty] 
    // private string Display_content = "This is Store view model layer";
    
    [RelayCommand]
    private async Task StoreViewModel_OpenAlbumWindow_Async()
    {
        // Code here will be executed when the buttom being pressed. Re-emerge test. 
        var tmp = await WeakReferenceMessenger.Default.Send(new Messages_OpenTextWindow());
    }
}