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
        { var tmp = await WeakReferenceMessenger.Default.Send(new Messages_OpenTextWindow()); }

        //一但点击'Purchase按钮'，绑定执行BuyMusic(),这里只有一个操作，即广播Message_CloseStoreWindow（）。
        //由于Store_PreviewPanel_View是被贴膜到Store_Window中的，所以在Store_Window_View.axaml.cs中把该消息注册给了本窗口，
        //一但侦测到消息广播，
        [RelayCommand]
        private void BuyMusic()
        {
            if (SelectedAlbum != null)
            {
                WeakReferenceMessenger.Default.Send(new Message_CloseStoreWindow(SelectedAlbum));
            }
        }
        
        private async Task DoSearch(string? term)
        {
            _cancellationTokenSource?.Cancel(); //取消之前标记为'_cancellationTokenSource'的异步操作(如果有的话)
            _cancellationTokenSource = new CancellationTokenSource();//创建新的取消标记(重置)
            var cancellationToken = _cancellationTokenSource.Token;//
            IsBusy = true;
            
            SearchListUpdate_event.Clear();
            var albums = await Album.SearchAsync(term);//新的异步操作
            foreach (var album in albums)
            {
                var vm = new Album_ViewModel(album);
                SearchListUpdate_event.Add(vm);
            }

            if (!cancellationToken.IsCancellationRequested)
            { LoadCovers(cancellationToken); }
            
            IsBusy = false;
        }
        
        partial void OnSearchTextChanged(string? value)
        { _ = DoSearch(SearchText); }

        private async void LoadCovers(CancellationToken cancellationToken)
        {
            foreach (var album in SearchListUpdate_event.ToList())
            {
                await album.LoadCover();
                if (cancellationToken.IsCancellationRequested)
                { return; }
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