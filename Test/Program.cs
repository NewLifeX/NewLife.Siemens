using System;
using System.Threading;
using NewLife;
using NewLife.IoT;
using NewLife.IoT.Drivers;
using NewLife.IoT.ThingModels;
using NewLife.Omron.Drivers;
using NewLife.Siemens.Drivers;
using NewLife.Siemens.Models;

Console.WriteLine("服务端地址默认为：127.0.0.1:102，保持默认请回车开始连接，否则请输入服务端地址：");
var address = Console.ReadLine();

var point = new Point
{
    Name = "污泥泵停止时间",
    Address = "DB1.DBD60", // "M100",
    Type = "long",
    Length = 4 //data.Length
};

var i = point.GetNetType();


if (address == null || address == "") address = "127.0.0.1:102";

var driver = new SiemensS7Driver();
var pm = new SiemensParameter
{
    Address = address,
    CpuType = CpuType.S7200Smart,
    Rack = 0,
    Slot = 2,
};
var node = driver.Open(null, pm);

//// 测试打开两个通道
//node = driver.Open(null, pm);

Console.WriteLine($"连接成功=>{address}！");

Console.WriteLine($"读写模式输入1，循环读输入2：");

var mode = Console.ReadLine();

var str = "0";

if (mode == "1")
{
    Console.WriteLine("请输入整数值，按q退出：");
    str = Console.ReadLine();
}



//var point2 = new Point
//{
//    Name = "test2",
//    Address = "DB1.DBX1128.0", // "M100",
//    Type = "Int32",
//    Length = 4 //data.Length
//};

do
{
    if (mode == "1")
    {
        // 写入
        var data = BitConverter.GetBytes(Int32.Parse(str));

        var res = driver.Write(node, point, data);

        // 读取
        var dic = driver.Read(node, new[] { point });
        var data1 = dic[point.Name] as Byte[];

        Console.WriteLine($"读取结果：{BitConverter.ToInt32(data1)}");
        Console.WriteLine($"");
        Console.WriteLine("请输入整数值，按q退出：");
    }
    else
    {
        // 读取
        var dic = driver.Read(node, new[] { point });
        var data1 = dic[point.Name] as Byte[];
        //var res = BitConverter.ToInt32(data1);
        var res = data1.Swap(true, false).ToInt();
        Console.WriteLine($"读取结果：{res}");
        Console.WriteLine($"");
        Thread.Sleep(1000);
    }
} while (
(mode == "1" && (str = Console.ReadLine()) != "q")
|| mode == "2");

// 断开连接
//driver.Close(node);
driver.Close(node);


public class Point : IPoint
{
    public String Name { get; set; }
    public String Address { get; set; }
    public String Type { get; set; }
    public Int32 Length { get; set; }
}