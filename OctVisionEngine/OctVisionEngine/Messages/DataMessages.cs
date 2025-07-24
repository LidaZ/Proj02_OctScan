using CommunityToolkit.Mvvm.Messaging.Messages;

namespace OctVisionEngine.Messages;

// 消息类就像工厂里的传送带上的标签，告诉下一个工序该处理什么
/// <summary>
/// 当读取到新的原始数据块时发送的消息
/// </summary>
public class RawDataReadyMessage : ValueChangedMessage<float[,]>
{
    public RawDataReadyMessage(float[,] value) : base(value)
    {
    }
}

/// <summary>
/// 当数据处理完成时发送的消息
/// </summary>
public class ProcessedDataReadyMessage : ValueChangedMessage<byte[,]>
{
    public ProcessedDataReadyMessage(byte[,] value) : base(value)
    {
    }
}

/// <summary>
/// 文件加载状态消息
/// </summary>
public class FileLoadingStatusMessage : ValueChangedMessage<string>
{
    public FileLoadingStatusMessage(string status) : base(status)
    {
    }
}

/// <summary>
/// 处理进度消息
/// </summary>
public class ProcessingProgressMessage : ValueChangedMessage<double>
{
    public ProcessingProgressMessage(double progress) : base(progress)
    {
    }
}
