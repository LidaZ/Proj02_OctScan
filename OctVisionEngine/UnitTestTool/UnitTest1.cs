using System.Buffers;
using NUnit.Framework;
using OctVisionEngine.Models;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;

namespace UnitTestTool;

public class Tests
{
    [SetUp]
    public void Setup()
    {
        Console.WriteLine("=== 测试开始 ===");
    }

    [Test]
    public async Task Test_CodeTest()
    {
        // var codetest = new CodeTest();
        // codetest.InitializeAsync();
        // var floatData = codetest.GetFloatData();

        int PixelsPerAline = 800;
        int AlinesPerFrame = 256;
        var _blockSize = PixelsPerAline * AlinesPerFrame * sizeof(float);
        var _filePath = @"J:\Data_2025\20250326_Jurkat4\Day0_Control_Pos1(bottom)\Data.bin";

        var buffer = ArrayPool<byte>.Shared.Rent(_blockSize);
        var _floatData = new float[PixelsPerAline, AlinesPerFrame];
        using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
        var bytesRead = await fileStream.ReadAsync(buffer, 0, _blockSize);
        ConvertFloatEndian(buffer);
        Buffer.BlockCopy(buffer, 0, _floatData, 0, _blockSize);
        // for (int col = 0; col < 10; col++) { Console.Write($"{_floatData[50, col]} "); }
    }

    private static void ConvertFloatEndian(Span<byte> buffer)
    {
        // byte[] bytes = BitConverter.GetBytes(value);
        // Array.Reverse(bytes);
        // return BitConverter.ToSingle(bytes, 0);
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