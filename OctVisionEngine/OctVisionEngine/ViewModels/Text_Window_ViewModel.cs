using Avalonia.Platform;
using System;
using Avalonia;
using Avalonia.Media.Imaging;
using System.Threading.Tasks;
using OctVisionEngine.Messages;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;


namespace OctVisionEngine.ViewModels
{
    public partial class Text_Window_ViewModel : ObservableObject
    {
        [ObservableProperty] 
        private Bitmap? _enfaceLoad;
        [ObservableProperty] 
        private Bitmap? _bscaLoad;
        
        public Text_Window_ViewModel()
        {
            Interface_EnfaceImageCommand = new RelayCommand(GetEnfaceImage);
        }

        public IRelayCommand Interface_EnfaceImageCommand { get; }

        private void GetEnfaceImage()
        {

            try
            {
                // var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                var enfaceUri = AssetLoader.Open(new Uri($"avares://OctVisionEngine/Assets/Enface.png"));
                var bscanUri = AssetLoader.Open((new Uri($"avares://OctVisionEngine/Assets/Bscan.png")));
            
                EnfaceLoad = new Bitmap(enfaceUri);
                BscaLoad = new Bitmap(bscanUri);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载图片失败: {ex.Message}");
            }

        }

    }
}