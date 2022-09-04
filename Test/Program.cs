using System;
using NewLife.IoT.Drivers;
using NewLife.IoT.Protocols;
using NewLife.IoT.ThingModels;
using NewLife.Omron.Drivers;
using NewLife.Siemens.Drivers;
using NewLife.Siemens.Models;

Console.WriteLine("服务端地址默认为：127.0.0.1:102，保持默认请回车开始连接，否则请输入服务端地址：");
var address = Console.ReadLine();

if (address == null || address == "") address = "127.0.0.1:102";

var driver = new SiemensS7Driver();
var pm = new SiemensParameter
{
    Address = address,
    CpuType = CpuType.S7400,
    Rack = 0,
    Slot = 3,
};
var node = driver.Open(null, pm);

// 测试打开两个通道
node = driver.Open(null, pm);

Console.WriteLine($"连接成功=>{address}！");

Console.WriteLine("请输入整数值，按q退出：");

var str = Console.ReadLine();

do
{
    // 写入
    var data = BitConverter.GetBytes(Int32.Parse(str));
    var point = new Point
    {
        Name = "test",
        Address = "M100",
        Type = "Int32",
        Length = data.Length
    };

    var res = driver.Write(node, point, data);

    // 读取
    var dic = driver.Read(node, new[] { point });
    var data1 = dic[point.Name] as Byte[];

    Console.WriteLine($"读取结果：{BitConverter.ToInt32(data1)}");
    Console.WriteLine($"");
    Console.WriteLine("请输入整数值，按q退出：");

} while ((str = Console.ReadLine()) != "q");

// 断开连接
driver.Close(node);
driver.Close(node);


public class Point : IPoint
{
    public String Name { get; set; }
    public String Address { get; set; }
    public String Type { get; set; }
    public Int32 Length { get; set; }
}