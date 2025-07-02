using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OctVisionEngine.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using OctVisionEngine.Messages;


namespace OctVisionEngine.Views;

public partial class Store_Window_View : Window
{
    public Store_Window_View()
    {
        InitializeComponent();
        
        if (Design.IsDesignMode)
            return;
        WeakReferenceMessenger.Default.Register<Store_Window_View, Messages_OpenTextWindow>(this, static (w, m) =>
        {
            var dialogStoreWindow = new Text_Window_View()
            {
                DataContext = new Text_Window_ViewModel()
            };
            m.Reply(dialogStoreWindow.ShowDialog<Text_Window_ViewModel?>(w));
        });
    }
}