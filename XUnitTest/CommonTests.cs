using System;
using NewLife.Siemens.Messages;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using Xunit;

namespace XUnitTest;

public class CommonTests
{
    #region ErrorCode
    [Fact]
    public void ErrorCode_AllValues_AreDefined()
    {
        var values = Enum.GetValues(typeof(ErrorCode));
        Assert.Equal(9, values.Length);
    }

    [Fact]
    public void ErrorCode_CanCastToInt()
    {
        Assert.Equal(0, (Int32)ErrorCode.NoError);
        Assert.Equal(50, (Int32)ErrorCode.WriteData);
    }

    [Fact]
    public void ErrorCode_AllMembers_HaveExpectedValues()
    {
        Assert.Equal(1, (Int32)ErrorCode.WrongCPU_Type);
        Assert.Equal(2, (Int32)ErrorCode.ConnectionError);
        Assert.Equal(3, (Int32)ErrorCode.IPAddressNotAvailable);
        Assert.Equal(10, (Int32)ErrorCode.WrongVarFormat);
        Assert.Equal(11, (Int32)ErrorCode.WrongNumberReceivedBytes);
        Assert.Equal(20, (Int32)ErrorCode.SendData);
        Assert.Equal(30, (Int32)ErrorCode.ReadData);
    }

    [Fact]
    public void ErrorCode_Enum_ContainsKey()
    {
        Assert.True(Enum.IsDefined(typeof(ErrorCode), 0));
        Assert.True(Enum.IsDefined(typeof(ErrorCode), 50));
        Assert.False(Enum.IsDefined(typeof(ErrorCode), 99));
    }
    #endregion

    #region DataItem 属性
    [Fact]
    public void DataItem_Create_DefaultCount()
    {
        var item = DataItem.Create(DataType.DataBlock, VarType.Int, 1, 20);
        Assert.Equal(DataType.DataBlock, item.DataType);
        Assert.Equal(VarType.Int, item.VarType);
        Assert.Equal(1, item.DbNumber);
        Assert.Equal(20, item.StartByteAdr);
        Assert.Equal(-1, item.BitAdr);
        Assert.Equal(1, item.Count);
        Assert.Null(item.Value);
    }

    [Fact]
    public void DataItem_Create_WithCountAndBitAdr()
    {
        var item = DataItem.Create(DataType.DataBlock, VarType.Bit, 3, 10, 5, 4);
        Assert.Equal(VarType.Bit, item.VarType);
        Assert.Equal(3, item.DbNumber);
        Assert.Equal(10, item.StartByteAdr);
        Assert.Equal(5, item.BitAdr);
        Assert.Equal(4, item.Count);
    }

    [Fact]
    public void DataItem_Create_InputDataType()
    {
        var item = DataItem.Create(DataType.Input, VarType.Byte, 0, 5);
        Assert.Equal(DataType.Input, item.DataType);
        Assert.Equal(0, item.DbNumber);
    }

    [Fact]
    public void DataItem_Create_MemoryDataType()
    {
        var item = DataItem.Create(DataType.Memory, VarType.Word, 0, 8);
        Assert.Equal(DataType.Memory, item.DataType);
        Assert.Equal(VarType.Word, item.VarType);
    }

    [Fact]
    public void DataItem_Value_SetAndGet()
    {
        var item = DataItem.Create(DataType.DataBlock, VarType.Int, 1, 0);
        item.Value = (Int16)12345;
        Assert.Equal((Int16)12345, item.Value);
    }

    [Fact]
    public void DataItem_ToString_WithTransportSize()
    {
        var item = new DataItem { TransportSize = 0x04, Data = new Byte[] { 0xFF } };
        var str = item.ToString();
        Assert.NotEmpty(str);
    }

    [Fact]
    public void DataItem_ToString_WithCode()
    {
        var item = new DataItem { Code = ReadWriteErrorCode.Success };
        Assert.Contains("Success", item.ToString());
    }

    [Fact]
    public void DataItem_Default_NullData()
    {
        var item = new DataItem();
        Assert.Null(item.Data);
        Assert.Null(item.Value);
        Assert.Equal(0, item.DbNumber);
        Assert.Equal(0, item.StartByteAdr);
        Assert.Equal(-1, item.BitAdr);
        Assert.Equal(1, item.Count);
    }

    [Fact]
    public void DataItem_Code_Success_IsFF()
    {
        Assert.Equal((Byte)0xFF, (Byte)ReadWriteErrorCode.Success);
    }

    [Fact]
    public void DataItem_AllVarTypes_Definable()
    {
        foreach (VarType vt in Enum.GetValues(typeof(VarType)))
        {
            var item = DataItem.Create(DataType.DataBlock, vt, 1, 0);
            Assert.Equal(vt, item.VarType);
        }
    }
    #endregion
}
