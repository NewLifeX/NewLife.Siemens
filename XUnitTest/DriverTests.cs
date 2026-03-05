using System;
using NewLife.Siemens.Drivers;
using NewLife.Siemens.Models;
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
}

/// <summary>Simple IPoint stub for testing GetAddress</summary>
internal class TestPoint : NewLife.IoT.ThingModels.IPoint
{
    public String Name { get; set; }
    public String Address { get; set; }
    public String Type { get; set; }
    public Int32 Length { get; set; }
}
