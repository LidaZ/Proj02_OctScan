using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OctVisionEngine.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using OctVisionEngine.Messages;


namespace OctVisionEngine.Views;

public partial class StoreWindow : Window
{
    public StoreWindow()
    {
        InitializeComponent();
        
        if (Design.IsDesignMode)
            return;
        WeakReferenceMessenger.Default.Register<StoreWindow, Messages_OpenTextWindow>(this, static (w, m) =>
        {
            var dialogStoreWindow = new TextWindow()
            {
                DataContext = new TextWindow_ViewModel()
            };
            m.Reply(dialogStoreWindow.ShowDialog<TextWindow_ViewModel?>(w));
        });
    }
}