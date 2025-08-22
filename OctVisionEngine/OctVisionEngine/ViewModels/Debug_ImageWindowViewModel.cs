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
    private readonly Debug_LoadFramesFromBin _imageReader;
    private CancellationTokenSource _cts;
    private readonly Queue<float[]> _enfaceBuffer = new Queue<float[]>();
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
    [ObservableProperty] private int _sampNum = 256;

    public Debug_ImageWindowViewModel()
    {
        _imageReader = new Debug_LoadFramesFromBin();
        _cts = new CancellationTokenSource();
        var MAX_ENFACE_FRAMES = SampNum;
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
            var mainChannel = Channel.CreateBounded<float[,,]>(new BoundedChannelOptions(5)
                { FullMode = BoundedChannelFullMode.DropOldest }); // 创建容量为5的有界Channel，避免消耗过多内存
            var secondChannel = Channel.CreateBounded<float[,,]>(new BoundedChannelOptions(5)
                { FullMode = BoundedChannelFullMode.DropOldest });
            var distributeTask = DistributeDataAsync(mainChannel.Reader, secondChannel.Writer);

            var bscanDisplayTask = UpdateBscanWithLoadedFramesAsync(mainChannel.Reader);
            var enfaceDisplayTask = UpdateEnfaceWithLoadedFramesAsync(secondChannel.Reader);

            var loadTask = LoadFramesAsync(mainChannel.Writer);
            await Task.WhenAll(loadTask, distributeTask, bscanDisplayTask, enfaceDisplayTask);
        }
        catch (OperationCanceledException)
        { Console.WriteLine("加载操作已取消。"); }
        catch (Exception e)
        { Console.WriteLine($"加载图像失败: {e.Message}"); }
        finally
        { IsProcessing = false; }
    }

    private async Task DistributeDataAsync(ChannelReader<float[,,]> reader, ChannelWriter<float[,,]> writer)
    {
        try
        {
            await foreach (var data in reader.ReadAllAsync(_cts.Token))
            { await writer.WriteAsync(data, _cts.Token); }
        }
        finally
        { writer.Complete(); }
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

    private async Task UpdateBscanWithLoadedFramesAsync(ChannelReader<float[,,]> reader)
    {
        try
        {
            await foreach (var floatData in reader.ReadAllAsync(_cts.Token))
            {
                if (RasterNum == 1)
                {
                    var floatData2D = floatData.To2DArray();
                    BscanLoaded = await _imageReader.ConvertFloatArrayToGrayImageAsync(floatData2D);
                }
                // else
                // {
                //     var bitmap = await _imageReader.ConvertFloat3DArrayToColorImageAsync(floatData);
                //     BscanLoaded = bitmap;
                // }
            }
        }
        catch (ChannelClosedException) { } // 通道关闭，正常退出
    }

    private async Task UpdateEnfaceWithLoadedFramesAsync(ChannelReader<float[,,]> reader)
    {
        try
        {
            await foreach (var floatData in reader.ReadAllAsync(_cts.Token))
            {
                if (RasterNum == 1)
                {
                    var floatData2D = floatData.To2DArray();
                    var projectionData = BscanProjection.MaxProjectionSpan(floatData2D, 1);
                    // 将投影数据添加到缓冲区
                    _enfaceBuffer.Enqueue(projectionData);
                    if (_enfaceBuffer.Count > SampNum)
                    { _enfaceBuffer.Dequeue(); }
                    // 将一维投影数据转换为2D数组以便显示
                    var enfaceData = new float[_enfaceBuffer.Count, projectionData.Length];
                    int row = 0;
                    foreach (var projection in _enfaceBuffer)
                    {
                        for (int col = 0; col < projection.Length; col++)
                        { enfaceData[row, col] = projection[col]; }
                        row++;
                    }

                    EnfaceImage = await _imageReader.ConvertFloatArrayToGrayImageAsync(enfaceData);
                }
            }
        }
        catch (ChannelClosedException) { }
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
