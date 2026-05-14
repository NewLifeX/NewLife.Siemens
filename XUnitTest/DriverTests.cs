using System;
using System.Collections.Generic;
using System.Threading;
using NewLife.Log;
using NewLife.Siemens.Drivers;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using NewLife.UnitTest;
using Xunit;

namespace XUnitTest;

public class DriverTests
{
    #region SiemensParameter
    [Fact]
    public void SiemensParameter_DefaultValues()
    {
        var pm = new SiemensParameter();
        Assert.Null(pm.Address);
        Assert.Equal(CpuType.S7200, pm.CpuType);
        Assert.Equal(0, pm.Rack);
        Assert.Equal(0, pm.Slot);
    }

    [Fact]
    public void SiemensParameter_SetProperties()
    {
        var pm = new SiemensParameter
        {
            Address = "192.168.0.1:102",
            CpuType = CpuType.S71200,
            Rack = 1,
            Slot = 2
        };
        Assert.Equal("192.168.0.1:102", pm.Address);
        Assert.Equal(CpuType.S71200, pm.CpuType);
        Assert.Equal(1, pm.Rack);
        Assert.Equal(2, pm.Slot);
    }
    #endregion

    #region SiemensNode
    [Fact]
    public void SiemensNode_Properties()
    {
        var driver = new SiemensS7Driver();
        var pm = new SiemensParameter { Address = "10.0.0.1:102" };

        var node = new SiemensNode
        {
            Address = "10.0.0.1:102",
            Driver = driver,
            Parameter = pm
        };

        Assert.Equal("10.0.0.1:102", node.Address);
        Assert.Same(driver, node.Driver);
        Assert.Same(pm, node.Parameter);
        Assert.Null(node.Device);
    }
    #endregion

    #region SiemensS7Driver
    [Fact]
    public void SiemensS7Driver_GetAddress_Plain()
    {
        var driver = new SiemensS7Driver();
        var point = new TestPoint { Address = "DB1.DBW10" };
        var addr = driver.GetAddress(point);
        Assert.Equal("DB1.DBW10", addr);
    }

    [Fact]
    public void SiemensS7Driver_GetAddress_WithColon()
    {
        var driver = new SiemensS7Driver();
        var point = new TestPoint { Address = "DB1.DBW10:2" };
        var addr = driver.GetAddress(point);
        Assert.Equal("DB1.DBW10", addr);
    }

    [Fact]
    public void SiemensS7Driver_GetAddress_NullThrows()
    {
        var driver = new SiemensS7Driver();
        var point = new TestPoint { Address = null };
        Assert.Throws<ArgumentException>(() => driver.GetAddress(point));
    }

    [Fact]
    public void SiemensS7Driver_GetAddress_EmptyThrows()
    {
        var driver = new SiemensS7Driver();
        var point = new TestPoint { Address = "" };
        Assert.Throws<ArgumentException>(() => driver.GetAddress(point));
    }
    #endregion

    #region S7Client
    [Fact]
    public void S7Client_Constructor_SetsProperties()
    {
        using var client = new S7Client(CpuType.S71200, "192.168.1.1", 102, 1, 2);
        Assert.Equal(CpuType.S71200, client.CPU);
        Assert.Equal("192.168.1.1", client.IP);
        Assert.Equal(102, client.Port);
        Assert.Equal(1, client.Rack);
        Assert.Equal(2, client.Slot);
        Assert.Equal(5000, client.Timeout);
        Assert.Equal(1024, client.MaxPDUSize);
    }

    [Fact]
    public void S7Client_Constructor_DefaultRackSlot()
    {
        using var client = new S7Client(CpuType.S7200Smart, "10.0.0.1", 102);
        Assert.Equal(CpuType.S7200Smart, client.CPU);
        Assert.Equal(0, client.Rack);
        Assert.Equal(0, client.Slot);
    }

    [Fact]
    public void S7Client_Constructor_ZeroPort_KeepsDefault()
    {
        using var client = new S7Client(CpuType.S7300, "10.0.0.1", 0);
        Assert.Equal(102, client.Port);
    }

    [Fact]
    public void S7Client_Close_DoesNotThrow()
    {
        using var client = new S7Client(CpuType.S7300, "10.0.0.1", 102);
        client.Close();
        // Calling close again should not throw
        client.Close();
    }
    #endregion
}

/// <summary>SiemensS7Driver + S7Server 集成测试（无需真实 PLC 硬件）</summary>
[TestCaseOrderer("NewLife.UnitTest.PriorityOrderer", "NewLife.UnitTest")]
public class DriverIntegrationTests
{
    private const Int32 TestPort = 10238;
    private static S7Server? _server;
    private static SiemensS7Driver? _driver;
    private static TestDevice? _device;
    private static SiemensNode? _node;

    #region 01 — 启动服务并初始化驱动
    [TestOrder(1)]
    [Fact]
    public void Driver_StartServer()
    {
        var server = new S7Server { Port = TestPort };
        // 预置 DB1.DBW0 = 500, DB1.DBW2 = -200
        server.SetValue("DB1.DBW0", (Int16)500);
        server.SetValue("DB1.DBW2", (Int16)(-200));
        server.SetValue("DB1.DBD10", 9.81f);
        server.Start();
        _server = server;

        Thread.Sleep(100);
        Assert.True(server.Active);
    }

    [TestOrder(2)]
    [Fact]
    public void Driver_Open()
    {
        var driver = new SiemensS7Driver();
        var pm = new SiemensParameter
        {
            Address = $"127.0.0.1:{TestPort}",
            CpuType = CpuType.S7200,
        };
        var device = new TestDevice();
        var node = (SiemensNode)driver.Open(device, pm);

        _driver = driver;
        _device = device;
        _node = node;

        Assert.NotNull(node);
        Assert.Equal($"127.0.0.1:{TestPort}", node.Address);
    }
    #endregion

    #region 02 — 读取操作
    [TestOrder(10)]
    [Fact]
    public void Driver_Read_RawBytes()
    {
        Assert.NotNull(_driver);
        Assert.NotNull(_node);
        // Type 为空 → 返回原始 Byte[]（大端）
        var points = new[] { new TestPoint { Name = "speed", Address = "DB1.DBW0", Type = null, Length = 2 } };
        var result = _driver!.Read(_node!, points);

        Assert.True(result.ContainsKey("speed"));
        var raw = result["speed"] as Byte[];
        Assert.NotNull(raw);
        // 500 = 0x01F4，大端 → {0x01, 0xF4}
        Assert.Equal(2, raw!.Length);
        // 解析为 Int16（大端）
        var val = (Int16)((raw[0] << 8) | raw[1]);
        Assert.Equal(500, val);
    }

    [TestOrder(11)]
    [Fact]
    public void Driver_Read_MultiplePoints()
    {
        Assert.NotNull(_driver);
        Assert.NotNull(_node);
        // Type 为空 → 返回 Byte[]
        var points = new[]
        {
            new TestPoint { Name = "p1", Address = "DB1.DBW0", Type = null, Length = 2 },
            new TestPoint { Name = "p2", Address = "DB1.DBW2", Type = null, Length = 2 },
        };
        var result = _driver!.Read(_node!, points);
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("p1"));
        Assert.True(result.ContainsKey("p2"));
    }

    [TestOrder(12)]
    [Fact]
    public void Driver_Read_Float_RawBytes()
    {
        Assert.NotNull(_driver);
        Assert.NotNull(_node);
        // Type 为空 → 返回原始 4 字节
        var points = new[] { new TestPoint { Name = "g", Address = "DB1.DBD10", Type = null, Length = 4 } };
        var result = _driver!.Read(_node!, points);
        Assert.True(result.ContainsKey("g"));
        var raw = result["g"] as Byte[];
        Assert.Equal(4, raw!.Length);
    }

    [TestOrder(13)]
    [Fact]
    public void Driver_Read_EmptyPoints_Returns_Empty()
    {
        Assert.NotNull(_driver);
        Assert.NotNull(_node);
        var result = _driver!.Read(_node!, []);
        Assert.Empty(result);
    }

    [TestOrder(14)]
    [Fact]
    public void Driver_Read_NullPoints_Returns_Empty()
    {
        Assert.NotNull(_driver);
        Assert.NotNull(_node);
        var result = _driver!.Read(_node!, null!);
        Assert.Empty(result);
    }
    #endregion

    #region 03 — 写入操作
    [TestOrder(20)]
    [Fact]
    public void Driver_Write_Int16_And_Verify()
    {
        Assert.NotNull(_driver);
        Assert.NotNull(_node);
        var writePoint = new TestPoint { Name = "val", Address = "DB1.DBW20", Type = "short", Length = 2 };
        var readPoint  = new TestPoint { Name = "val", Address = "DB1.DBW20", Type = null,    Length = 2 };

        // 写入字节数组（大端）
        var bytes = new Byte[] { 0x03, 0xE8 }; // 1000 big-endian
        _driver!.Write(_node!, writePoint, bytes);

        // 读回验证（Type=null 返回原始 Byte[]）
        var result = _driver.Read(_node!, [readPoint]);
        var raw = result["val"] as Byte[];
        Assert.NotNull(raw);
        Assert.Equal(2, raw!.Length);
        // 大端 {0x03, 0xE8} → 1000
        var val = (Int16)((raw[0] << 8) | raw[1]);
        Assert.Equal(1000, val);
    }

    [TestOrder(21)]
    [Fact]
    public void Driver_Write_Float_TypeString()
    {
        Assert.NotNull(_driver);
        Assert.NotNull(_node);
        var writePoint = new TestPoint { Name = "temp", Address = "DB1.DBD40", Type = "float", Length = 4 };
        var readPoint  = new TestPoint { Name = "temp", Address = "DB1.DBD40", Type = null,    Length = 4 };
        _driver!.Write(_node!, writePoint, "3.14");

        var result = _driver.Read(_node!, [readPoint]);
        var raw = result["temp"] as Byte[];
        Assert.NotNull(raw);
        Assert.Equal(4, raw!.Length);
    }

    [TestOrder(22)]
    [Fact]
    public void Driver_Write_NullValue_Returns_Null_Or_OK()
    {
        Assert.NotNull(_driver);
        Assert.NotNull(_node);
        var point = new TestPoint { Name = "x", Address = "DB1.DBW30", Type = "short", Length = 2 };
        // null value：应返回 null（无操作）
        var result = _driver!.Write(_node!, point, null);
        Assert.Null(result);
    }
    #endregion

    #region 04 — 关闭
    [TestOrder(90)]
    [Fact]
    public void Driver_Close()
    {
        Assert.NotNull(_driver);
        Assert.NotNull(_node);
        _driver!.Close(_node!);
        // Close 后不应抛异常
    }

    [TestOrder(91)]
    [Fact]
    public void Driver_StopServer()
    {
        Assert.NotNull(_server);
        _server!.Stop("done");
        _server = null;
    }
    #endregion
}

/// <summary>Simple IPoint stub for testing GetAddress</summary>
internal class TestPoint : NewLife.IoT.ThingModels.IPoint
{
    public String Name { get; set; }
    public String Address { get; set; }
    public String Type { get; set; }
    public Int32 Length { get; set; }
}

/// <summary>Simple IDevice stub for driver integration tests</summary>
internal class TestDevice : NewLife.IoT.IDevice
{
    public String? Code { get; set; }
    public IDictionary<String, Object>? Properties { get; set; }
    public NewLife.IoT.ThingSpecification.ThingSpec? Specification { get; set; }
    public NewLife.IoT.ThingModels.IPoint[]? Points { get; set; }
    public IDictionary<String, Delegate>? Services { get; set; }
    public System.Threading.Tasks.Task Start() => System.Threading.Tasks.Task.CompletedTask;
    public void Stop() { }
    public NewLife.IoT.Models.IDeviceInfo[] SetOnline(NewLife.IoT.Models.IDeviceInfo[] devices) => [];
    public NewLife.IoT.Models.IDeviceInfo[] SetOffline(String[] codes) => [];
    public void PostProperty() { }
    public void SetProperty(String name, Object? value) { }
    public Boolean AddData(String name, String value) => true;
    public Boolean WriteEvent(String type, String name, String remark) => true;
    public void RegisterService(String name, Delegate handler) { }
}
