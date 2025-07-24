using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Avalonia.Controls;
// using Avalonia.Metadata;
// using Avalonia.Threading;

namespace OctVisionEngine.Models;

// public class CodeTest
// {
//     public static int Add(int x, int y) => x + y;
//     public static int Subtract(int x, int y) => x - y;
//     // private string aFriend = "Bill";
//     // Console.WriteLine(aFriend);
// }



public class DataItem
{
    public int Id { get; set; }
    public string RawData { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ProcessedData
{
    public int Id { get; set; }
    public string ProcessedContent { get; set; }
    public double Value { get; set; }
    public DateTime ProcessedTime { get; set; }
}

public class HighThroughputProcessor
{
    // 就像工厂的传送带 - 用Channel替代BlockingCollection，性能更好
    // 私有字段, 只读引用, 属于Channel类, 只允许传递(读写)DataItem类中包含的数据类型
    private readonly Channel<DataItem> _rawDataChannel;
    private readonly Channel<ProcessedData> _processedDataChannel;
    
    private readonly ChannelWriter<DataItem> _rawDataWriter;
    private readonly ChannelReader<DataItem> _rawDataReader;
    private readonly ChannelWriter<ProcessedData> _processedDataWriter;
    private readonly ChannelReader<ProcessedData> _processedDataReader;
    
    // 取消令牌 - 就像工厂的停机按钮
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    // UI控件引用
    private readonly TextBlock _statusTextBlock;
    private readonly ListBox _dataListBox;
    
    // 统计信息 - 就像工厂的生产报表
    private long _totalProcessed = 0;
    private long _totalDisplayed = 0;

    public HighThroughputProcessor(TextBlock statusTextBlock, ListBox dataListBox)
    {
        // 限制传送带上的物品数量，防止内存爆炸
        var options = new BoundedChannelOptions(1000) // 最多1000个待处理项
        {
            FullMode = BoundedChannelFullMode.Wait, // 满了就等待，像传送带堵塞
            SingleReader = false, // 允许多个读者（多个工人）
            SingleWriter = false  // 允许多个写者
        };
        
        _rawDataChannel = Channel.CreateBounded<DataItem>(options);
        // 获取读/写接口
        _rawDataReader = _rawDataChannel.Reader;
        _rawDataWriter = _rawDataChannel.Writer;
        
        _processedDataChannel = Channel.CreateBounded<ProcessedData>(options);
        _processedDataReader = _processedDataChannel.Reader;
        _processedDataWriter = _processedDataChannel.Writer;
        
        _cancellationTokenSource = new CancellationTokenSource();
        _statusTextBlock = statusTextBlock;
        _dataListBox = dataListBox;
    }

    public async Task StartAsync()
    {
        var token = _cancellationTokenSource.Token;
        
        // 启动各个车间的工人
        var tasks = new[]
        {
            // 数据采集车间 - 2个工人（线程）
            Task.Run(() => DataCollectionWorker(token), token),
            Task.Run(() => DataCollectionWorker(token), token),
            
            // 数据处理车间 - 4个工人（CPU密集型任务需要更多线程）
            Task.Run(() => DataProcessingWorker(token), token),
            Task.Run(() => DataProcessingWorker(token), token),
            Task.Run(() => DataProcessingWorker(token), token),
            Task.Run(() => DataProcessingWorker(token), token),
            
            // UI显示车间 - 1个工人（UI线程只能有一个）
            Task.Run(() => DisplayWorker(token), token),
            
            // 统计报告员
            Task.Run(() => StatusReporter(token), token)
        };
        
        await Task.WhenAll(tasks);
    }

    // 数据采集工人 - 就像传感器不断采集数据
    private async Task DataCollectionWorker(CancellationToken token)
    {
        var random = new Random();
        var itemId = 0;
        
        try
        {
            while (!token.IsCancellationRequested)
            {
                // 模拟数据采集 - 可能来自传感器、网络、文件等
                var dataItem = new DataItem
                {
                    Id = Interlocked.Increment(ref itemId),
                    RawData = $"SensorData_{itemId}_{random.Next(1000)}",
                    Timestamp = DateTime.Now
                };
                
                // 把数据放到传送带上
                await _rawDataWriter.WriteAsync(dataItem, token);
                
                // 模拟采集间隔 - 实际中可能是实时的
                await Task.Delay(10, token); // 每10ms采集一次，相当于100Hz采样率
            }
        }
        catch (OperationCanceledException)
        {
            // 工厂停工了，正常退出
        }
        finally
        {
            // 采集完成，关闭传送带写入端
            _rawDataWriter.Complete();
        }
    }

    // 数据处理工人 - 就像工人把原材料加工成产品
    private async Task DataProcessingWorker(CancellationToken token)
    {
        try
        {
            // 持续从传送带上拿原材料
            await foreach (var rawData in _rawDataReader.ReadAllAsync(token))
            {
                // 模拟复杂的数据处理 - 可能是算法计算、格式转换等
                var processedData = await ProcessDataAsync(rawData, token);
                
                // 处理完成，放到下一个传送带
                await _processedDataWriter.WriteAsync(processedData, token);
                
                // 更新统计
                Interlocked.Increment(ref _totalProcessed);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常退出
        }
        finally
        {
            // 如果所有处理工人都完成了，关闭输出传送带
            _processedDataWriter.Complete();
        }
    }

    // 实际的数据处理逻辑
    private async Task<ProcessedData> ProcessDataAsync(DataItem rawData, CancellationToken token)
    {
        // 模拟CPU密集型处理 - 比如复杂计算、图像处理、算法分析
        await Task.Delay(50, token); // 模拟50ms的处理时间
        
        // 提取数据中的数字
        var numberPart = rawData.RawData.Split('_').LastOrDefault() ?? "0";
        var value = double.TryParse(numberPart, out var v) ? v : 0.0;
        
        return new ProcessedData
        {
            Id = rawData.Id,
            ProcessedContent = $"Processed: {rawData.RawData}",
            Value = value * 1.5 + Math.Sin(value), // 一些数学处理
            ProcessedTime = DateTime.Now
        };
    }

    // UI显示工人 - 负责把结果展示给用户
    private async Task DisplayWorker(CancellationToken token)
    {
        try
        {
            await foreach (var processedData in _processedDataReader.ReadAllAsync(token))
            {
                // UI更新必须在UI线程上执行 - 就像只有前台服务员能接待客户
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // 更新UI - 添加到列表框
                    _dataListBox.Items.Add($"ID:{processedData.Id}, Value:{processedData.Value:F2}, Time:{processedData.ProcessedTime:HH:mm:ss.fff}");
                    
                    // 保持列表不要太长，就像显示屏空间有限
                    if (_dataListBox.Items.Count > 100)
                    {
                        _dataListBox.Items.RemoveAt(0);
                    }
                    
                    // 滚动到最新项
                    if (_dataListBox.Items.Count > 0)
                    {
                        _dataListBox.ScrollIntoView(_dataListBox.Items[^1]);
                    }
                }, Avalonia.Threading.DispatcherPriority.Background, token);
                
                Interlocked.Increment(ref _totalDisplayed);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常退出
        }
    }

    // 统计报告工人 - 定期更新生产统计
    private async Task StatusReporter(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(1000, token); // 每秒更新一次
                
                var processed = Interlocked.Read(ref _totalProcessed);
                var displayed = Interlocked.Read(ref _totalDisplayed);
                
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _statusTextBlock.Text = $"处理: {processed}/秒, 显示: {displayed}/秒 " +
                                          $"队列: 原始数据({_rawDataChannel.Reader.CanCount}), " +
                                          $"处理数据({_processedDataChannel.Reader.CanCount})";
                }, Avalonia.Threading.DispatcherPriority.Background, token);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常退出
        }
    }

    // 停止所有工作 - 就像按下工厂的停机按钮
    public void Stop()
    {
        _cancellationTokenSource.Cancel();
    }
    
    // 释放资源
    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
    }
}
//帮我实现一个高数据吞吐量的多线程(采集-处理-显示)处理的项目代码.
//要求: 1. 实现对二进制大文件的按块读取,每一块为256x700的2D数组,精度float32, big-endian; 
//2. 对每一个读出的2D数组的值使用octRangedB = [-15, 20]做归一化并转换至uint8精度,参考如下Python代码:octImgDb = (numpy.clip((octImg - octRangedB[0]) / (octRangedB[1] - octRangedB[0]), 0, 1) * 255).astype(dtype='uint8');
//3. 对每一个处理完成的2D数组实时显示; 
//4. 在Avalonia的框架下用MVVM模式,使用的是CommunityToolkit; 
//5. 项目的名称为OctVisionEngine,下面包含/Messages, /Models, /ViewModels, /Views 四个文件夹.  