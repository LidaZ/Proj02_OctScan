using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using OctVisionEngine.Messages;
using OctVisionEngine.ViewModels;
using Avalonia.Media.Imaging;
using System.IO;



namespace OctVisionEngine.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // var testFigPath = @"C:\Users\lzhu\Desktop\Fig1.png";
        // if (File.Exists(testFigPath))
        // {
        //     ImageBox.Source = new Bitmap(testFigPath);
        // }
        if (Design.IsDesignMode)
            return;
        WeakReferenceMessenger.Default.Register<MainWindow, TestCode_OpenStorePage>(this, static (w, m) =>
        {
            var dialog = new StoreWindow
            {
                DataContext = new StoreViewModel()
            };
            m.Reply(dialog.ShowDialog<Album_ViewModel?>(w));
        });
    }
}