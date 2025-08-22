using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging;
using OctVisionEngine.Messages;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OctVisionEngine.Models;
using System.Threading.Channels;


namespace OctVisionEngine.ViewModels;

public partial class Debug_ImageWindowViewModel : ObservableObject // INotifyPropertyChanged
{
    private readonly Debug_ImageRead _imageReader;
    private CancellationTokenSource _cts;
    // private readonly SemaphoreSlim _pauseSemaphore = new(1, 1);
    // 以下为手动实现CommunityToolkit.Mvvm的[ObservableProperty]的功能. 包括:
    // 1) 内部用的字段_imagePanelDebug和外部调用的ImagePanelDebug相互隔离, 并使用get set方法互通;
    // 2) 针对set, 自动实现INotifyPropertyChanged(), 一但值变动及时通知View层更新.
    // 关于类/方法的partial声明: 当该函数中使用了来自其他源的方法, 需要声明除了自己在的代码范围, 这个类还同时使用了来自其他源的方法
    // private WriteableBitmap _imagePanelDebug;
    // public event PropertyChangedEventHandler PropertyChanged;
    //
    // public WriteableBitmap ImagePanel_Debug
    // {
    //     get => _imagePanelDebug;
    //     set
    //     {
    //         _imagePanelDebug = value;
    //         OnPropertyChanged();
    //     }
    // }
    // protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    // { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
    [ObservableProperty] private bool _isFileSelected = false;
    [ObservableProperty] private string _selectedFilePath = string.Empty;
    [ObservableProperty] private WriteableBitmap? _imagePanelDebug;
    [ObservableProperty] private bool _isProcessing = false;
    [ObservableProperty] private bool _isPaused = false;
    [ObservableProperty] private int _rasterNum = 1;
    [ObservableProperty] private int _sampNum = 256;

    public Debug_ImageWindowViewModel()
    {
        _imageReader = new Debug_ImageRead();
        _cts = new CancellationTokenSource();
        // _ = LoadFramesContinuouslyCommand.ExecuteAsync(null);
        WeakReferenceMessenger.Default.Register<StopGrabFrameMessage>
            (this, (recipient, message) => HandleStopGrabFrame(message));
    }


    [RelayCommand]
    private async Task SelectFileAsync()
    {
        var storageProvider = App.MainWindowHandler?.StorageProvider;
        if (storageProvider == null) return;
        var options = new FilePickerOpenOptions
        {
            Title = "Select binary raw data",
            FileTypeFilter = new[]
            { new FilePickerFileType("Binary raw data") { Patterns = new[] { "*.bin" } } }
        };
        try
        {
            var selectedFile = await storageProvider.OpenFilePickerAsync(options);
            if (selectedFile.Count > 0 && selectedFile[0].TryGetLocalPath() is { } filePath)
            {
                SelectedFilePath = filePath;
                IsFileSelected = true;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            IsFileSelected = false;
            throw;
        }
    }


    
    [RelayCommand]
    private async Task LoadFramesAndDisplayUpdateAsync()
    {
        IsProcessing = true;
        try
        {
            // 创建容量为5的有界Channel，避免消耗过多内存
            var channel = Channel.CreateBounded<WriteableBitmap>(new BoundedChannelOptions(5)
            { FullMode = BoundedChannelFullMode.DropOldest });
            var displayTask = UpdateBscanWithLoadedFramesAsync(channel.Reader); // 启动消费者任务
            await LoadFramesAsync(channel.Writer); // 生产者：读取图像
            await displayTask;// 等待显示任务完成
        }
        catch (OperationCanceledException)
        { Console.WriteLine("加载操作已取消。"); }
        catch (Exception e)
        { Console.WriteLine($"加载图像失败: {e.Message}"); }
        finally
        { IsProcessing = false; }
    }

    private async Task LoadFramesAsync(ChannelWriter<WriteableBitmap> writer)
    {
        try
        {
            await foreach (var bitmap in _imageReader.LoadFramesSequenceFromBinAsync(SelectedFilePath, RasterNum, _cts.Token))
            {
                while (IsPaused)
                { await Task.Delay(300, _cts.Token); }
                await writer.WriteAsync(bitmap, _cts.Token);
            }
        }
        finally
        { writer.Complete(); }
    }

    private async Task UpdateBscanWithLoadedFramesAsync(ChannelReader<WriteableBitmap> reader)
    {
        try
        {
            await foreach (var bitmap in reader.ReadAllAsync(_cts.Token))
            { ImagePanelDebug = bitmap; }
        }
        catch (ChannelClosedException)
        { } // 通道关闭，正常退出
    }



    [RelayCommand]
    private void PauseResume()
    { IsPaused = !IsPaused; }

    private void HandleStopGrabFrame(StopGrabFrameMessage message)
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();
        IsProcessing = false;
        IsPaused = false;
        // if (_pauseSemaphore.CurrentCount == 0) { _pauseSemaphore.Release(); }
    }

    [RelayCommand]
    private void StopGrabFrame()
    { WeakReferenceMessenger.Default.Send(new StopGrabFrameMessage()); }


}
