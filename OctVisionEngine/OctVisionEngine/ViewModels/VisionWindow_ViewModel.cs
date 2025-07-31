using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OctVisionEngine.Messages;
using OctVisionEngine.Models;
using Avalonia.Threading;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Remote.Protocol.Viewport;
using PixelFormat = Avalonia.Platform.PixelFormat;


namespace OctVisionEngine.ViewModels;

/// <summary>
/// 主窗口视图模型 - 控制中心，协调各个部分
/// </summary>
public partial class VisionWindow_ViewModel : ObservableObject
{
    private readonly OctDataProcessor _processor;
    private readonly IMessenger _messenger;
    
    [ObservableProperty]
    private WriteableBitmap _currentImage;
    
    [ObservableProperty]
    private string _statusText = "就绪";
    
    [ObservableProperty]
    private double _progress;
    
    [ObservableProperty]
    private bool _isProcessing;
    
    [ObservableProperty]
    private string _selectedFilePath;
    
    [ObservableProperty]
    private int _frameCount;
    
    [ObservableProperty]
    private double _fps;
    
    private DateTime _lastFrameTime = DateTime.Now;
    private int _frameCounter;

    public VisionWindow_ViewModel()
    {
        _processor = new OctDataProcessor();
        _messenger = WeakReferenceMessenger.Default;
        
        // 初始化图像（256x700的灰度图）
        CurrentImage = new WriteableBitmap(
            new PixelSize(700, 256),
            new Vector(96, 96), 
            PixelFormats.Gray8); // PixelFormats.Gray8
        
        // 注册消息处理（就像订阅快递通知）
        RegisterMessages();
    }

    private void RegisterMessages()
    {
        // UI更新必须在UI线程（就像只有特定工人能操作显示屏）
        _messenger.Register<ProcessedDataReadyMessage>(this, (r, m) =>
        { Dispatcher.UIThread.Post(() => UpdateImage(m.Value)); });
        
        _messenger.Register<FileLoadingStatusMessage>(this, (r, m) =>
        { Dispatcher.UIThread.Post(() => StatusText = m.Value); });
        
        _messenger.Register<ProcessingProgressMessage>(this, (r, m) =>
        { Dispatcher.UIThread.Post(() => Progress = m.Value); });
    }

    /// <summary>
    /// 选择文件命令
    /// </summary>
    [RelayCommand]
    private async Task SelectFileAsync()
    {
        // 先检查MainWindow是否存在
        if (App.MainWindowHandler == null)
        {
            StatusText = "窗口未初始化，请稍后再试";
            return;
        }
        var dialog = new OpenFileDialog
        {
            Title = "选择数据文件",
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "二进制文件", Extensions = { "bin" } },
                new FileDialogFilter { Name = "所有文件", Extensions = { "*" } }
            }
        };

        // 这里需要获取主窗口引用
        try
        {
            var result = await dialog.ShowAsync(App.MainWindowHandler); // 需要传入父窗口

            if (result != null && result.Length > 0)
            {
                SelectedFilePath = result[0];
                StatusText = $"已选择: {System.IO.Path.GetFileName(result[0])}";
                IsProcessing = false;
                // !string.IsNullOrEmpty(SelectedFilePath)} 已经是true了
                StartProcessingCommand.NotifyCanExecuteChanged();
            }
            else
            {
                StatusText = "未选择文件";
            }
        }
        catch (Exception exception)
        {
            StatusText = $"文件选择出错: {exception.Message}";
        }
    }

    /// <summary>
    /// 开始处理命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStartProcessing))]
    private async Task StartProcessingAsync()
    {
        if (string.IsNullOrEmpty(SelectedFilePath))
        {
            StatusText = "请先选择文件";
            return;
        }
        // Console.WriteLine($"process异步被触发");
        IsProcessing = true;
        StopProcessingCommand.NotifyCanExecuteChanged();
        Progress = 0;
        FrameCount = 0;
        _frameCounter = 0;
        StatusText = "正在处理...";
        
        try
        {
            await _processor.StartProcessingAsync(SelectedFilePath);
            StatusText = "处理完成";
        }
        catch (Exception ex)
        {
            StatusText = $"处理失败: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private bool CanStartProcessing() => !IsProcessing && !string.IsNullOrEmpty(SelectedFilePath);

    /// <summary>
    /// 停止处理命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStopProcessing))]
    private void StopProcessing()
    {
        _processor.StopProcessing();
        StatusText = "已停止";
        IsProcessing = false;
    }

    private bool CanStopProcessing() => IsProcessing;

    /// <summary>
    /// 更新图像显示
    /// </summary>
    private void UpdateImage(byte[,] data)
    {
        var height = data.GetLength(0);
        var width = data.GetLength(1);
        // 如果CurrentImage尺寸不对，重新创建
        if (CurrentImage == null ||
            CurrentImage.PixelSize.Width != width ||
            CurrentImage.PixelSize.Height != height)
        {
            // 🔧 修复2：使用BGRA8888格式，与成功代码保持一致
            CurrentImage = new WriteableBitmap(
                new PixelSize(width, height),
                new Vector(96, 96),
                PixelFormat.Bgra8888,  // 改为BGRA8888
                AlphaFormat.Opaque);
        }
        
        using (var lockedBitmap = CurrentImage.Lock())
        {
            unsafe
            {
                // 🔧 修复3：按照BGRA格式处理像素
                uint* pixelPtr = (uint*)lockedBitmap.Address;
                int stride = lockedBitmap.RowBytes / 4; // uint步长

                Parallel.For(0, height, y =>
                {
                    uint* rowPtr = pixelPtr + y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        byte gray = data[y, x];
                        // 🔧 修复4：使用与成功代码相同的像素格式
                        uint grayPixel = 0xFF000000u | ((uint)gray << 16) | ((uint)gray << 8) | gray;
                        rowPtr[x] = grayPixel;
                    }
                });
            }
        }
        
        // 更新帧计数和FPS
        FrameCount++;
        _frameCounter++;
        
        var now = DateTime.Now;
        var elapsed = (now - _lastFrameTime).TotalSeconds;
        if (elapsed >= 1.0) // 每秒更新一次FPS
        {
            Fps = _frameCounter / elapsed;
            _frameCounter = 0;
            _lastFrameTime = now;
        }
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Cleanup()
    {
        _messenger.UnregisterAll(this);
        _processor.StopProcessing();
    }
}