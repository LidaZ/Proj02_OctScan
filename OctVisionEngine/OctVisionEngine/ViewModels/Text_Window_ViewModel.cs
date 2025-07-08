using Avalonia.Platform;
using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Styling; 
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OctVisionEngine.Extensions; // 添加这个引用



namespace OctVisionEngine.ViewModels
{
    
    public partial class Text_Window_ViewModel : ObservableObject
    {
        
        [ObservableProperty] 
        private Bitmap? _enfaceLoad;
        [ObservableProperty]
        private double _imageWidth;
        [ObservableProperty]
        private double _imageHeight;
        [ObservableProperty]
        private double _scanRangeValue = 1; // 初始值设为1
        [ObservableProperty]
        private string _scanRangeText = "Scan Range: 1 mm";
        [ObservableProperty]
        private int _splitFactor = 1; 

        [ObservableProperty] 
        private Bitmap? _bscaLoad;
        
        [ObservableProperty]
        private ObservableCollection<RectangleItem> _rectangleItems;

        
        public Text_Window_ViewModel()
        {
            Interface_EnfaceImageCommand = new RelayCommand(GetEnfaceImage);
            UpdateRectangles();
        }

        public IRelayCommand Interface_EnfaceImageCommand { get; }

        private void GetEnfaceImage()
        {
            try
            {
                var enfaceUri = AssetLoader.Open(new Uri($"avares://OctVisionEngine/Assets/Enface.png"));
                var bscanUri = AssetLoader.Open((new Uri($"avares://OctVisionEngine/Assets/Bscan.png")));
    
                EnfaceLoad = new Bitmap(enfaceUri);
                BscaLoad = new Bitmap(bscanUri);

                // 设置图片尺寸
                ImageWidth = EnfaceLoad.PixelSize.Width;
                ImageHeight = EnfaceLoad.PixelSize.Height;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载图片失败: {ex.Message}");
            }
        }

        
        partial void OnSplitFactorChanged(int value)
        {
            UpdateRectangles();
        }

        partial void OnScanRangeValueChanged(double value)
        {
            // 将滑块值四舍五入到最近的整数
            int step = (int)Math.Round(value);
            // 确保值在1-6之间
            step = Math.Max(1, Math.Min(6, step));
            // 更新显示文本
            ScanRangeText = $"Scan Range: {step} mm";
            // 根据步进值设置 SplitFactor
            SplitFactor = step; // 1->1, 2->4, 3->9, 4->16, 5->25, 6->36
        }

        // 添加一个用于表示每个矩形的类
        public class RectangleItem
        {
            public string Fill { get; set; } = "#00D4D4D4";
            public string Stroke { get; set; } = "#FF39BCE8";
            public double StrokeThickness { get; set; } = 1;
        }
        // 更新矩形集合的方法
        private void UpdateRectangles()
        {
            RectangleItems = new ObservableCollection<RectangleItem>();
            int totalRectangles = SplitFactor * SplitFactor;
        
            for (int i = 0; i < totalRectangles; i++)
            {
                RectangleItems.Add(new RectangleItem());
            }
        }

        // 添加一个修改分割因子的方法
        private void Rectangle_OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (sender is Rectangle rectangle)
            {
                rectangle.Classes.Toggle("selected");
            }
        }




    }
}