using System;
using Avalonia.Controls;
using OctVisionEngine.ViewModels;


namespace OctVisionEngine.Views;

public partial class VisionWindow_View : Window
{
    public VisionWindow_View()
    {
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // 清理资源
        if (DataContext is VisionWindow_ViewModel vm)
        {
            vm.Cleanup();
        }
    }

}
