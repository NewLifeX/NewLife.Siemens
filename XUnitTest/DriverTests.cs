using System;
using NewLife.Siemens.Drivers;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
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

/// <summary>Simple IPoint stub for testing GetAddress</summary>
internal class TestPoint : NewLife.IoT.ThingModels.IPoint
{
    public String Name { get; set; }
    public String Address { get; set; }
    public String Type { get; set; }
    public Int32 Length { get; set; }
}
