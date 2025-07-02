using System;
using System.Linq;
using System.Threading;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using OctVisionEngine.Models;
using OctVisionEngine.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
// using iTunesSearch.Library.Models;


namespace OctVisionEngine.ViewModels
{
    public partial class StoreViewModel_Class : ViewModelBase
    {
        // StoreViewModel : ObservableObject
        [ObservableProperty]
        public partial string? SearchText { get; set; }
        
        [ObservableProperty] 
        public partial bool IsBusy { get; private set; }

        [ObservableProperty] 
        public partial Album_ViewModel? SelectedAlbum { get; set; }

        public ObservableCollection<Album_ViewModel> SearchListUpdate_event { get; } = new(); 

        [RelayCommand]
        private async Task StoreViewModel_OpenAlbumWindow_Async()
        {
            // Code here will be executed when the buttom being pressed. Re-emerge test. 
            var tmp = await WeakReferenceMessenger.Default.Send(new Messages_OpenTextWindow());
        }

        private async Task DoSearch(string? term)
        {
            IsBusy = true;
            SearchListUpdate_event.Clear();

            var albums = await Album.SearchAsync(term);

            foreach (var album in albums)
            {
                var vm = new Album_ViewModel(album);
                SearchListUpdate_event.Add(vm);
            }

            IsBusy = false;
        }
        
        partial void OnSearchTextChanged(string? value)
        {
            _ = DoSearch(SearchText);
        }
        
        // public StoreViewModel_Class()
        // {
        //     SearchListUpdate_event.Add(new Album_ViewModel());
        //     SearchListUpdate_event.Add(new Album_ViewModel());
        //     SearchListUpdate_event.Add(new Album_ViewModel());
        // }
    }
    
}