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

        [RelayCommand]
        private async Task AddAlbumAsync()
        {
            var album = await WeakReferenceMessenger.Default.Send(new Message_PurchaseToOpenStorePage());
            if (album != null)
            {Albums.Add(album);}
        }

        private void LoadImage()
        {
            var testFigPath = @"C:\Users\lzhu\Desktop\Fig1.png";
            if (File.Exists(testFigPath))
            {
                DisplayImage = new Bitmap(testFigPath);
                Console.WriteLine("✅ Image loaded successfully.");
            }
        }

        [RelayCommand]
        private async Task Command_OpenStoreWindow_Async()
        { var album = await WeakReferenceMessenger.Default.Send(new Message_PurchaseToOpenStorePage()); }

        // [RelayCommand]
        // private async Task LoadImagePopWindow_Async()
        // {
        //     var dialog = new OpenFileDialog
        //     {
        //         Title = "select an image", AllowMultiple = false, Filters =
        //         {
        //             new FileDialogFilter { Name = "Image Files" }
        //         }
        //     };
        //
        //     var window = App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
        //         ? desktop.MainWindow_View
        //         : null;
        //     
        //     if (window == null)
        //         return;
        //
        //     var result = await dialog.ShowAsync(window);
        //     if (result.Length > 0 && File.Exists(result[0]))
        //     {
        //         ImagePath = result[0];
        //         using var stream = File.OpenRead(ImagePath);
        //         DisplayImage = await Task.Run(() => Bitmap.DecodeToWidth(stream, 1024)); 
        //     }
        // }
    }
}
