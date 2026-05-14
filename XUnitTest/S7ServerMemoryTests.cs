using System;
using NewLife.Siemens.Messages;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using NewLife.UnitTest;
using Xunit;

namespace XUnitTest;

/// <summary>S7Server 内存区域存储单元测试（无网络）</summary>
public class S7ServerMemoryTests
{
    #region GetMemory 区域访问
    [Fact]
    public void GetMemory_DB_LazyInit()
    {
        var server = new S7Server();

        var mem1 = server.GetMemory(DataType.DataBlock, 1);
        var mem2 = server.GetMemory(DataType.DataBlock, 1);
        var mem3 = server.GetMemory(DataType.DataBlock, 2);

        Assert.NotNull(mem1);
        Assert.Equal(65536, mem1.Length);
        Assert.Same(mem1, mem2);      // 同一 DB 返回同一对象
        Assert.NotSame(mem1, mem3);   // 不同 DB 返回不同对象
    }

    [Fact]
    public void GetMemory_AllAreas_HaveCorrectLength()
    {
        var server = new S7Server();

        Assert.Equal(65536, server.GetMemory(DataType.Memory).Length);
        Assert.Equal(65536, server.GetMemory(DataType.Input).Length);
        Assert.Equal(65536, server.GetMemory(DataType.Output).Length);
        Assert.Equal(512, server.GetMemory(DataType.Timer).Length);
        Assert.Equal(512, server.GetMemory(DataType.Counter).Length);
    }

    [Fact]
    public void GetMemory_InvalidArea_Throws()
    {
        var server = new S7Server();
        Assert.Throws<ArgumentException>(() => server.GetMemory((DataType)0xFF));
    }
    #endregion

    #region SetValue 字节数组
    [Fact]
    public void SetValue_DB_Word_TwoBytes()
    {
        var server = new S7Server();
        server.SetValue("DB1.DBW0", new Byte[] { 0x03, 0xE8 }); // 1000 big-endian

        var mem = server.GetMemory(DataType.DataBlock, 1);
        Assert.Equal(0x03, mem[0]);
        Assert.Equal(0xE8, mem[1]);
    }

    [Fact]
    public void SetValue_DB_DWord_OffsetCorrect()
    {
        var server = new S7Server();
        server.SetValue("DB1.DBD10", new Byte[] { 0x40, 0x48, 0xF5, 0xC3 }); // 3.14f big-endian

        var mem = server.GetMemory(DataType.DataBlock, 1);
        Assert.Equal(0x40, mem[10]);
        Assert.Equal(0x48, mem[11]);
        Assert.Equal(0xF5, mem[12]);
        Assert.Equal(0xC3, mem[13]);
    }

    [Fact]
    public void SetValue_Memory_Byte()
    {
        var server = new S7Server();
        server.SetValue("MB100", new Byte[] { 0x42 });

        var mem = server.GetMemory(DataType.Memory);
        Assert.Equal(0x42, mem[100]);
    }

    [Fact]
    public void SetValue_Input_Byte()
    {
        var server = new S7Server();
        server.SetValue("IB5", new Byte[] { 0xAB });

        var mem = server.GetMemory(DataType.Input);
        Assert.Equal(0xAB, mem[5]);
    }

    [Fact]
    public void SetValue_Output_Byte()
    {
        var server = new S7Server();
        server.SetValue("QB3", new Byte[] { 0xCD });

        var mem = server.GetMemory(DataType.Output);
        Assert.Equal(0xCD, mem[3]);
    }
    #endregion

    #region SetValue 类型化重载
    [Fact]
    public void SetValue_Int16_BigEndian()
    {
        var server = new S7Server();
        server.SetValue("DB1.DBW0", (Int16)1000);

        var mem = server.GetMemory(DataType.DataBlock, 1);
        Assert.Equal(0x03, mem[0]); // 1000 = 0x03E8
        Assert.Equal(0xE8, mem[1]);
    }

    [Fact]
    public void SetValue_Int16_Negative()
    {
        var server = new S7Server();
        server.SetValue("DB1.DBW4", (Int16)(-1));

        var mem = server.GetMemory(DataType.DataBlock, 1);
        Assert.Equal(0xFF, mem[4]);
        Assert.Equal(0xFF, mem[5]);
    }

    [Fact]
    public void SetValue_Int32_BigEndian()
    {
        var server = new S7Server();
        server.SetValue("DB1.DBD0", 100000);

        var mem = server.GetMemory(DataType.DataBlock, 1);
        // 100000 = 0x000186A0
        Assert.Equal(0x00, mem[0]);
        Assert.Equal(0x01, mem[1]);
        Assert.Equal(0x86, mem[2]);
        Assert.Equal(0xA0, mem[3]);
    }

    [Fact]
    public void SetValue_Single_BigEndian()
    {
        var server = new S7Server();
        server.SetValue("DB1.DBD0", 3.14f);

        var mem = server.GetMemory(DataType.DataBlock, 1);
        // 3.14f = 0x4048F5C3
        Assert.Equal(0x40, mem[0]);
        Assert.Equal(0x48, mem[1]);
        Assert.Equal(0xF5, mem[2]);
        Assert.Equal(0xC3, mem[3]);
    }
    #endregion

    #region SetValue 位操作
    [Fact]
    public void SetValue_Bit_SetTrue()
    {
        var server = new S7Server();
        var mem = server.GetMemory(DataType.DataBlock, 1);
        mem[0] = 0x00; // 先清零

        server.SetValue("DB1.DBX0.0", new Byte[] { 1 });

        Assert.Equal(0x01, mem[0]);
    }

    [Fact]
    public void SetValue_Bit3_SetTrue_OthersBits_Unchanged()
    {
        var server = new S7Server();
        var mem = server.GetMemory(DataType.DataBlock, 1);
        mem[0] = 0x00;

        server.SetValue("DB1.DBX0.3", new Byte[] { 1 });

        Assert.Equal(0x08, mem[0]); // bit3 = 0b00001000
    }

    [Fact]
    public void SetValue_Bit_ClearFalse()
    {
        var server = new S7Server();
        var mem = server.GetMemory(DataType.DataBlock, 1);
        mem[0] = 0xFF; // 全置位

        server.SetValue("DB1.DBX0.0", new Byte[] { 0 });

        Assert.Equal(0xFE, mem[0]); // bit0 清零
    }
    #endregion

    #region DataItem.Create 工厂方法
    [Fact]
    public void DataItem_Create_SetsAllProperties()
    {
        var item = DataItem.Create(DataType.DataBlock, VarType.Word, 1, 10, -1, 3);

        Assert.Equal(DataType.DataBlock, item.DataType);
        Assert.Equal(VarType.Word, item.VarType);
        Assert.Equal(1, item.DbNumber);
        Assert.Equal(10, item.StartByteAdr);
        Assert.Equal(-1, item.BitAdr);
        Assert.Equal(3, item.Count);
        Assert.Null(item.Value);
    }

    [Fact]
    public void DataItem_DefaultValues()
    {
        var item = new DataItem();

        Assert.Equal(ReadWriteErrorCode.Reserved, item.Code);
        Assert.Equal(0, item.TransportSize);
        Assert.Null(item.Data);
        Assert.Equal(-1, item.BitAdr);
        Assert.Equal(1, item.Count);
    }
    #endregion
}
