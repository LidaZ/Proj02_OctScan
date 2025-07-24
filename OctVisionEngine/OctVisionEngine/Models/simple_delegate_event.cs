using System;

// 
public class Publisher
{
    // 声明1: (通知书的格式规范名为"MyDelegate", 必须包含一个 message")
    // delegate -> 接下来要声明一种通知书的格式(只需要自带一个string)
    // (也可以声明其他的格式规范,例如 MyDelegate2(int32 number))
    public delegate void MyDelegate(string message);

    // 声明2: (Event: 遵循通知书格式规范'MyDelegate'的 通知书1 和通知书2)
    // Event1, Event2,... -> 接下来要声明每种通知书的名字/抬头
    public event MyDelegate Event1;
    public event MyDelegate Event2;
    // 现代模式: 
    // public event Action<string> Event1; // 等效于: Action<格式规范(需要自带的变量类型)> 通知书名字. 相当于默认一种格式规范只能有一种格式规范名,不能有两个规范名字不一样但是格式却一模一样
    // public event Action<string> Event2;

    // 声明3: 针对(声明1格式规范:声明2通知书名字)的通知书发布/广播方法
    // 把"发布的方法A:发送名为'Event1'的通知书"这个行为包装成一个函数"Plan_A()",中需要自带一个message, 因为该方法A中包含了发送名为'Event1(message)'通知书的指令
    // (可以在"发布的方法A:"中同时包括点别的什么事, 也可以按顺序发不同的通知书)
    public void Plan_A(string message)
    {
        // 发送名叫"Event1"的通知书, 并遵循其格式规范带着一个message (广播出去,等待各位订阅者们各自响应. 这里有多个订阅者的话,按照订阅顺序执行)
        Event1?.Invoke(message);
        // 例如在发送通知书'Event1'后,记录执行A号方案的时间
        var startTime = DateTime.Now;
        Console.WriteLine($"[{startTime:HH:mm:ss}] 开始执行A号方案...");
    }
}

// 订阅者
public class Subscriber
{
    // 把"收到某个通知书后的响应1"这个行为包装成一个函数"Action1()"
    public void Action_1(string message)
    {
        Console.WriteLine($"收到消息: {message}");
    }
}

// 主程序
class Program
{
    static void Test_Main()
    {
        var publisher = new Publisher();
        var subscriber = new Subscriber();

        // 通知书规范:名为'Event1'的通知书 <-(注册)<- 订阅者A:收到某通知书后的响应1
        // (当有多方订阅者同时对一份通知书响应时: 通知书规范:名为'Event1'的通知书 <-(注册)<- 订阅者B:收到某通知书后的响应2)
        publisher.Event1 += subscriber.Action_1;
        // e.g., publisher.Event1 += subscriber2.Action_2

        // 通知书规范:执行A号方案(), 需要自带一个message -> 发送名为'Event1'的通知书,带着一个message 
        // ~~~ 
        // 自动: 收到'Event1'通知书后, 根据注册开始执行响应1 (来自上文)
        publisher.Plan_A("Hello World!");

        // 取消订阅
        publisher.Event1 -= subscriber.Action_1;

        // 再次触发（没有输出）
        publisher.Plan_A("这条消息不会显示");
    }
}