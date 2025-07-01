using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using OctVisionEngine.Messages;

namespace OctVisionEngine.ViewModels
{
    public partial class ClassStoreViewModel : ViewModelBase
    {
        // StoreViewModel : ObservableObject
        [ObservableProperty]
        public partial string? SearchText { get; set; }

        [ObservableProperty] 
        public partial bool IsBusy { get; private set; }
        
        [ObservableProperty]
        public partial Album_ViewModel? SelectItem { get ; get; }

        public ObservableCollection<Album_ViewModel> SearchPreview { get; } = new(); 

        [RelayCommand]
        private async Task StoreViewModel_OpenAlbumWindow_Async()
        {
            // Code here will be executed when the buttom being pressed. Re-emerge test. 
            var tmp = await WeakReferenceMessenger.Default.Send(new Messages_OpenTextWindow());
        }

        public ClassStoreViewModel()
        {
            SearchPreview.Add(new Album_ViewModel());
            SearchPreview.Add(new Album_ViewModel());
            SearchPreview.Add(new Album_ViewModel());
        }
    }
    
    
}