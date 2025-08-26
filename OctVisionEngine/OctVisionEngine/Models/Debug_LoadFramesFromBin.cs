using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OctVisionEngine.Models;

public partial class Debug_LoadFramesFromBin : ObservableObject
{
    private readonly float _dbRange;
    private int _blockSizeRead;
    private WriteableBitmap? _bscanBitmap;
    private WriteableBitmap? _enfaceBitmap;
    [ObservableProperty] private float _minDb = -25f;
    [ObservableProperty] private float _maxDb = 25f;
    [ObservableProperty] private int _alinesPerFrame = 256;
    [ObservableProperty] private int _pixelsPerAline = 800;
    [ObservableProperty] private int _rasterNumber = 1;

    public Debug_LoadFramesFromBin()
    {
        _dbRange = _maxDb - _minDb;
        // _blockSizeRead = RasterNumber * AlinesPerFrame * PixelsPerAline * sizeof(float);
    }

    public async IAsyncEnumerable<float[,,]> LoadFramesSequenceFromBinAsync(string filePath, int rasterNumber, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _blockSizeRead = rasterNumber * AlinesPerFrame * PixelsPerAline * sizeof(float);
        var buffer = ArrayPool<byte>.Shared.Rent(_blockSizeRead);
        // 预分配重用的数组，避免每次循环都创建新对象
        // float[,,]? floatData2D = rasterNumber == 1 ? new float[rasterNumber, AlinesPerFrame, PixelsPerAline] : null;
        // float[,,]? floatData3D = new float[rasterNumber, AlinesPerFrame, PixelsPerAline] : null;
        float[,,] floatData3D = rasterNumber == 1 ? new float[1, AlinesPerFrame, PixelsPerAline] : new float[rasterNumber, AlinesPerFrame, PixelsPerAline];
        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read); //await using语法糖 => try...finally{DisposeAsync()} 确保文件流读完后自动关闭
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await fileStream.ReadAsync(new Memory<byte>(buffer, 0, _blockSizeRead), cancellationToken);
                if (bytesRead < _blockSizeRead)
                {
                    Console.WriteLine($"读取完成，最后一块字节 {bytesRead} / {_blockSizeRead}");
                    break;
                }
                SwapByteEndianForFloat(buffer.AsSpan(0, _blockSizeRead));
                // WriteableBitmap bitmapAfterConversion;
                if (rasterNumber == 1)
                {
                    // var floatData2D = new float[AlinesPerFrame, PixelsPerAline];
                    Buffer.BlockCopy(buffer, 0, floatData3D, 0, _blockSizeRead);
                    // bitmapAfterConversion = await ConvertFloatArrayBscanToGrayAsync(floatData2D);
                    yield return floatData3D;
                }
                // else
                // {
                //     // var floatData3D = new float[rasterNumber, AlinesPerFrame, PixelsPerAline];
                //     Buffer.BlockCopy(buffer, 0, floatData3D, 0, _blockSizeRead);
                //     // bitmapAfterConversion = await ConvertFloat3DArrayToColorImageAsync(floatData3D);
                // }
                // yield return bitmapAfterConversion;
            }
        }
        finally
        { ArrayPool<byte>.Shared.Return(buffer); }
    }

    //     var buffer = ArrayPool<byte>.Shared.Rent(_blockSizeOfBscan); //只在方法开始时分配了一个大的 byte[] 数组，这个数组在堆上, 虽然读取快,但每次new一个类实例，都会涉及到堆内存分配和后续的垃圾回收（GC）开销。所以不参与循环.
    //     var floatData = new float[AlinesPerFrame, PixelsPerAline];
    //     try
    //     {
    //         while (!cancellationToken.IsCancellationRequested)
    //         {
    //             var bytesRead = await fileStream.ReadAsync(new Memory<byte>(buffer, 0, _blockSizeOfBscan), cancellationToken); //Memory<T> 和 Span<T> 都是结构体(struct), 分配在栈上, 每次new都只是把值复制拷贝到栈上, 没有内存分配所以没有GC开销. 故放在循环里重复读写.
    //             if (bytesRead < _blockSizeOfBscan)
    //             {
    //                 Console.WriteLine($"读取完成，最后一块字节 {bytesRead} / {_blockSizeOfBscan}");
    //                 break;  // break之后执行finally{}，然后跳出fileStream的作用域同时触发await using中自动实现的fileStram.DisposeAsync()
    //             }
    //
    //             SwapByteEndianForFloat(buffer);
    //             Buffer.BlockCopy(buffer, 0, floatData, 0, _blockSizeOfBscan);
    //
    //             var bitmapAfterConversion = await ConvertFloatArrayBscanToGrayAsync(floatData);
    //             yield return bitmapAfterConversion;
    //             // return floatData;
    //         }
    // }
        // async/await：这是最基础的语法糖，它解决了如何等待异步操作完成的问题。它的核心作用是管理单个异步任务的执行流程，并等待其结果。
        // System.Threading.Channels: 这是解决并发任务间数据传递的工具。它提供了一个线程安全、带有缓冲区的队列。生产者将数据放入通道，消费者从通道中取出数据，它们之间可以独立运行，速度不匹配时通道可以缓冲数据。
        // IAsyncEnumerable<T>：这是位于最高层的抽象，它解决了异步数据流的问题。它将 async/await 和 Channel 的功能结合起来，使得你可以使用熟悉的 await foreach 循环来处理一个接一个的异步数据项。
        // 它在底层就是基于 Channel 模式实现的，但它把所有复杂的生产者-消费者逻辑、线程安全和数据缓冲都封装了起来.
        // 所以并不需要直接使用 Channel，因为 IAsyncEnumerable 已经为你封装好了它的大部分功能，提供了更简洁、更高级的接口来处理这种异步数据流.
        // 在 IAsyncEnumerable 方法内部，你应该专注于用 try...finally 来处理资源清理。
        // 至于所有可能抛出的异常，应该让它们传播到调用端，由调用端的 try...catch 来统一处理.
        // finally
        // { ArrayPool<byte>.Shared.Return(buffer); }
    // }

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

    public async Task<WriteableBitmap> ConvertFloatArrayBscanToGrayAsync(float[,] floatArrayBscan)
    {
        if (floatArrayBscan == null)
            throw new ArgumentNullException(nameof(floatArrayBscan));
        int bscanWidth = floatArrayBscan.GetLength(0);   // AlinesPerFrame = 800
        int bscanHeight = floatArrayBscan.GetLength(1);  // PixelsPerAline = 256

        _bscanBitmap = new WriteableBitmap(new PixelSize(bscanWidth, bscanHeight), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
        await Task.Run(() =>
        {
            using var lockedBitmap = _bscanBitmap.Lock();
            unsafe
            {
                uint* pixelPtr = (uint*)lockedBitmap.Address; // 直接用uint指针，一次写4个字节
                int stride = lockedBitmap.RowBytes / 4; // uint步长
                Parallel.For(0, bscanHeight, y =>
                {
                    for (int x = 0; x < bscanWidth; x++)
                    {
                        byte gray = (byte)(Math.Max(0f, Math.Min(1f, (floatArrayBscan[x, y] - _minDb) / _dbRange)) * 255f);
                        uint grayPixel = 0xFF000000u | ((uint)gray << 16) | ((uint)gray << 8) | gray; // BGRA (Little-endian)
                        // uint grayPixel = ((uint)gray << 24) | ((uint)gray << 16) | ((uint)gray << 8) | 0xFF; //Big-endian,
                        // Console.WriteLine($"原值: {_floatData[x, y]}; 对应灰度值: {gray}");
                        pixelPtr[y * stride + x] = grayPixel;
                    }
                });
            }
        });
        return _bscanBitmap;
    }

    public async Task<WriteableBitmap> ConvertFloatArrayEnfaceToGrayAsync(float[,] floatDataEnface)
    {
        if (floatDataEnface == null)
            throw new ArgumentNullException(nameof(floatDataEnface));
        int enfaceWidth = floatDataEnface.GetLength(0);   // AlinesPerFrame = 800
        int enfaceHight = floatDataEnface.GetLength(1);  // PixelsPerAline = 256
        _enfaceBitmap = new WriteableBitmap(new PixelSize(enfaceWidth, enfaceHight), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
        await Task.Run(() =>
        {
            using var lockedBitmap = _enfaceBitmap.Lock();
            unsafe
            {
                uint* pixelPtr = (uint*)lockedBitmap.Address; // 直接用uint指针，一次写4个字节
                int stride = lockedBitmap.RowBytes / 4; // uint步长
                Parallel.For(0, enfaceHight, y =>
                {
                    for (int x = 0; x < enfaceWidth; x++)
                    {
                        byte gray = (byte)(Math.Max(0f, Math.Min(1f, (floatDataEnface[x, y] - _minDb) / _dbRange)) * 255f);
                        uint grayPixel = 0xFF000000u | ((uint)gray << 16) | ((uint)gray << 8) | gray; // BGRA (Little-endian)
                        // uint grayPixel = ((uint)gray << 24) | ((uint)gray << 16) | ((uint)gray << 8) | 0xFF; //Big-endian,
                        // Console.WriteLine($"原值: {_floatData[x, y]}; 对应灰度值: {gray}");
                        pixelPtr[y * stride + x] = grayPixel;
                    }
                });
            }
        });
        return _enfaceBitmap;
    }


    public async Task<WriteableBitmap> ConvertFloat3DArrayToColorImageAsync(float[,,] floatData3D)
    {
        if (floatData3D == null)
            throw new ArgumentNullException(nameof(floatData3D));
        int rasterCount = floatData3D.GetLength(0);  // RasterNumber
        int width = floatData3D.GetLength(1);        // AlinesPerFrame
        int height = floatData3D.GetLength(2);       // PixelsPerAline
        var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
        await Task.Run(() =>
        {
            using var lockedBitmap = bitmap.Lock();
            unsafe
            {
                uint* pixelPtr = (uint*)lockedBitmap.Address;
                int stride = lockedBitmap.RowBytes / 4;
                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        // 沿RasterNumber轴计算统计值
                        float sum = 0f;
                        float sumSquares = 0f;
                        for (int ra = 0; ra < rasterCount; ra++)
                        {
                            float arrayValue = floatData3D[ra, x, y];
                            sum += arrayValue;
                            sumSquares += arrayValue * arrayValue;
                        }
                        // 计算平均值和方差
                        float mean = sum / rasterCount;
                        float variance = (sumSquares / rasterCount) - (mean * mean);
                        // 将统计值映射到HSV色彩空间
                        // Hue: 方差映射到0-360度 (这里需要根据你的数据范围调整)
                        float hue = Math.Max(0f, Math.Min(360f, variance * 10f)); // 简单的线性映射，你可以调整倍数
                        float saturation = 1.0f;  // Saturation: 固定为1（最饱和）
                        float value = Math.Max(0f, Math.Min(1f, (mean - _minDb) / _dbRange));  // Value: 平均值映射到0-1
                        var (r, g, b) = HsvToRgb(hue, saturation, value);  // HSV转RGB
                        uint colorPixel = 0xFF000000u | ((uint)(r * 255) << 16) | ((uint)(g * 255) << 8) | (uint)(b * 255);  // 转换为BGRA像素格式
                        pixelPtr[y * stride + x] = colorPixel;
                    }
                });
            }
        });
        return bitmap;
    }

    private static (float r, float g, float b) HsvToRgb(float h, float s, float v)
    {
        h = h / 360f;  // 将色调(满值360)标准化到0-1范围
        int i = (int)Math.Floor(h * 6);
        float f = h * 6 - i;
        float p = v * (1 - s);
        float q = v * (1 - f * s);
        float t = v * (1 - (1 - f) * s);
        return (i % 6) switch
        {
            0 => (v, t, p),
            1 => (q, v, p),
            2 => (p, v, t),
            3 => (p, q, v),
            4 => (t, p, v),
            _ => (v, p, q)
        };
    }


}


public static class FloatArrayExtensions
{
    /// <summary>
    /// 将3D数组的第一层转换为2D数组
    /// </summary>
    /// <param name="source">源3D数组</param>
    /// <returns>提取的2D数组</returns>
    public static float[,] To2DArray(this float[,,] source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        int width = source.GetLength(1);
        int height = source.GetLength(2);
        var result = new float[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                result[x, y] = source[0, x, y];
            }
        }

        return result;
    }
}