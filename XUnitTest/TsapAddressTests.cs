using System;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using Xunit;

namespace XUnitTest;

public class TsapAddressTests
{
    #region Constructor
    [Fact]
    public void Constructor_SetsProperties()
    {
        var tsap = new TsapAddress(0x1000, 0x0300);
        Assert.Equal((UInt16)0x1000, tsap.Local);
        Assert.Equal((UInt16)0x0300, tsap.Remote);
    }
    #endregion

    #region GetDefaultTsapPair per CpuType
    [Fact]
    public void GetDefaultTsapPair_S7200()
    {
        var tsap = TsapAddress.GetDefaultTsapPair(CpuType.S7200, 0, 0);
        Assert.Equal((UInt16)0x1000, tsap.Local);
        Assert.Equal((UInt16)0x1001, tsap.Remote);
    }

    [Fact]
    public void GetDefaultTsapPair_Logo0BA8()
    {
        var tsap = TsapAddress.GetDefaultTsapPair(CpuType.Logo0BA8, 0, 0);
        Assert.Equal((UInt16)0x0100, tsap.Local);
        Assert.Equal((UInt16)0x0102, tsap.Remote);
    }

    [Fact]
    public void GetDefaultTsapPair_S7200Smart_Rack0_Slot0()
    {
        var tsap = TsapAddress.GetDefaultTsapPair(CpuType.S7200Smart, 0, 0);
        Assert.Equal((UInt16)0x1000, tsap.Local);
        // remote = 0x03 << 8 | (0 << 5 | 0) = 0x0300
        Assert.Equal((UInt16)0x0300, tsap.Remote);
    }

    [Fact]
    public void GetDefaultTsapPair_S7200Smart_Rack0_Slot1()
    {
        var tsap = TsapAddress.GetDefaultTsapPair(CpuType.S7200Smart, 0, 1);
        Assert.Equal((UInt16)0x1000, tsap.Local);
        // remote = 0x03 << 8 | (0 << 5 | 1) = 0x0301
        Assert.Equal((UInt16)0x0301, tsap.Remote);
    }

    [Fact]
    public void GetDefaultTsapPair_S7300_Rack0_Slot2()
    {
        var tsap = TsapAddress.GetDefaultTsapPair(CpuType.S7300, 0, 2);
        Assert.Equal((UInt16)0x0100, tsap.Local);
        // remote = 0x03 << 8 | (0 << 5 | 2) = 0x0302
        Assert.Equal((UInt16)0x0302, tsap.Remote);
    }

    [Fact]
    public void GetDefaultTsapPair_S7400_Rack0_Slot3()
    {
        var tsap = TsapAddress.GetDefaultTsapPair(CpuType.S7400, 0, 3);
        Assert.Equal((UInt16)0x0100, tsap.Local);
        Assert.Equal((UInt16)0x0303, tsap.Remote);
    }

    [Fact]
    public void GetDefaultTsapPair_S71200_Rack0_Slot0()
    {
        var tsap = TsapAddress.GetDefaultTsapPair(CpuType.S71200, 0, 0);
        Assert.Equal((UInt16)0x0100, tsap.Local);
        Assert.Equal((UInt16)0x0300, tsap.Remote);
    }

    [Fact]
    public void GetDefaultTsapPair_S71500_Rack0_Slot0()
    {
        var tsap = TsapAddress.GetDefaultTsapPair(CpuType.S71500, 0, 0);
        Assert.Equal((UInt16)0x0100, tsap.Local);
        Assert.Equal((UInt16)0x0300, tsap.Remote);
    }
    #endregion

    #region Rack/Slot encoding
    [Fact]
    public void GetDefaultTsapPair_Rack1_Slot0()
    {
        var tsap = TsapAddress.GetDefaultTsapPair(CpuType.S71200, 1, 0);
        // remote = 0x03 << 8 | (1 << 5 | 0) = 0x0320
        Assert.Equal((UInt16)0x0320, tsap.Remote);
    }

    [Fact]
    public void GetDefaultTsapPair_Rack2_Slot3()
    {
        var tsap = TsapAddress.GetDefaultTsapPair(CpuType.S71500, 2, 3);
        // remote = 0x03 << 8 | (2 << 5 | 3) = 0x0300 | 0x43 = 0x0343
        Assert.Equal((UInt16)0x0343, tsap.Remote);
    }

    [Fact]
    public void GetDefaultTsapPair_Rack15_Slot15()
    {
        var tsap = TsapAddress.GetDefaultTsapPair(CpuType.S7300, 15, 15);
        // remote = 0x03 << 8 | (byte)((15 << 5) | 15) = 0x0300 | (byte)(480|15) = 0x0300 | (byte)495
        // (byte)495 = 495 - 256 = 239 = 0xEF
        Assert.Equal((UInt16)0x03EF, tsap.Remote);
    }
    #endregion

    #region Boundary validation
    [Fact]
    public void GetDefaultTsapPair_RackNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TsapAddress.GetDefaultTsapPair(CpuType.S7300, -1, 0));
    }

    [Fact]
    public void GetDefaultTsapPair_RackOver15_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TsapAddress.GetDefaultTsapPair(CpuType.S7300, 16, 0));
    }

    [Fact]
    public void GetDefaultTsapPair_SlotNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TsapAddress.GetDefaultTsapPair(CpuType.S7300, 0, -1));
    }

    [Fact]
    public void GetDefaultTsapPair_SlotOver15_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TsapAddress.GetDefaultTsapPair(CpuType.S7300, 0, 16));
    }
    #endregion

    #region S7200/Logo ignore rack/slot
    [Fact]
    public void GetDefaultTsapPair_S7200_IgnoresRackSlot()
    {
        var tsap = TsapAddress.GetDefaultTsapPair(CpuType.S7200, 1, 2);
        Assert.Equal((UInt16)0x1000, tsap.Local);
        Assert.Equal((UInt16)0x1001, tsap.Remote);
    }

    [Fact]
    public void GetDefaultTsapPair_Logo0BA8_IgnoresRackSlot()
    {
        var tsap = TsapAddress.GetDefaultTsapPair(CpuType.Logo0BA8, 3, 4);
        Assert.Equal((UInt16)0x0100, tsap.Local);
        Assert.Equal((UInt16)0x0102, tsap.Remote);
    }
    #endregion
}
