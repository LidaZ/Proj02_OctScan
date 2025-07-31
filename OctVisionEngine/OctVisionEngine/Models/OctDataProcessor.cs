using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using OctVisionEngine.Messages;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OctVisionEngine.Models;

/// <summary>
/// OCT数据处理器 - 整个工厂的核心
/// </summary>
public class OctDataProcessor
{
    public int PixelsPerAline { get; set; } = 800;
    public int AlinesPerFrame { get; set; } = 256;
    private readonly int _blockSize; //= PixelsPerAline * AlinesPerFrame * _floatSize;
    private readonly float _minDb = -25f;
    private readonly float _maxDb = 25f;
    private readonly float _dbRange;
    private float[,] _floatData;


    // Channel就像工厂的传送带，可以缓冲数据
    private readonly Channel<float[,]> _rawDataChannel;
    private readonly Channel<byte[,]> _processedDataChannel;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly IMessenger _messenger;

    public OctDataProcessor()
    {
        _dbRange = _maxDb - _minDb;
        _blockSize = PixelsPerAline * AlinesPerFrame * sizeof(float);
        _floatData = new float[PixelsPerAline, AlinesPerFrame];
        _messenger = WeakReferenceMessenger.Default;
        
        // 创建有界通道，防止内存溢出（长度限制）, capacity=10 => 10帧缓冲(读取)
        var rawChannelOptions = new BoundedChannelOptions(10)
        { FullMode = BoundedChannelFullMode.Wait };
        _rawDataChannel = Channel.CreateBounded<float[,]>(rawChannelOptions);
        
        // 10帧缓冲(处理)
        var processedChannelOptions = new BoundedChannelOptions(10)
        { FullMode = BoundedChannelFullMode.Wait };
        _processedDataChannel = Channel.CreateBounded<byte[,]>(processedChannelOptions);
    }

    /// <summary>
    /// 启动处理管道
    /// </summary>
    public async Task StartProcessingAsync(string filePath)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        
        // 启动三个工人同时工作
        var readTask = ReadDataAsync(filePath, token);
        var processTask = ProcessDataAsync(token);
        var displayTask = DisplayDataAsync(token);
        
        await Task.WhenAll(readTask, processTask, displayTask);
    }

    /// <summary>
    /// 停止处理
    /// </summary>
    public void StopProcessing()
    {
        _cancellationTokenSource?.Cancel();
        // 等待所有任务完成或取消
        _rawDataChannel.Writer.TryComplete();
        _processedDataChannel.Writer.TryComplete();
    }

    /// <summary>
    /// 读取工人 - 从文件读取数据块
    /// </summary>
    private async Task ReadDataAsync(string filePath, CancellationToken token)
    {
        try
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);  //using 可以让函数执行完自动调用.Dispose()
            var fileLength = fileStream.Length;
            var totalBlocks = fileLength / _blockSize;  // 检查过了, 是4096没问题
            var currentBlock = 0;

            // 使用ArrayPool减少内存分配（重复使用容器）
            var buffer = ArrayPool<byte>.Shared.Rent(_blockSize);
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var bytesRead = await fileStream.ReadAsync(buffer, 0, _blockSize, token); //第 1 块，读取了 819200 字节
                    ConvertFloatEndian(buffer);
                    if (bytesRead < _blockSize)
                    {
                        Console.WriteLine($"读取完成，最后一块只有 {bytesRead} 字节");
                        break;
                    }
                    // 检查数据大小:  bytesRead = expectedBytes = PixelsPerAline * AlinesPerFrame * 4 = 819200
                    // 将字节转换为float数组
                    var floatData = new float[AlinesPerFrame, PixelsPerAline];
                    Buffer.BlockCopy(buffer, 0, floatData, 0, _blockSize);
                    // 发送到处理通道
                    await _rawDataChannel.Writer.WriteAsync(floatData, token);
                    
                    // 更新进度
                    currentBlock++;
                    var progress = (double)currentBlock / totalBlocks * 100;
                    _messenger.Send(new ProcessingProgressMessage(progress));
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                _rawDataChannel.Writer.TryComplete();
            }
        }
        catch (Exception ex)
        {
            _messenger.Send(new FileLoadingStatusMessage($"读取中断: {ex.Message}"));
            _rawDataChannel.Writer.TryComplete();
            
        }
    }

    /// <summary>
    /// 处理工人 - 归一化和转换数据
    /// </summary>
    private async Task ProcessDataAsync(CancellationToken token)
    {
        try
        {
            await foreach (var rawData in _rawDataChannel.Reader.ReadAllAsync(token))
            {
                var processedData = ProcessBlock(rawData);
                await _processedDataChannel.Writer.WriteAsync(processedData, token);
            }
        }
        finally
        {
            _processedDataChannel.Writer.TryComplete();
        }
    }

    /// <summary>
    /// 显示工人 - 发送数据到UI
    /// </summary>
    private async Task DisplayDataAsync(CancellationToken token)
    {
        await foreach (var processedData in _processedDataChannel.Reader.ReadAllAsync(token))
        {
            _messenger.Send(new ProcessedDataReadyMessage(processedData));
            // 控制显示速率，避免UI卡顿（就像控制传送带速度）
            await Task.Delay(33, token); // 约30fps
        }
    }


    /// <summary>
    /// 处理数据块 - 归一化并转换为uint8
    /// </summary>
    private byte[,] ProcessBlock(float[,] input)
    {
        var height = input.GetLength(0);  // AlinesPerFrame = 256
        var width = input.GetLength(1);   // PixelsPerAline = 800
        var result = new byte[height, width];

        Console.WriteLine($"Process block: height={height}, width={width}");

        // 🔧 优化：预计算倒数，避免除法
        float invDbRange = 1.0f / _dbRange;

        Parallel.For(0, height, i =>
        {
            for (int j = 0; j < width; j++)
            {
                var normalized = (input[i, j] - _minDb) * invDbRange;
                var clipped = Math.Clamp(normalized, 0f, 1f);  // 🔧 使用Math.Clamp更高效
                result[i, j] = (byte)(clipped * 255);
            }
        });

        return result;
    }

    private static void ConvertFloatEndian(Span<byte> buffer)
    {
        if (buffer.Length % 4 != 0)
            throw new ArgumentException("Buffer长度必须是4的倍数");

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
    
}