using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OctVisionEngine.Models;

namespace UnitTestTool;

public partial class CodeTest_UnitTestWindow : Window
{
    private HighThroughputProcessor _processor;
    
    public CodeTest_UnitTestWindow()
    {
        InitializeComponent();
        // 假设你的XAML中有这些控件
        var statusText = this.FindControl<TextBlock>("StatusText");
        var dataList = this.FindControl<ListBox>("DataList");
        
        _processor = new HighThroughputProcessor(statusText, dataList);
    }
    
    
    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        await _processor.StartAsync();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _processor.Stop();
    }
    
}