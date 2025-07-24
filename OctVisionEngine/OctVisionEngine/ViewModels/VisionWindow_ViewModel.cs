using System;
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
        _messenger.Register<ProcessedDataReadyMessage>(this, (r, m) =>
        {
            // UI更新必须在UI线程（就像只有特定工人能操作显示屏）
            Dispatcher.UIThread.Post(() => UpdateImage(m.Value));
        });
        
        _messenger.Register<FileLoadingStatusMessage>(this, (r, m) =>
        {
            Dispatcher.UIThread.Post(() => StatusText = m.Value);
        });
        
        _messenger.Register<ProcessingProgressMessage>(this, (r, m) =>
        {
            Dispatcher.UIThread.Post(() => Progress = m.Value);
        });
    }

    /// <summary>
    /// 选择文件命令
    /// </summary>
    [RelayCommand]
    private async Task SelectFileAsync()
    {
        // 这里简化处理，实际应该使用文件对话框
        // 在实际应用中，你需要使用 Avalonia 的文件对话框
        SelectedFilePath = "J:/Data_2025/20250326_Jurkat4/Day0_Control_Pos1(bottom)/Data.bin";
        StatusText = "已选择文件";
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
        
        IsProcessing = true;
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
        
        using (var lockedBitmap = CurrentImage.Lock())
        {
            unsafe
            {
                var ptr = (byte*)lockedBitmap.Address;
                var stride = lockedBitmap.RowBytes;
                
                // 并行复制数据以提高性能
                Parallel.For(0, height, y =>
                {
                    var rowPtr = ptr + y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        rowPtr[x] = data[y, x];
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