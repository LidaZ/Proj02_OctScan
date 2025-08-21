using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OctVisionEngine.Models;

public partial class Debug_ImageRead : ObservableObject
{
    // private readonly float _minDb = -25f;
    // private readonly float _maxDb = 25f;
    // private const int AlinesPerFrame = 256;
    // private const int PixelsPerAline = 800;
    private readonly float _dbRange;
    private readonly int _blockSizeOfBscan;
    [ObservableProperty] private float _minDb = -25f;
    [ObservableProperty] private float _maxDb = 25f;
    [ObservableProperty] private int _alinesPerFrame = 256;
    [ObservableProperty] private int _pixelsPerAline = 800;
    [ObservableProperty] private int _rasterNumber = 1;

    public Debug_ImageRead()
    {
        _dbRange = _maxDb - _minDb;
        _blockSizeOfBscan = AlinesPerFrame * PixelsPerAline * sizeof(float);
    }


    public async IAsyncEnumerable<WriteableBitmap> LoadFramesSequenceFromBinAsync(string filePath, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(_blockSizeOfBscan); //只在方法开始时分配了一个大的 byte[] 数组，这个数组在堆上, 虽然读取快,但每次new一个类实例，都会涉及到堆内存分配和后续的垃圾回收（GC）开销。所以不参与循环.
        var floatData = new float[AlinesPerFrame, PixelsPerAline];
        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read); //await using语法糖 => try...finally{DisposeAsync()} 确保文件流读完后自动关闭
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await fileStream.ReadAsync(new Memory<byte>(buffer, 0, _blockSizeOfBscan), cancellationToken); //Memory<T> 和 Span<T> 都是结构体(struct), 分配在栈上, 每次new都只是把值复制拷贝到栈上, 没有内存分配所以没有GC开销. 故放在循环里重复读写.
                if (bytesRead < _blockSizeOfBscan)
                {
                    Console.WriteLine($"读取完成，最后一块字节 {bytesRead} / {_blockSizeOfBscan}");
                    break;  // break之后执行finally{}，然后跳出fileStream的作用域同时触发await using中自动实现的fileStram.DisposeAsync()
                }

                SwapByteEndianForFloat(buffer);
                Buffer.BlockCopy(buffer, 0, floatData, 0, _blockSizeOfBscan);

                var bitmapAfterConversion = await ConvertFloatArrayToGrayImageAsync(floatData);
                yield return bitmapAfterConversion;
                // return floatData;
            }
        }
        // async/await：这是最基础的语法糖，它解决了如何等待异步操作完成的问题。它的核心作用是管理单个异步任务的执行流程，并等待其结果。
        // System.Threading.Channels: 这是解决并发任务间数据传递的工具。它提供了一个线程安全、带有缓冲区的队列。生产者将数据放入通道，消费者从通道中取出数据，它们之间可以独立运行，速度不匹配时通道可以缓冲数据。
        // IAsyncEnumerable<T>：这是位于最高层的抽象，它解决了异步数据流的问题。它将 async/await 和 Channel 的功能结合起来，使得你可以使用熟悉的 await foreach 循环来处理一个接一个的异步数据项。
        // 它在底层就是基于 Channel 模式实现的，但它把所有复杂的生产者-消费者逻辑、线程安全和数据缓冲都封装了起来.
        // 所以并不需要直接使用 Channel，因为 IAsyncEnumerable 已经为你封装好了它的大部分功能，提供了更简洁、更高级的接口来处理这种异步数据流.
        // 在 IAsyncEnumerable 方法内部，你应该专注于用 try...finally 来处理资源清理。
        // 至于所有可能抛出的异常，应该让它们传播到调用端，由调用端的 try...catch 来统一处理.
        finally
        { ArrayPool<byte>.Shared.Return(buffer); }
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

    public async Task<WriteableBitmap> ConvertFloatArrayToGrayImageAsync(float[,] floatData)
    {
        if (floatData == null)
            throw new ArgumentNullException(nameof(floatData));
        int width = floatData.GetLength(0);   // 800
        int height = floatData.GetLength(1);  // 256
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
                        byte gray = (byte)(Math.Max(0f, Math.Min(1f, (floatData[x, y] - _minDb) / _dbRange)) * 255f);
                        uint grayPixel = 0xFF000000u | ((uint)gray << 16) | ((uint)gray << 8) | gray; // BGRA (Little-endian)
                        // uint grayPixel = ((uint)gray << 24) | ((uint)gray << 16) | ((uint)gray << 8) | 0xFF; //Big-endian,
                        // Console.WriteLine($"原值: {_floatData[x, y]}; 对应灰度值: {gray}");
                        pixelPtr[y * stride + x] = grayPixel;
                    }
                });
            }
        });
        return bitmap;
    }



}