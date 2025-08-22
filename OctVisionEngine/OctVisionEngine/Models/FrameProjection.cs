using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace OctVisionEngine.Models;

public class FrameProjection
{
    private float[,]? _enfaceProjectionData;
    private int _currentFrameIndex;
    private readonly object _lockObject = new();

    public void Initialize(int totalAlineNum, int sampNum)
    {
        lock (_lockObject)
        {
            _enfaceProjectionData = new float[totalAlineNum, sampNum];
            _currentFrameIndex = 0;
            // 初始化为最小值，因为我们要取最大值投影
            for (int i = 0; i < totalAlineNum; i++)
            {
                for (int j = 0; j < sampNum; j++)
                { _enfaceProjectionData[i, j] = float.MinValue; }
            }
        }
    }

    public async Task<WriteableBitmap?> UpdateEnfaceProjection(WriteableBitmap inputBitmap, int totalAlineNum, int count, float minDb = -25f, float maxDb = 25f)
    {
        if (_enfaceProjectionData == null)
        { Initialize(totalAlineNum, inputBitmap.PixelSize.Height); }
        // 从bitmap中提取投影数据
        var projectionLine = await ExtractMaxProjectionFromBitmap(inputBitmap);
        if (projectionLine == null) return null;
        // 更新En-face投影数据
        lock (_lockObject)
        {
            // 计算当前frame在En-face图像中的位置
            int enfaceX = count % totalAlineNum;
            // 将1D投影线放入2D En-face数据的对应位置
            for (int y = 0; y < projectionLine.Length && y < _enfaceProjectionData.GetLength(1); y++)
            { _enfaceProjectionData[enfaceX, y] = projectionLine[y]; }
            _currentFrameIndex = count;
        }
        // 生成En-face bitmap
        return await ConvertProjectionDataToBitmap(_enfaceProjectionData, minDb, maxDb);
    }

    private async Task<float[]?> ExtractMaxProjectionFromBitmap(WriteableBitmap bitmap)
    {
        if (bitmap == null) return null;
        int width = bitmap.PixelSize.Width;   // A-lines
        int height = bitmap.PixelSize.Height; // Pixels per A-line
        var projection = new float[width];
        await Task.Run(() =>
        {
            using var lockedBitmap = bitmap.Lock();
            unsafe
            {
                uint* pixelPtr = (uint*)lockedBitmap.Address;
                int stride = lockedBitmap.RowBytes / 4;

                // 对每个A-line位置（X坐标），沿深度方向（Y坐标）取最大值
                for (int x = 0; x < width; x++)
                {
                    float maxValue = float.MinValue;
                    for (int y = 0; y < height; y++)
                    {
                        uint pixel = pixelPtr[y * stride + x];
                        // 从BGRA格式提取灰度值并转换回float值
                        // 假设原始数据是灰度图，R=G=B
                        byte grayValue = (byte)(pixel & 0xFF); // 取B通道值
                        // 将灰度值转换回原始的dB值范围
                        // 这里需要反向转换 ConvertFloatArrayToGrayImageAsync 中的映射逻辑
                        float dbValue = (grayValue / 255f) * (25f - (-25f)) + (-25f); // 默认范围-25到25dB

                        if (dbValue > maxValue)
                        { maxValue = dbValue; }
                    }
                    projection[x] = maxValue;
                }
            }
        });

        return projection;
    }

    private async Task<WriteableBitmap> ConvertProjectionDataToBitmap(float[,] projectionData, float minDb, float maxDb)
    {
        int width = projectionData.GetLength(0);   // totalAlineNum
        int height = projectionData.GetLength(1);  // sampNum
        float dbRange = maxDb - minDb;

        var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);

        await Task.Run(() =>
        {
            using var lockedBitmap = bitmap.Lock();
            unsafe
            {
                uint* pixelPtr = (uint*)lockedBitmap.Address;
                int stride = lockedBitmap.RowBytes / 4;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float value = projectionData[x, y];

                        // 处理未初始化的像素
                        if (value == float.MinValue)
                        { pixelPtr[y * stride + x] = 0xFF000000u; } // 设置为黑色
                        else
                        {
                            // 正常的灰度映射
                            byte gray = (byte)(Math.Max(0f, Math.Min(1f, (value - minDb) / dbRange)) * 255f);
                            uint grayPixel = 0xFF000000u | ((uint)gray << 16) | ((uint)gray << 8) | gray;
                            pixelPtr[y * stride + x] = grayPixel;
                        }
                    }
                }
            }
        });

        return bitmap;
    }

    public void Reset()
    {
        lock (_lockObject)
        {
            _enfaceProjectionData = null;
            _currentFrameIndex = 0;
        }
    }


}