using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Messaging;
using OctVisionEngine.Messages;
using Tmds.DBus.Protocol;
using OctVisionEngine.Models;
// using Avalonia.Controls;
// using Avalonia.Controls.ApplicationLifetimes;

namespace OctVisionEngine.ViewModels
{
    public partial class MainWindow_ViewModel : ObservableObject
    {
        public MainWindow_ViewModel()
        {
            // ViewModel initialization logic.
            LoadAlbums();
            LoadImageCommand = new RelayCommand(LoadImage);
        }
        
        private double _input;
        private double _result; 
        [ObservableProperty] private string? _imagePath;
        [ObservableProperty] private Bitmap? _displayImage;

        public ObservableCollection<Album_ViewModel> Albums { get; } = new();
        
        public IRelayCommand LoadImageCommand { get; }

        private void LoadImage()
        {
            var testFigPath = @"C:\Users\lzhu\Desktop\Fig1.png";
            if (File.Exists(testFigPath))
            {
                DisplayImage = new Bitmap(testFigPath);
                Console.WriteLine("✅ Image loaded successfully.");
            }
        }
        
        // MainWindow中的'Store按钮'一但被点击，就执行MainWindow_ViewModel.cs中的Command_OpenStoreWindowAfterPurchase_Async()。该命令执行以下两个操作：
        // 1) [异步]new了一个Message_PurchaseToOpenStorePage()并广播; 
        //      - 1.1) ...
        //      - 1.2) ...
        // 2) 一但收到消息的回应，且回应带回的变量(album)且不为空，就将其添加到自己的public属性'Albums'中。
        // 
        // 同时，MainWindow_View.axaml.cs里对本窗口也注册了同一个消息Message_PurchaseToOpenStorePage()，一但侦测到消息广播时，执行以下两个操作：
        // 1.1) new一个Store_Window_View的类(即新开一个Store_Window窗口);
        //      - 这个新开的Store_Window中的内容(DataContext)用的是Store_PreviewPanel_ViewModel()的贴膜. 
        //      而且这里'Store_Window_View' 是作为对话框打开的，在Avalonia中当一个窗口作为对话框打开时,
        //      close()方法可以接受一个返回值参数（详见Store_Window_View.axaml.cs），并通过.ShowDialog<>返回.
        //      在这里，当这个对话框受到“点击Purchase按钮”影响而被关闭时: button > BuyMusic() > Message_CloseStoreWindow() > Store_Window_View中的Window.Close(message.SelectedAlbum)
        //      接受到的返回值为来自Store_window_View.axaml.cs的massage.SelectedAlbum => album.
        // 1.2) 调用ShowDialog<Album_ViewModel?>(w)来显示模态对话框（这是什么操作？？？？）， 并以此回复消息。
        //      - 这个消息回复会马上触发(2)，从而马上导致回复内容（模态对话框？）被添加到MainWindow_ViewModel的自带属性'Albums'中。
        // 总结：点击'Store按钮'，
        [RelayCommand] 
        private async Task Command_OpenStoreWindowAfterPurchase_Async()
        {
            var album = await WeakReferenceMessenger.Default.Send(new Message_PurchaseToOpenStorePage());
            if (album != null)
            {
                Albums.Add(album);
                await album.SaveToDiskAsync();
            }
        }

        // [RelayCommand]
        // private async Task AddAlbumAsync()
        // {
        //     var album = await WeakReferenceMessenger.Default.Send(new Message_PurchaseToOpenStorePage());
        //     if (album != null)
        //     {
        //         Albums.Add(album);
        //         await album.SaveToDiskAsync();
        //     }
        // }

        private async void LoadAlbums()
        {
            var albums = (await Album.LoadCachedAsync()).Select(x => new Album_ViewModel(x)).ToList();
            foreach (var album in albums)
            { Albums.Add(album); }

            var coverTasks = albums.Select(album => album.LoadCover());
            await Task.WhenAll(coverTasks);
        }
    }
}
