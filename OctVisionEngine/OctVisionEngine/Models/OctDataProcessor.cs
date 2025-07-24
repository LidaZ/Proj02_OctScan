using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using OctVisionEngine.Messages;

namespace OctVisionEngine.Models;

/// <summary>
/// OCT数据处理器 - 整个工厂的核心
/// </summary>
public class OctDataProcessor
{
    private const int Width = 700;
        private const int Height = 256;
        private const int FloatSize = 4;
        private const int BlockSize = Width * Height * FloatSize;
        
        private readonly float _minDb = -15f;
        private readonly float _maxDb = 20f;
        private readonly float _dbRange;
        
        // Channel就像工厂的传送带，可以缓冲数据
        private readonly Channel<float[,]> _rawDataChannel;
        private readonly Channel<byte[,]> _processedDataChannel;
        
        private CancellationTokenSource _cancellationTokenSource;
        private readonly IMessenger _messenger;

        public OctDataProcessor()
        {
            _dbRange = _maxDb - _minDb;
            _messenger = WeakReferenceMessenger.Default;
            
            // 创建有界通道，防止内存溢出（就像传送带有长度限制）, capacity=10 => 10帧缓冲(读取)
            var rawChannelOptions = new BoundedChannelOptions(10)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _rawDataChannel = Channel.CreateBounded<float[,]>(rawChannelOptions);
            
            // 10帧缓冲(处理)
            var processedChannelOptions = new BoundedChannelOptions(10)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
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
        }

        /// <summary>
        /// 读取工人 - 从文件读取数据块
        /// </summary>
        private async Task ReadDataAsync(string filePath, CancellationToken token)
        {
            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var fileLength = fileStream.Length;
                var totalBlocks = fileLength / BlockSize;
                var currentBlock = 0;
                
                // 使用ArrayPool减少内存分配（就像重复使用容器）
                var buffer = ArrayPool<byte>.Shared.Rent(BlockSize);
                
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var bytesRead = await fileStream.ReadAsync(buffer, 0, BlockSize, token);
                        if (bytesRead < BlockSize)
                            break;
                        
                        // 将字节转换为float数组
                        var floatData = ConvertBytesToFloat(buffer, Width, Height);
                        
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
                    _rawDataChannel.Writer.Complete();
                }
            }
            catch (Exception ex)
            {
                _messenger.Send(new FileLoadingStatusMessage($"读取错误: {ex.Message}"));
                _rawDataChannel.Writer.Complete();
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
                _processedDataChannel.Writer.Complete();
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
        /// 将字节数组转换为float二维数组, >big-endian）
        /// </summary>
        private float[,] ConvertBytesToFloat(byte[] buffer, int width, int height)
        {
            var result = new float[height, width];
            var index = 0;
            
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    // Big-endian转换 
                    if (BitConverter.IsLittleEndian)
                    {
                        var bytes = new byte[4];
                        bytes[0] = buffer[index + 3];
                        bytes[1] = buffer[index + 2];
                        bytes[2] = buffer[index + 1];
                        bytes[3] = buffer[index];
                        result[i, j] = BitConverter.ToSingle(bytes, 0);
                    }
                    else
                    {
                        result[i, j] = BitConverter.ToSingle(buffer, index);
                    }
                    index += 4;
                }
            }
            
            return result;
        }

        /// <summary>
        /// 处理数据块 - 归一化并转换为uint8
        /// </summary>
        private byte[,] ProcessBlock(float[,] input)
        {
            var height = input.GetLength(0);
            var width = input.GetLength(1);
            var result = new byte[height, width];
            
            // 并行处理提高效率（多个工人同时处理不同行）
            Parallel.For(0, height, i =>
            {
                for (int j = 0; j < width; j++)
                {
                    // 实现Python代码的逻辑
                    var normalized = (input[i, j] - _minDb) / _dbRange;
                    var clipped = Math.Max(0, Math.Min(1, normalized));
                    result[i, j] = (byte)(clipped * 255);
                }
            });
            
            return result;
        }
}