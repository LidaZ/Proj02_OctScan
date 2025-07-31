using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
using System.Diagnostics;
using System.Runtime.CompilerServices;


namespace OctVisionEngine.Views;

public partial class ImageWindow : Window, INotifyPropertyChanged
{
    // private CodeTest _codeTest;
    // private float[,] _floatData;
    private readonly float _minDb = -25f;
    private readonly float _maxDb = 25f;
    private readonly float _dbRange;
    public WriteableBitmap _displayBitmap;
    public event PropertyChangedEventHandler PropertyChanged;

    public WriteableBitmap DisplayBitmap
    {
        get => _displayBitmap;
        set
        {
            _displayBitmap = value;
            OnPropertyChanged();
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

    public ImageWindow()
    {
        _dbRange = _maxDb - _minDb;
        InitializeComponent();
        DataContext = this;
        SetupExitButton();
        LoadFrameFromBin();
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

    private async void LoadFrameFromBin()
    {
        int AlinesPerFrame = 256;
        int PixelsPerAline = 800;
        var _blockSize = AlinesPerFrame * PixelsPerAline * sizeof(float);
        var _filePath = @"J:\Data_2025\20250326_Jurkat4\Day0_Control_Pos1(bottom)\Data.bin";

        var buffer = ArrayPool<byte>.Shared.Rent(_blockSize);
        var _floatData = new float[AlinesPerFrame, PixelsPerAline];
        using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
        try
        {
            var bytesRead = await fileStream.ReadAsync(buffer, 0, _blockSize);
            // if (bytesRead < _blockSize)
            // { Console.WriteLine($"读取完成，最后一块字节 {bytesRead} / {_blockSize}"); }
            SwapByteEndianForFloat(buffer);
            Buffer.BlockCopy(buffer, 0, _floatData, 0, _blockSize);
            // for (int col = 0; col < 10; col++) { Console.Write($"{_floatData[50, col]} "); }
            await ConvertFloatArrayToGrayImage(_floatData);
        }
        catch (Exception e)
        {
            Console.WriteLine($"读取中断: {e.Message}");
            throw;
        }

    }

    private static void SwapByteEndianForFloat(Span<byte> buffer)
    {
        if (buffer.Length % 4 != 0)
            throw new ArgumentException("Buffer length should be 4*int");
        var uintSpan = MemoryMarshal.Cast<byte, uint>(buffer);
        for (int i = 0; i < uintSpan.Length; i++)
        {
            uint value = uintSpan[i];
            uintSpan[i] = ((value & 0x000000FF) << 24) |
                          ((value & 0x0000FF00) << 8) |
                          ((value & 0x00FF0000) >> 8) |
                          ((value & 0xFF000000) >> 24);
        }
    }

    private async Task ConvertFloatArrayToGrayImage(float[,] _floatData)
    {
        if (_floatData == null) return;
        int width = _floatData.GetLength(0);   // 800
        int height = _floatData.GetLength(1);  // 256

        var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
        await Task.Run(() =>
        {
            using var lockedBitmap = bitmap.Lock();
            unsafe
            {
                uint* pixelPtr = (uint*)lockedBitmap.Address; // 直接用uint指针，一次写4个字节
                int stride = lockedBitmap.RowBytes / 4; // uint步长
                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        byte gray = (byte)(Math.Max(0f, Math.Min(1f, (_floatData[x, y] - _minDb) / _dbRange)) * 255f);
                        uint grayPixel = 0xFF000000u | ((uint)gray << 16) | ((uint)gray << 8) | gray;  // BGRA (Little-endian)
                        // uint grayPixel = ((uint)gray << 24) | ((uint)gray << 16) | ((uint)gray << 8) | 0xFF; //Big-endian, 说不定是这个问题?试试这行代码
                        // Console.WriteLine($"原值: {_floatData[x, y]}; 对应灰度值: {gray}");
                        pixelPtr[y * stride + x] = grayPixel;
                    }
                });
            }
        });
        DisplayBitmap = bitmap;
    }


}