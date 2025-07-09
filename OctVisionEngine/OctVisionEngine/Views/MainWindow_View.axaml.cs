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
        WeakReferenceMessenger.Default.Register<MainWindow_View, Message_PurchaseToOpenStorePage>
            (this, static (w, m) =>
                { 
                    var dialog = new Store_Window_View { DataContext = new Store_PreviewPanel_ViewModel() };
                    // 'Store_Window_View' 是UI界面，'Store_PreviewPanel_ViewModel()'是数据、逻辑处理界面，
                    // 通过声明'DataContext'把这俩绑在一起，告诉UI该从哪个数据/逻辑处理集合中调取变量。
                    // 而且这里'Store_Window_View' 是作为对话框打开的，在Avalonia中当一个窗口作为对话框打开时,
                    // close()方法可以接受一个返回值参数（详见Store_Window_View.axaml.cs），并通过.ShowDialog<>返回.
                    // 在这里，当这个对话框受到“点击Purchase按钮”影响而被关闭时: button > BuyMusic() > Message_CloseStoreWindow() > Store_Window_View中的Window.Close(message.SelectedAlbum)
                    // 接受到的返回值为来自Store_window_View.axaml.cs的massage.SelectedAlbum => album.
                    m.Reply(dialog.ShowDialog<Album_ViewModel?>(w));
                    // 期望返回 Album_ViewModel? 这句话是干嘛用的？
                }
            );
        // WeakReferenceMessenger.Default.Register<MainWindow_View, Message_CloseStoreWindow>
        // (this, static (Window, Message) => { Window.Close(Message.SelectedAlbum); }
        // );
    }
}