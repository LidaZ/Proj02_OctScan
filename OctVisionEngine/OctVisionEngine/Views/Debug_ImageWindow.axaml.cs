using Avalonia;
using Avalonia.Controls;
using System;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using OctVisionEngine.Models;
using System.Runtime.InteropServices;
using System.Buffers;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OctVisionEngine.ViewModels;


namespace OctVisionEngine.Views;

public partial class Debug_ImageWindow : Window
{
    public Debug_ImageWindow()
    {
        InitializeComponent();
        DataContext = new Debug_ImageWindowViewModel();
        SetupExitButton();
    }

    private void SetupExitButton()
    {
        var exitButton = this.Find<Button>("ExitButton");
        if (exitButton != null)
        {
            exitButton.Click += (sender, e) =>
            {
                Close(); // 关闭窗口
                Environment.Exit(0); // 强制退出程序（调试时用）
            };
        }
    }

}