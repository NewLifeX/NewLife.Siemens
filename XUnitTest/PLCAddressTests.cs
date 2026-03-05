using System;
using System.IO;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using Xunit;

namespace XUnitTest;

public class PLCAddressTests
{
    #region DB addresses
    [Fact]
    public void DB_DBD()
    {
        var addr = new PLCAddress("DB1.DBD32");

        Assert.Equal(DataType.DataBlock, addr.DataType);
        Assert.Equal(1, addr.DbNumber);
        Assert.Equal(32, addr.StartByte);
        Assert.Equal(VarType.DWord, addr.VarType);
        Assert.Equal(-1, addr.BitNumber);
    }

    [Fact]
    public void DB_DBX()
    {
        var addr = new PLCAddress("DB1.DBX5.0");
        Assert.Equal(DataType.DataBlock, addr.DataType);
        Assert.Equal(1, addr.DbNumber);
        Assert.Equal(5, addr.StartByte);
        Assert.Equal(VarType.Bit, addr.VarType);
        Assert.Equal(0, addr.BitNumber);
    }

    [Fact]
    public void DB_STRING()
    {
        var addr = new PLCAddress("DB1.STRING34.20");
        Assert.Equal(DataType.DataBlock, addr.DataType);
        Assert.Equal(1, addr.DbNumber);
        Assert.Equal(34, addr.StartByte);
        Assert.Equal(VarType.String, addr.VarType);
        Assert.Equal(20, addr.BitNumber);
    }

    [Fact]
    public void DB_DBB()
    {
        var addr = new PLCAddress("DB10.DBB100");
        Assert.Equal(DataType.DataBlock, addr.DataType);
        Assert.Equal(10, addr.DbNumber);
        Assert.Equal(100, addr.StartByte);
        Assert.Equal(VarType.Byte, addr.VarType);
        Assert.Equal(-1, addr.BitNumber);
    }

    [Fact]
    public void DB_DBW()
    {
        var addr = new PLCAddress("DB5.DBW20");
        Assert.Equal(DataType.DataBlock, addr.DataType);
        Assert.Equal(5, addr.DbNumber);
        Assert.Equal(20, addr.StartByte);
        Assert.Equal(VarType.Word, addr.VarType);
        Assert.Equal(-1, addr.BitNumber);
    }

    [Fact]
    public void DB_DBX_Bit7()
    {
        var addr = new PLCAddress("DB2.DBX0.7");
        Assert.Equal(DataType.DataBlock, addr.DataType);
        Assert.Equal(2, addr.DbNumber);
        Assert.Equal(0, addr.StartByte);
        Assert.Equal(VarType.Bit, addr.VarType);
        Assert.Equal(7, addr.BitNumber);
    }

    [Fact]
    public void DB_DBX_Bit8_Throws()
    {
        Assert.Throws<InvalidDataException>(() => new PLCAddress("DB1.DBX0.8"));
    }

    [Fact]
    public void DB_TooFewPeriods_Throws()
    {
        Assert.Throws<InvalidDataException>(() => new PLCAddress("DB1"));
    }

    [Fact]
    public void DB_InvalidSubtype_Throws()
    {
        Assert.Throws<InvalidDataException>(() => new PLCAddress("DB1.XXX0"));
    }
    #endregion

    #region Input addresses
    [Fact]
    public void Input_IB()
    {
        var addr = new PLCAddress("IB10");
        Assert.Equal(DataType.Input, addr.DataType);
        Assert.Equal(0, addr.DbNumber);
        Assert.Equal(10, addr.StartByte);
        Assert.Equal(VarType.Byte, addr.VarType);
    }

    [Fact]
    public void Input_EB()
    {
        var addr = new PLCAddress("EB5");
        Assert.Equal(DataType.Input, addr.DataType);
        Assert.Equal(0, addr.DbNumber);
        Assert.Equal(5, addr.StartByte);
        Assert.Equal(VarType.Byte, addr.VarType);
    }

    [Fact]
    public void Input_IW()
    {
        var addr = new PLCAddress("IW20");
        Assert.Equal(DataType.Input, addr.DataType);
        Assert.Equal(0, addr.DbNumber);
        Assert.Equal(20, addr.StartByte);
        Assert.Equal(VarType.Word, addr.VarType);
    }

    [Fact]
    public void Input_EW()
    {
        var addr = new PLCAddress("EW12");
        Assert.Equal(DataType.Input, addr.DataType);
        Assert.Equal(12, addr.StartByte);
        Assert.Equal(VarType.Word, addr.VarType);
    }

    [Fact]
    public void Input_ID()
    {
        var addr = new PLCAddress("ID0");
        Assert.Equal(DataType.Input, addr.DataType);
        Assert.Equal(0, addr.StartByte);
        Assert.Equal(VarType.DWord, addr.VarType);
    }

    [Fact]
    public void Input_ED()
    {
        var addr = new PLCAddress("ED100");
        Assert.Equal(DataType.Input, addr.DataType);
        Assert.Equal(100, addr.StartByte);
        Assert.Equal(VarType.DWord, addr.VarType);
    }
    #endregion

    #region Output addresses
    [Fact]
    public void Output_QB()
    {
        var addr = new PLCAddress("QB0");
        Assert.Equal(DataType.Output, addr.DataType);
        Assert.Equal(0, addr.StartByte);
        Assert.Equal(VarType.Byte, addr.VarType);
    }

    [Fact]
    public void Output_AB()
    {
        var addr = new PLCAddress("AB3");
        Assert.Equal(DataType.Output, addr.DataType);
        Assert.Equal(3, addr.StartByte);
        Assert.Equal(VarType.Byte, addr.VarType);
    }

    [Fact]
    public void Output_OB()
    {
        var addr = new PLCAddress("OB7");
        Assert.Equal(DataType.Output, addr.DataType);
        Assert.Equal(7, addr.StartByte);
        Assert.Equal(VarType.Byte, addr.VarType);
    }

    [Fact]
    public void Output_QW()
    {
        var addr = new PLCAddress("QW10");
        Assert.Equal(DataType.Output, addr.DataType);
        Assert.Equal(10, addr.StartByte);
        Assert.Equal(VarType.Word, addr.VarType);
    }

    [Fact]
    public void Output_AW()
    {
        var addr = new PLCAddress("AW2");
        Assert.Equal(DataType.Output, addr.DataType);
        Assert.Equal(2, addr.StartByte);
        Assert.Equal(VarType.Word, addr.VarType);
    }

    [Fact]
    public void Output_OW()
    {
        var addr = new PLCAddress("OW4");
        Assert.Equal(DataType.Output, addr.DataType);
        Assert.Equal(4, addr.StartByte);
        Assert.Equal(VarType.Word, addr.VarType);
    }

    [Fact]
    public void Output_QD()
    {
        var addr = new PLCAddress("QD0");
        Assert.Equal(DataType.Output, addr.DataType);
        Assert.Equal(0, addr.StartByte);
        Assert.Equal(VarType.DWord, addr.VarType);
    }

    [Fact]
    public void Output_AD()
    {
        var addr = new PLCAddress("AD8");
        Assert.Equal(DataType.Output, addr.DataType);
        Assert.Equal(8, addr.StartByte);
        Assert.Equal(VarType.DWord, addr.VarType);
    }

    [Fact]
    public void Output_OD()
    {
        var addr = new PLCAddress("OD16");
        Assert.Equal(DataType.Output, addr.DataType);
        Assert.Equal(16, addr.StartByte);
        Assert.Equal(VarType.DWord, addr.VarType);
    }
    #endregion

    #region Memory addresses
    [Fact]
    public void Memory_MB()
    {
        var addr = new PLCAddress("MB0");
        Assert.Equal(DataType.Memory, addr.DataType);
        Assert.Equal(0, addr.StartByte);
        Assert.Equal(VarType.Byte, addr.VarType);
    }

    [Fact]
    public void Memory_MW()
    {
        var addr = new PLCAddress("MW100");
        Assert.Equal(DataType.Memory, addr.DataType);
        Assert.Equal(100, addr.StartByte);
        Assert.Equal(VarType.Word, addr.VarType);
    }

    [Fact]
    public void Memory_MD()
    {
        var addr = new PLCAddress("MD50");
        Assert.Equal(DataType.Memory, addr.DataType);
        Assert.Equal(50, addr.StartByte);
        Assert.Equal(VarType.DWord, addr.VarType);
    }
    #endregion

    #region Bit addresses
    [Fact]
    public void Bit_E()
    {
        var addr = new PLCAddress("E0.0");
        Assert.Equal(DataType.Input, addr.DataType);
        Assert.Equal(VarType.Bit, addr.VarType);
        Assert.Equal(0, addr.StartByte);
        Assert.Equal(0, addr.BitNumber);
    }

    [Fact]
    public void Bit_I()
    {
        var addr = new PLCAddress("I1.7");
        Assert.Equal(DataType.Input, addr.DataType);
        Assert.Equal(VarType.Bit, addr.VarType);
        Assert.Equal(1, addr.StartByte);
        Assert.Equal(7, addr.BitNumber);
    }

    [Fact]
    public void Bit_Q()
    {
        var addr = new PLCAddress("Q2.3");
        Assert.Equal(DataType.Output, addr.DataType);
        Assert.Equal(VarType.Bit, addr.VarType);
        Assert.Equal(2, addr.StartByte);
        Assert.Equal(3, addr.BitNumber);
    }

    [Fact]
    public void Bit_A()
    {
        var addr = new PLCAddress("A3.5");
        Assert.Equal(DataType.Output, addr.DataType);
        Assert.Equal(VarType.Bit, addr.VarType);
        Assert.Equal(3, addr.StartByte);
        Assert.Equal(5, addr.BitNumber);
    }

    [Fact]
    public void Bit_O()
    {
        var addr = new PLCAddress("O4.6");
        Assert.Equal(DataType.Output, addr.DataType);
        Assert.Equal(VarType.Bit, addr.VarType);
        Assert.Equal(4, addr.StartByte);
        Assert.Equal(6, addr.BitNumber);
    }

    [Fact]
    public void Bit_M()
    {
        var addr = new PLCAddress("M5.1");
        Assert.Equal(DataType.Memory, addr.DataType);
        Assert.Equal(VarType.Bit, addr.VarType);
        Assert.Equal(5, addr.StartByte);
        Assert.Equal(1, addr.BitNumber);
    }

    [Fact]
    public void Bit_Over7_Throws()
    {
        Assert.Throws<InvalidDataException>(() => new PLCAddress("M0.8"));
    }

    [Fact]
    public void Bit_NoPeriod_Throws()
    {
        Assert.Throws<InvalidDataException>(() => new PLCAddress("M0"));
    }
    #endregion

    #region Timer and Counter
    [Fact]
    public void Timer_T10()
    {
        var addr = new PLCAddress("T10");
        Assert.Equal(DataType.Timer, addr.DataType);
        Assert.Equal(0, addr.DbNumber);
        Assert.Equal(10, addr.StartByte);
        Assert.Equal(VarType.Timer, addr.VarType);
    }

    [Fact]
    public void Counter_Z5()
    {
        var addr = new PLCAddress("Z5");
        Assert.Equal(DataType.Counter, addr.DataType);
        Assert.Equal(0, addr.DbNumber);
        Assert.Equal(5, addr.StartByte);
        Assert.Equal(VarType.Counter, addr.VarType);
    }

    [Fact]
    public void Counter_C8()
    {
        var addr = new PLCAddress("C8");
        Assert.Equal(DataType.Counter, addr.DataType);
        Assert.Equal(0, addr.DbNumber);
        Assert.Equal(8, addr.StartByte);
        Assert.Equal(VarType.Counter, addr.VarType);
    }
    #endregion

    #region Error cases
    [Fact]
    public void InvalidPrefix_Throws()
    {
        Assert.Throws<InvalidDataException>(() => new PLCAddress("X0.0"));
    }
    #endregion
}
