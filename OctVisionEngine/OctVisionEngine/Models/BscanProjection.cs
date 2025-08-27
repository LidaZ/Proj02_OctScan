using System;
using System.Runtime.InteropServices;
namespace OctVisionEngine.Models;



public class BscanProjection
{
    public static float[] MaxProjectionSpan(float[,] source, int axis)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var rows = source.GetLength(0);
        var cols = source.GetLength(1);
        // 将二维数组转换为 Span<float>
        var span = MemoryMarshal.CreateSpan(ref source[0, 0], rows * cols);
        switch (axis)
        {
            case 0: // 按行投影
                var result = new float[rows];
                for (int i = 0; i < rows; i++)
                {
                    var rowSpan = span.Slice(i * cols, cols);
                    result[i] = rowSpan[0];
                    for (int j = 1; j < cols; j++)
                    { if (rowSpan[j] > result[i]) result[i] = rowSpan[j]; }
                }
                return result;

            case 1: // 按列投影
                var colResult = new float[cols];
                for (int j = 0; j < cols; j++) colResult[j] = float.MinValue;
                for (int i = 0; i < rows; i++)
                {
                    var rowSpan = span.Slice(i * cols, cols);
                    for (int j = 0; j < cols; j++)
                    { if (rowSpan[j] > colResult[j]) colResult[j] = rowSpan[j]; }
                }
                return colResult;

            default:
                throw new ArgumentOutOfRangeException(nameof(axis));
        }
    }


    // hsv3dArray[hsvChannel, width, height], where hsvChannel: 0 is hue, 1 is saturation (always f1.0), 2 is value
    public static float[,] MaxHueProjectionSpan(float[,,] hsv3dArray, int axis)
    {
        if (hsv3dArray == null) throw new ArgumentNullException(nameof(hsv3dArray));
        if (hsv3dArray.GetLength(0) != 3) throw new ArgumentException("hsv3dArray[hsvChannel, width, height] hsvChan is not 3");
        var rows = hsv3dArray.GetLength(1);
        var cols = hsv3dArray.GetLength(2);
        var channelSize = rows * cols;
        var hueSpan = MemoryMarshal.CreateSpan(ref hsv3dArray[0, 0, 0], channelSize);
        var satSpan = MemoryMarshal.CreateSpan(ref hsv3dArray[1, 0, 0], channelSize);
        var valSpan = MemoryMarshal.CreateSpan(ref hsv3dArray[2, 0, 0], channelSize);
        switch (axis)
        {
            case 0: // 按行投影，结果数组为 [3, rows]
            {
                var result = new float[3, rows];
                for (int i = 0; i < rows; i++)
                {
                    var hueRowSpan = hueSpan.Slice(i * cols, cols);
                    var satRowSpan = satSpan.Slice(i * cols, cols);
                    var valRowSpan = valSpan.Slice(i * cols, cols);
                    var maxHue = -1f; // hue值是正数，所以用-1作为初始值
                    var maxHueIndex = -1;
                    for (int j = 0; j < cols; j++)
                    {
                        if (hueRowSpan[j] > maxHue)
                        { maxHue = hueRowSpan[j]; maxHueIndex = j; }
                    }
                    result[0, i] = maxHue;
                    result[1, i] = satRowSpan[maxHueIndex];
                    result[2, i] = valRowSpan[maxHueIndex];
                }
                return result;
            }

            case 1: // 按列投影，结果数组为 [3, cols]
            {
                var result = new float[3, cols];
                var maxHueIndices = new int[cols];
                for (int j = 0; j < cols; j++)
                {
                    result[0, j] = -1f;
                    maxHueIndices[j] = -1;
                }
                for (int i = 0; i < rows; i++)
                {
                    var hueRowSpan = hueSpan.Slice(i * cols, cols);
                    var satRowSpan = satSpan.Slice(i * cols, cols);
                    var valRowSpan = valSpan.Slice(i * cols, cols);
                    for (int j = 0; j < cols; j++)
                    {
                        if (hueRowSpan[j] > result[0, j])
                        {
                            result[0, j] = hueRowSpan[j];
                            result[1, j] = satRowSpan[j];
                            result[2, j] = valRowSpan[j];
                        }
                    }
                }
                return result;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(axis), "Axis must be 0 (rows) or 1 (columns).");
        }
    }

}