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
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = false  // 允许多个读取者
            });

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
                var bscanArray = blockAs3dArray.To2DArray();

                var (bscanBitmap, enfaceBitmap) = await Task.Run(async () =>
                {
                    var bscanTask = _imageReader.ConvertFloat2dArrayToGrayAsync(bscanArray);
                    var projectionData = BscanProjection.MaxProjectionSpan(bscanArray, 0);
                    if (_enfaceArray == null || _enfaceArray.GetLength(0) != SampNumY || _enfaceArray.GetLength(1) != SampNumX)
                    {
                        _enfaceArray = new float[SampNumY, SampNumX];
                        _currentRow = 0;
                    }
                    for (int y = 0; y < projectionData.Length; y++)
                    { _enfaceArray[_currentRow, y] = projectionData[y]; }
                    _currentRow = (_currentRow + 1) % SampNumY;
                    var enfaceTask = _imageReader.ConvertFloat2dArrayToGrayAsync(_enfaceArray);
                    await Task.WhenAll(bscanTask, enfaceTask);
                    return (bscanTask.Result, enfaceTask.Result);
                }, _cts.Token);
                BscanLoaded = bscanBitmap;
                EnfaceImage = enfaceBitmap;
            }
            else if (RasterNum > 1)
            {
                var (bscanBitmap, enfaceHsvArray) = await Task.Run(() =>
                {
                    var bscanTask = _imageReader.ConvertFloat3dArrayToRgbAsync(blockAs3dArray);
                    var hsvTask = _imageReader.ConvertFloat3dArrayToHsvAsync(blockAs3dArray);
                    Task.WaitAll(bscanTask, hsvTask);
                    return (bscanTask.Result, hsvTask.Result);
                }, _cts.Token);
                BscanLoaded = bscanBitmap;
                // // 使用enfaceHsvArray计算EnfaceImage
                // var projectionData = BscanProjection.MaxProjectionSpan(enfaceHsvArray, 0);
                // // 这里应该用 projectionData 更新 _enfaceBitmap，然后赋给 EnfaceImage, 也是用await
                // // ...
            }
        }
    }
    catch (ChannelClosedException) { }
}

    // private async Task UpdateBscanWithLoadedFramesAsync(ChannelReader<float[,,]> reader)
    // {
    //     try
    //     {
    //         await foreach (var blockAs3dArrayForBscanChan in reader.ReadAllAsync(_cts.Token))
    //         {
    //             if (RasterNum == 1)
    //             {
    //                 var bscanArray = blockAs3dArrayForBscanChan.To2DArray();
    //                 BscanLoaded = await _imageReader.ConvertFloat2dArrayToGrayAsync(bscanArray);
    //             }
    //             else if (RasterNum > 1)
    //             {
    //                 var bscanBitmap = await _imageReader.ConvertFloat3dArrayToRgbAsync(blockAs3dArrayForBscanChan);
    //                 BscanLoaded = bscanBitmap;
    //             }
    //         }
    //     }
    //     catch (ChannelClosedException) { } // 通道关闭，正常退出
    // }
    //
    // private async Task UpdateEnfaceWithLoadedFramesAsync(ChannelReader<float[,,]> reader)
    // {
    //     try
    //     {
    //         await foreach (var blockAs3dArrayForEnfaceChan in reader.ReadAllAsync(_cts.Token))
    //         {
    //             if (RasterNum == 1)
    //             {
    //                 var bscanArraySubChan = blockAs3dArrayForEnfaceChan.To2DArray();
    //                 var projectionData = BscanProjection.MaxProjectionSpan(bscanArraySubChan, 0);
    //                 // 检查是否需要重新分配数组（只有在尺寸变化时才分配）
    //                 if (_enfaceArray == null || _enfaceArray.GetLength(0) != SampNumY || _enfaceArray.GetLength(1) != SampNumX)
    //                 {
    //                     _enfaceArray = new float[SampNumY, SampNumX];
    //                     _currentRow = 0;
    //                 }
    //                 // 每一个抽出的Bscan的1D投影更新到_enfaceData中指定的col
    //                 for (int y = 0; y < projectionData.Length; y++)
    //                 { _enfaceArray[_currentRow, y] = projectionData[y]; }
    //                 _currentRow = (_currentRow + 1) % SampNumY;
    //                 EnfaceImage = await _imageReader.ConvertFloat2dArrayToGrayAsync(_enfaceArray);
    //             }
    //             else if (RasterNum > 1)
    //             {
    //                 var hsvArray = await _imageReader.ConvertFloat3dArrayToHsvAsync(blockAs3dArrayForEnfaceChan);
    //                 var projectionData = BscanProjection.MaxProjectionSpan(hsvArray, 0);
    //                 // if (_enfaceBitmap == null || _enfaceBitmap.PixelSize.Width != SampNumY || _enfaceBitmap.PixelSize.Height != SampNumX)
    //                 // {
    //                 //     _enfaceBitmap = new WriteableBitmap(new PixelSize(SampNumY, SampNumX), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
    //                 //     _currentRow = 0;
    //                 // }
    //                 // for (int y = 0; y < SampNumY; y++)
    //                 // { _enfaceBitmap[_currentRow, y] = projectionData[y]; }
    //                 // _currentRow = (_currentRow + 1) % SampNumY;
    //                 //
    //                 // EnfaceImage = null;
    //             }
    //         }
    //     }
    //     catch (ChannelClosedException) { }
    // }

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
