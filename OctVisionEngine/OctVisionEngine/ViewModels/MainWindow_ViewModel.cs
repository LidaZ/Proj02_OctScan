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

        // 这里的逻辑是在Store_Window中的“Store_Preview贴膜”中选择一个搜索结果并点击Button后，new了一个Message_CloseStoreWindow('selectedAlbum')并广播
        // 1)在new的Message_CloseStoreWindow()中，引用了Album_ViewModel的同名类(强制初始化，把'selectedAlbum'传给自带的私密字段_album
        // (那这里每次发消息都是new，意思是每次发消息都新建一个Album_ViewModel._album <= 'selectedAlbum'?)
        // 2)同时，Store_Window本身的axaml.cs中对本窗口注册了Message_CloseStoreWindow()，一但收到消息广播后，触发功能：关闭本窗口
        // 3)同时，MainWindow的View里也对本窗口注册了Message_PurchaseToOpenStorePage()，一但点击'Store按钮'就new了一个Store_PreviewPanel_ViewModel的类（？）
        [RelayCommand] 
        private async Task Command_OpenStoreWindowAfterPurchase_Async()
        {
            var album = await WeakReferenceMessenger.Default.Send(new Message_PurchaseToOpenStorePage()); 
            if (album != null)
            {Albums.Add(album);}
        }

        
    }
}
