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
using Avalonia;
using Avalonia.Platform;


namespace OctVisionEngine.ViewModels;

public partial class Debug_ImageWindowViewModel : ObservableObject // INotifyPropertyChanged
{
    private readonly Debug_LoadFramesFromBin _imageReader;
    private CancellationTokenSource _cts;
    private Channel<float[,,]> _broadcastChannel;
    private float[,]? _enfaceArray;
    private float[,,]? _enfaceHsvArray;
    private WriteableBitmap? _enfaceBitmap;
    private int _currentRow = 0;
    // private readonly SemaphoreSlim _pauseSemaphore = new(1, 1);
    // 以下为手动实现CommunityToolkit.Mvvm的[ObservableProperty]的功能. 包括:
    // 1) 内部用的字段_imagePanelDebug和外部调用的ImagePanelDebug相互隔离, 并使用get set方法互通;
    // 2) 针对set, 自动实现INotifyPropertyChanged(), 一但值变动及时通知View层更新.
    // 关于类/方法的partial声明: 当该函数中使用了来自其他源的方法, 需要声明除了自己在的代码范围, 这个类还同时使用了来自其他源的方法
    // private WriteableBitmap _bscanLoaded;
    // public event PropertyChangedEventHandler PropertyChanged;
    //
    // public WriteableBitmap ImagePanel_Debug
    // {
    //     get => _bscanLoaded;
    //     set
    //     {
    //         _bscanLoaded = value;
    //         OnPropertyChanged();
    //     }
    // }
    // protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    // { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
    [ObservableProperty] private bool _isFileSelected = false;
    [ObservableProperty] private string _selectedFilePath = string.Empty;
    [ObservableProperty] private WriteableBitmap? _bscanLoaded;
    [ObservableProperty] private WriteableBitmap? _enfaceImage;
    [ObservableProperty] private bool _isProcessing = false;
    [ObservableProperty] private bool _isPaused = false;
    [ObservableProperty] private int _rasterNum = 1;
    [ObservableProperty] private int _sampNumX = 256;
    [ObservableProperty] private int _sampNumY;

    public Debug_ImageWindowViewModel()
    {
        _imageReader = new Debug_LoadFramesFromBin();
        _cts = new CancellationTokenSource();
        SampNumY = SampNumX;
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
    private async Task LoadFramesAndUpdateDisplayAsync()
    {
        IsProcessing = true;
        try
        {
            _broadcastChannel = Channel.CreateBounded<float[,,]>(new BoundedChannelOptions(10)
            { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });  // 当false时允许多个读取者
            // 当 SingleReader 设置为 false 时，意味着多个线程可能会同时尝试从通道中读取数据。为了避免出现竞态条件（race conditions），
            // 通道的内部实现必须引入线程同步机制。例如，当一个线程正在从通道中读取数据时，其他线程必须被阻塞或等待，直到读取操作完成。
            // 这些额外的同步开销（如加锁和解锁）会降低读取操作的效率.
            // var bscanDisplayTask = UpdateBscanWithLoadedFramesAsync(_broadcastChannel.Reader);
            // var enfaceDisplayTask = UpdateEnfaceWithLoadedFramesAsync(_broadcastChannel.Reader);
            var updateDisplayTask = UpdateDisplayWithLoadedFramesAsync(_broadcastChannel.Reader);
            var loadTask = LoadFramesAsync(_broadcastChannel.Writer);
            await Task.WhenAll(loadTask, updateDisplayTask);
        }
        catch (OperationCanceledException)
        { Console.WriteLine("加载操作已取消。"); }
        catch (Exception e)
        { Console.WriteLine($"加载图像失败: {e.Message}"); }
        finally
        { IsProcessing = false; }
    }


    private async Task LoadFramesAsync(ChannelWriter<float[,,]> writer)
    {
        try
        {
            await foreach (var floatData3D in _imageReader.LoadFramesSequenceFromBinAsync(SelectedFilePath, RasterNum, _cts.Token))
            {
                while (IsPaused)
                { await Task.Delay(300, _cts.Token); }
                await writer.WriteAsync(floatData3D, _cts.Token);
            }
        }
        finally
        { writer.Complete(); }
    }



    private async Task UpdateDisplayWithLoadedFramesAsync(ChannelReader<float[,,]> reader)
    {
        try
        {
            await foreach (var blockAs3dArray in reader.ReadAllAsync(_cts.Token))
            {
                if (RasterNum == 1)
                {
                    var (bscanBitmap, enfaceBitmap) = await Task.Run(async () =>
                    {
                        // 任务1：计算B-scan位图，与任务2并行
                        var bscanArray = blockAs3dArray.To2DArray();
                        var bscanTask = _imageReader.ConvertFloat2dArrayToGrayAsync(bscanArray);
                        // 任务2：计算En-face位图，与任务1并行
                        var projection1dArray = BscanProjection.MaxProjectionSpan(bscanArray, 0);
                        // UpdateEnfaceArray(projection1dArray); // 同步封装，无额外开销
                        BscanProjectionUpdater.UpdateEnfaceArray(projection1dArray, ref _enfaceArray, ref _currentRow, SampNumY, SampNumX);
                        var enfaceTask = _imageReader.ConvertFloat2dArrayToGrayAsync(_enfaceArray);

                        await Task.WhenAll(bscanTask, enfaceTask);
                        return (bscanTask.Result, enfaceTask.Result);
                    }, _cts.Token);
                    BscanLoaded = bscanBitmap;
                    EnfaceImage = enfaceBitmap;
                }

                else if (RasterNum > 1)
                {
                    var (bscanBitmap, enfaceBitmap) = await Task.Run(async () =>
                    {
                        // 任务1：计算B-scan位图
                        var bscanTask = _imageReader.ConvertFloat3dArrayToRgbAsync(blockAs3dArray);
                        // 任务2：计算En-face位图。它依赖于一个前置的同步计算。
                        var hsvArray = _imageReader.ConvertFloat3dArrayToHsv(blockAs3dArray); // 同步计算
                        var projectionHsvArray = BscanProjection.MaxHueProjectionSpan(hsvArray, 0);
                        // UpdateEnfaceHsvArray(projectionHsvArray); // 同步封装，无额外开销
                        BscanProjectionUpdater.UpdateEnfaceHsvArray(projectionHsvArray, ref _enfaceHsvArray, ref _currentRow, SampNumY, SampNumX);
                        var enfaceTask = _imageReader.ConvertFloat3dArrayToRgbAsync(_enfaceHsvArray); // 异步转换

                        await Task.WhenAll(bscanTask, enfaceTask);
                        return (bscanTask.Result, enfaceTask.Result);
                    }, _cts.Token);
                    BscanLoaded = bscanBitmap;
                    EnfaceImage = enfaceBitmap;
                }
            }
        }
        catch (ChannelClosedException) { }
    }



    [RelayCommand]
    private void PauseResume()
    { IsPaused = !IsPaused; }

    [RelayCommand]
    private void StopGrabFrame()
    { WeakReferenceMessenger.Default.Send(new StopGrabFrameMessage()); }

    private void HandleStopGrabFrame(StopGrabFrameMessage message)
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();
        _broadcastChannel?.Writer.Complete();
        IsProcessing = false;
        IsPaused = false;
        // if (_pauseSemaphore.CurrentCount == 0) { _pauseSemaphore.Release(); }
    }


}
