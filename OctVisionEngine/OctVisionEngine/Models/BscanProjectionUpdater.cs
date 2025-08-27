namespace OctVisionEngine.Models;

public static class BscanProjectionUpdater
{
    public static void UpdateEnfaceArray(
        float[] projection1dArray,
        ref float[,] enfaceArray,
        ref int currentRow,
        int sampNumY,
        int sampNumX)
    {
        if (enfaceArray == null || enfaceArray.GetLength(0) != sampNumY || enfaceArray.GetLength(1) != sampNumX)
        {
            enfaceArray = new float[sampNumY, sampNumX];
            currentRow = 0;
        }
        for (int y = 0; y < projection1dArray.Length; y++)
        {
            enfaceArray[currentRow, y] = projection1dArray[y];
        }
        currentRow = (currentRow + 1) % sampNumY;
    }

    public static void UpdateEnfaceHsvArray(
        float[,] projectionHsvArray,
        ref float[,,] enfaceHsvArray,
        ref int currentRow,
        int sampNumY,
        int sampNumX)
    {
        if (enfaceHsvArray == null || enfaceHsvArray.GetLength(1) != sampNumY || enfaceHsvArray.GetLength(2) != sampNumX)
        {
            enfaceHsvArray = new float[3, sampNumY, sampNumX];
            currentRow = 0;
        }
        for (int hsvChannel = 0; hsvChannel < 3; hsvChannel++)
        {
            for (int y = 0; y < projectionHsvArray.GetLength(1); y++)
            {
                enfaceHsvArray[hsvChannel, currentRow, y] = projectionHsvArray[hsvChannel, y];
            }
        }
        currentRow = (currentRow + 1) % sampNumY;
    }
}