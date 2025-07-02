using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Messaging;
using OctVisionEngine.Messages;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace OctVisionEngine.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private double _input;
        private double _result; 
        
        [ObservableProperty] 
        private string? _imagePath;
        
        [ObservableProperty] 
        private Bitmap? _displayImage;
        
        public MainWindowViewModel()
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

        [RelayCommand]
        private async Task Command_OpenStoreWindow_Async()
        {
            // Code here will be executed when the buttom being pressed. Re-emerge test. 
            // GetInput();
            // CalToOutput();
            var album = await WeakReferenceMessenger.Default.Send(new Message_OpenStorePage());
        }

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
        //         ? desktop.MainWindow
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
