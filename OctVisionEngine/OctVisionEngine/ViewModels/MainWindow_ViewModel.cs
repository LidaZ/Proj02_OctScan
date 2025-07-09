using System;
using System.Collections.ObjectModel;
using System.IO;
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
        private double _input;
        private double _result; 
        
        [ObservableProperty] 
        private string? _imagePath;
        
        [ObservableProperty] 
        private Bitmap? _displayImage;

        public ObservableCollection<Album_ViewModel> Albums { get; } = new();
        
        public MainWindow_ViewModel()
        {
            // ViewModel initialization logic.
            LoadImageCommand = new RelayCommand(LoadImage);
        }
        
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

        // 这里的逻辑是在Store_Window中的“Store_Preview贴膜”中选择一个搜索结果并点击'Store'Button后，new了一个Message_CloseStoreWindow('selectedAlbum')并广播
        // 1)在new的Message_CloseStoreWindow()中，引用了Album_ViewModel的同名类(强制初始化，把'selectedAlbum'传给自带的私密字段_album
        // (那这里每次发消息都是new，意思是每次发消息都新建一个Album_ViewModel._album <= 'selectedAlbum'?)
        // 2)同时，Store_Window本身的axaml.cs中对本窗口注册了Message_CloseStoreWindow()，一但收到消息广播后，触发功能：关闭本窗口
        // MainWindow中的'Store按钮'一但被点击，就执行MainWindow_ViewModel.cs中的Command_OpenStoreWindowAfterPurchase_Async()。该命令执行以下两个操作：
        // 1) [异步]new了一个Message_CloseStoreWindow('selectedAlbum')并广播; | (那这里每次发消息都是new，意思是每次发消息都新建一个Album_ViewModel._album <= 'selectedAlbum'?)
        //      - 其中，在new的Message_CloseStoreWindow()中，引用了Album_ViewModel的同名类(强制初始化，把'selectedAlbum'传给自带的私密字段_album
        //
        
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
            {Albums.Add(album);}
        }

        
    }
}
