using NUnit.Framework;
using OctVisionEngine.Models; 

namespace UnitTestTool;

public class Tests
{
    [SetUp]
    public void Setup()
    {
        Console.WriteLine("=== 测试开始 ===");
    }

    [Test]
    public void TestAdd()
    {
        int x = 5;
        int y = 3;
        int expeted = 8;
        
        int result = CodeTest.Add(x, y);
        Assert.That(result, Is.EqualTo(expeted));
        Console.WriteLine("Add 测试通过");
    }
    
    [Test]
    public void TestSubtract()
    {
        int x = 5;
        int y = 3;
        int expeted = 2;
        
        int result = CodeTest.Subtract(x, y);
        Assert.That(result, Is.EqualTo(expeted));
        Console.WriteLine("Subtract 测试通过");
    }
}