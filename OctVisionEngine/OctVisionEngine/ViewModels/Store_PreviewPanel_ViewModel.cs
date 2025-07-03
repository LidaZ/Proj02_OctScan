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
    public partial class Store_PreviewPanel_ViewModel : ViewModelBase
    {
        // StoreViewModel : ObservableObject
        private CancellationTokenSource? _cancellationTokenSource; 
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
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            
            IsBusy = true;
            SearchListUpdate_event.Clear();
            var albums = await Album.SearchAsync(term);
            foreach (var album in albums)
            {
                var vm = new Album_ViewModel(album);
                SearchListUpdate_event.Add(vm);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                LoadCovers(cancellationToken);
            }
            
            IsBusy = false;
        }
        
        partial void OnSearchTextChanged(string? value)
        {
            _ = DoSearch(SearchText);
        }

        private async void LoadCovers(CancellationToken cancellationToken)
        {
            foreach (var album in SearchListUpdate_event.ToList())
            {
                await album.LoadCover();
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }
        // public Store_PreviewPanel_ViewModel()
        // {
        //     SearchListUpdate_event.Add(new Album_ViewModel());
        //     SearchListUpdate_event.Add(new Album_ViewModel());
        //     SearchListUpdate_event.Add(new Album_ViewModel());
        // }
    }
    
}