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

    public Debug_ImageWindowViewModel()
    {
        _imageReader = new Debug_ImageRead();
        _cts = new CancellationTokenSource(); // 在构造函数中初始化 CancellationTokenSource
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
    private async Task LoadFramesContinuouslyAsync()
    {
        IsProcessing = true;
        try
        {
            await foreach (var bitmap in _imageReader.LoadFramesSequenceFromBinAsync(SelectedFilePath, _cts.Token))
            {
                while (_isPaused) { await Task.Delay(300, _cts.Token);}
                ImagePanelDebug = bitmap;
                // await Task.Delay(100);
            }
        }
        catch (OperationCanceledException)
        { Console.WriteLine("加载操作已取消。"); }
        catch (Exception e)
        { Console.WriteLine($"加载图像失败: {e.Message}"); }
        finally
        { IsProcessing = false; }
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
