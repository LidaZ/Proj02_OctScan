using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using OctVisionEngine.Messages;
using OctVisionEngine.ViewModels;
using Tmds.DBus.Protocol;


namespace OctVisionEngine.Views;

public partial class MainWindow_View : Window
{
    public MainWindow_View()
    {
        InitializeComponent();

        // var testFigPath = @"C:\Users\lzhu\Desktop\Fig1.png";
        // if (File.Exists(testFigPath))
        // {
        //     ImageBox.Source = new Bitmap(testFigPath);
        // }
        if (Design.IsDesignMode)
            return;
        WeakReferenceMessenger.Default.Register<MainWindow_View, Message_OpenStorePage>
            (this, static (w, m) =>
                { var dialog = new Store_Window_View
                    { DataContext = new Store_PreviewPanel_ViewModel() };
                    m.Reply(dialog.ShowDialog<Album_ViewModel?>(w));
                }
            );
        // WeakReferenceMessenger.Default.Register<MainWindow_View, Message_CloseStoreWindow>
        // (this, static (Window, Message) => { Window.Close(Message.SelectedAlbum); }
        // );
    }
}