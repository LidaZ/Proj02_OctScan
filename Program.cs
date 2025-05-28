// See https://aka.ms/new-console-template for more information

// Console.WriteLine("Hello, World!");  # 顶级语句
// using System;
namespace OctScanner
{
    class Rectangle
    {
        // 私有成员变量
        private double _length;
        private double _width;

        // 公有方法，用于从用户输入获取矩形的长度和宽度
        public void AcceptDetails()
        {
            Console.WriteLine("请输入长度：");
            _length = Convert.ToDouble(Console.ReadLine());
            Console.WriteLine("请输入宽度：");
            _width = Convert.ToDouble(Console.ReadLine());
        }

        // 公有方法，用于计算矩形的面积
        private double GetArea()
        {
            return _length * _width;
        }

        // 公有方法，用于显示矩形的属性和面积
        public void Display()
        {
            Console.WriteLine("长度： {0}", _length);
            Console.WriteLine("宽度： {0}", _width);
            Console.WriteLine("面积： {0}", GetArea());
            // Console.WriteLine(new int[1]);
        }
    }//end class Rectangle (test_4)

    class ExecuteRectangle
    {
        static void Main(string[] args)
        {
            // 创建 Rectangle 类的实例
            Rectangle r = new Rectangle();

            // 通过公有方法 AcceptDetails() 从用户输入获取矩形的长度和宽度
            r.AcceptDetails();

            // 通过公有方法 Display() 显示矩形的属性和面积
            r.Display();

            Console.ReadLine();
        }
    }
}