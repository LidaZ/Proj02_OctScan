// See https://aka.ms/new-console-template for more information

// Console.WriteLine("Hello, World!");  # 顶级语句
// using System;
namespace OctScanner
{
    class Testfunc
    {
        // 私有成员变量
        internal double InputDisplay;
        private double _result; 

        // 公有方法，用于从用户输入获取矩形的长度和宽度
        public void GetInput()
        {
            Console.WriteLine("Enter your input：");
            InputDisplay = Convert.ToDouble(Console.ReadLine());
        }
        
        private double Calculate()
        {
            // _result = _length * _width;
            _result = --InputDisplay;
            return _result;
        }
        
        public void Display()
        {
            Console.WriteLine("The input integer is： {0}", Calculate());
            // Console.WriteLine(new int[1]);
        }
    }

    
    class CodeRun
    {
        static void Main(string[] args)
        {
            // 创建 testfunc 类的实例
            Testfunc r = new Testfunc(); 
            r.GetInput();
            if (r.InputDisplay % 1 == 0)
            {
                r.Display();
            }
            else  Console.WriteLine("The input is not an integer");
            switch (r.InputDisplay)
            {
                case (> 0):
                    Console.WriteLine("and it's > 0");
                    break;
                case (< 0):
                    Console.WriteLine("and it's < 0");
                    break;
                default:
                    Console.WriteLine("and it's zero");
                    break;
            }

            Console.WriteLine("Announcement: code test finish.");
            // Console.ReadLine();
        }
    }
}