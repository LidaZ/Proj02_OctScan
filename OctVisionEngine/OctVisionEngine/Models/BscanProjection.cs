using System;
using System.Runtime.InteropServices;
namespace OctVisionEngine.Models;



public class BscanProjection
{
    public static float[] MaxProjectionSpan(float[,] source, int axis)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        int rows = source.GetLength(0);
        int cols = source.GetLength(1);

        // 将二维数组转换为 Span<float>
        var span = MemoryMarshal.CreateSpan(ref source[0, 0], rows * cols);

        switch (axis)
        {
            case 0: // 按行投影
                float[] result = new float[rows];
                for (int i = 0; i < rows; i++)
                {
                    var rowSpan = span.Slice(i * cols, cols);
                    result[i] = rowSpan[0];
                    for (int j = 1; j < cols; j++)
                    {
                        if (rowSpan[j] > result[i]) result[i] = rowSpan[j];
                    }
                }
                return result;

            case 1: // 按列投影
                float[] colResult = new float[cols];
                for (int j = 0; j < cols; j++) colResult[j] = float.MinValue;

                for (int i = 0; i < rows; i++)
                {
                    var rowSpan = span.Slice(i * cols, cols);
                    for (int j = 0; j < cols; j++)
                    {
                        if (rowSpan[j] > colResult[j]) colResult[j] = rowSpan[j];
                    }
                }
                return colResult;

            default:
                throw new ArgumentOutOfRangeException(nameof(axis));
        }
    }
}