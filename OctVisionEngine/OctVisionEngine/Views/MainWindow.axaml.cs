using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using OctVisionEngine.Messages;
using OctVisionEngine.ViewModels;

namespace OctVisionEngine.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
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