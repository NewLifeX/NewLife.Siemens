using System;
using System.Threading;
using NewLife.Log;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using NewLife.UnitTest;
using Xunit;

namespace XUnitTest;

/// <summary>S7Client 高层泛型读写 API 单元测试</summary>
[TestCaseOrderer("NewLife.UnitTest.PriorityOrderer", "NewLife.UnitTest")]
public class S7HighLevelTests
{
    private static S7Server? _server;
    private static S7Client? _client;
    private const Int32 TestPort = 10232;

    #region GetVarTypeByteSize 纯逻辑测试（无网络）
    [Fact]
    public void GetVarTypeByteSize_BitAndByte()
    {
        Assert.Equal(1, S7Client.GetVarTypeByteSize(VarType.Bit));
        Assert.Equal(1, S7Client.GetVarTypeByteSize(VarType.Byte));
    }

    [Fact]
    public void GetVarTypeByteSize_WordAndInt()
    {
        Assert.Equal(2, S7Client.GetVarTypeByteSize(VarType.Word));
        Assert.Equal(2, S7Client.GetVarTypeByteSize(VarType.Int));
        Assert.Equal(2, S7Client.GetVarTypeByteSize(VarType.Timer));
        Assert.Equal(2, S7Client.GetVarTypeByteSize(VarType.Counter));
    }

    [Fact]
    public void GetVarTypeByteSize_FourBytes()
    {
        Assert.Equal(4, S7Client.GetVarTypeByteSize(VarType.DWord));
        Assert.Equal(4, S7Client.GetVarTypeByteSize(VarType.DInt));
        Assert.Equal(4, S7Client.GetVarTypeByteSize(VarType.Real));
    }

    [Fact]
    public void GetVarTypeByteSize_EightBytes()
    {
        Assert.Equal(8, S7Client.GetVarTypeByteSize(VarType.LReal));
        Assert.Equal(8, S7Client.GetVarTypeByteSize(VarType.DateTime));
    }

    [Fact]
    public void GetVarTypeByteSize_String_Throws()
    {
        Assert.Throws<NotSupportedException>(() => S7Client.GetVarTypeByteSize(VarType.String));
    }
    #endregion

    #region 服务端准备
    [TestOrder(10)]
    [Fact]
    public void HL_StartServer()
    {
        var server = new S7Server { Port = TestPort };
        // DB1: DBW0=1000, DBW2=(-500), DBD10=3.14f, DBD20=1.23456789, DBX30.0=true, DBX30.2=false
        server.SetValue("DB1.DBW0", (Int16)1000);
        server.SetValue("DB1.DBW2", (Int16)(-500));
        server.SetValue("DB1.DBD10", 3.14f);
        server.SetValue("DB1.DBD20", (Single)1.234567f);  // LReal 不支持，用 Single 模拟
        // Set DB1.DBX30.0 = true
        server.SetValue("DB1.DBX30.0", new Byte[] { 1 });
        // Set DB1.DBX30.2 = false (already zero)
        // Memory area: MB10=0x55
        server.SetValue("MB10", new Byte[] { 0x55 });
        // DB1.DBD70 = Double/LReal ≈ 1.23456789012345
        server.SetValue("DB1.DBD70", 1.23456789012345);
        // DB1.DBD80 = UInt32/DWord = 3000000000 (big-endian: B2 D0 5E 00)
        server.SetValue("DB1.DBD80", new Byte[] { 0xB2, 0xD0, 0x5E, 0x00 });
        // DB1.DBD84 = Int32/DInt = 100000
        server.SetValue("DB1.DBD84", 100000);
        // String in DB2.STRING0.20
        var strBytes = new Byte[22];
        strBytes[0] = 20;       // max length
        strBytes[1] = 5;        // actual length
        strBytes[2] = (Byte)'H'; strBytes[3] = (Byte)'e'; strBytes[4] = (Byte)'l';
        strBytes[5] = (Byte)'l'; strBytes[6] = (Byte)'o';
        server.SetValue("DB2.DBB0", strBytes);  // write raw bytes

        server.Start();
        _server = server;

        Thread.Sleep(100); // 等待监听启动
    }

    [TestOrder(11)]
    [Fact]
    public async void HL_ConnectClient()
    {
        var client = new S7Client(CpuType.S7200, "127.0.0.1", TestPort);
        await client.OpenAsync();
        _client = client;
        Assert.True(client.MaxPDUSize > 0);
    }
    #endregion

    #region Read<T> 泛型读取
    [TestOrder(20)]
    [Fact]
    public void HL_Read_Int16_Positive()
    {
        Assert.NotNull(_client);
        var val = _client!.Read<Int16>("DB1.DBW0");
        Assert.Equal((Int16)1000, val);
    }

    [TestOrder(21)]
    [Fact]
    public void HL_Read_Int16_Negative()
    {
        Assert.NotNull(_client);
        var val = _client!.Read<Int16>("DB1.DBW2");
        Assert.Equal((Int16)(-500), val);
    }

    [TestOrder(22)]
    [Fact]
    public void HL_Read_Single_Pi()
    {
        Assert.NotNull(_client);
        var val = _client!.Read<Single>("DB1.DBD10");
        Assert.Equal(3.14f, val, precision: 4);
    }

    [TestOrder(23)]
    [Fact]
    public void HL_Read_Byte_Memory()
    {
        Assert.NotNull(_client);
        var val = _client!.Read<Byte>("MB10");
        Assert.Equal(0x55, val);
    }

    [TestOrder(24)]
    [Fact]
    public void HL_Read_Bool_True()
    {
        Assert.NotNull(_client);
        var val = _client!.Read<Boolean>("DB1.DBX30.0");
        Assert.True(val);
    }

    [TestOrder(25)]
    [Fact]
    public void HL_Read_Bool_False()
    {
        Assert.NotNull(_client);
        // DBX30.2 未置位，应为 false
        var val = _client!.Read<Boolean>("DB1.DBX30.2");
        Assert.False(val);
    }

    [TestOrder(26)]
    [Fact]
    public void HL_Read_UInt32_DWord()
    {
        Assert.NotNull(_client);
        var val = _client!.Read<UInt32>("DB1.DBD80");
        Assert.Equal(3000000000u, val);
    }

    [TestOrder(27)]
    [Fact]
    public void HL_Read_Double_LReal()
    {
        Assert.NotNull(_client);
        var val = _client!.Read<Double>("DB1.DBD70");
        Assert.Equal(1.23456789012345, val, precision: 10);
    }

    [TestOrder(28)]
    [Fact]
    public void HL_Read_Int32_DInt()
    {
        Assert.NotNull(_client);
        var val = _client!.Read<Int32>("DB1.DBD84");
        Assert.Equal(100000, val);
    }
    #endregion

    #region Write + Read 泛型回环
    [TestOrder(30)]
    [Fact]
    public void HL_Write_Int16_And_Read_Back()
    {
        Assert.NotNull(_client);
        _client!.Write("DB1.DBW6", (Int16)(-1234));
        var val = _client.Read<Int16>("DB1.DBW6");
        Assert.Equal((Int16)(-1234), val);
    }

    [TestOrder(31)]
    [Fact]
    public void HL_Write_Single_And_Read_Back()
    {
        Assert.NotNull(_client);
        _client!.Write("DB1.DBD40", 2.718f);
        var val = _client.Read<Single>("DB1.DBD40");
        Assert.Equal(2.718f, val, precision: 4);
    }

    [TestOrder(32)]
    [Fact]
    public void HL_Write_Byte_And_Read_Back()
    {
        Assert.NotNull(_client);
        _client!.Write("MB50", (Byte)0xAB);
        var val = _client.Read<Byte>("MB50");
        Assert.Equal((Byte)0xAB, val);
    }

    [TestOrder(33)]
    [Fact]
    public void HL_Write_UInt16_And_Read_Back()
    {
        Assert.NotNull(_client);
        _client!.Write("DB1.DBW8", (UInt16)60000);
        var val = _client.Read<UInt16>("DB1.DBW8");
        Assert.Equal((UInt16)60000, val);
    }

    [TestOrder(34)]
    [Fact]
    public void HL_Write_Bool_True_And_Read_Back()
    {
        Assert.NotNull(_client);
        _client!.Write("DB1.DBX100.4", true);
        var val = _client.Read<Boolean>("DB1.DBX100.4");
        Assert.True(val);
    }

    [TestOrder(35)]
    [Fact]
    public void HL_Write_Bool_False_And_Read_Back()
    {
        Assert.NotNull(_client);
        // 先置位再清零
        _client!.Write("DB1.DBX100.5", true);
        _client.Write("DB1.DBX100.5", false);
        var val = _client.Read<Boolean>("DB1.DBX100.5");
        Assert.False(val);
    }

    [TestOrder(36)]
    [Fact]
    public void HL_Write_UInt32_And_Read_Back()
    {
        Assert.NotNull(_client);
        _client!.Write("DB1.DBD90", (UInt32)4000000000);
        var val = _client.Read<UInt32>("DB1.DBD90");
        Assert.Equal(4000000000u, val);
    }

    [TestOrder(37)]
    [Fact]
    public void HL_Write_Double_And_Read_Back()
    {
        Assert.NotNull(_client);
        _client!.Write("DB1.DBD100", 2.718281828459045);
        var val = _client.Read<Double>("DB1.DBD100");
        Assert.Equal(2.718281828459045, val, precision: 12);
    }

    [TestOrder(38)]
    [Fact]
    public void HL_Write_Int32_And_Read_Back()
    {
        Assert.NotNull(_client);
        _client!.Write("DB1.DBD110", -999999);
        var val = _client.Read<Int32>("DB1.DBD110");
        Assert.Equal(-999999, val);
    }
    #endregion

    #region ReadArray<T>
    [TestOrder(40)]
    [Fact]
    public void HL_ReadArray_Int16()
    {
        Assert.NotNull(_client);
        // 先写入 DB3.DBW0/2/4 三个 Int16 值
        _client!.Write("DB3.DBW0", (Int16)10);
        _client.Write("DB3.DBW2", (Int16)20);
        _client.Write("DB3.DBW4", (Int16)30);

        // 读取时使用起始地址 DB3.DBW0，ReadArray<Int16> 按步长=2 连续读3个
        var arr = _client.ReadArray<Int16>("DB3.DBW0", 3);
        Assert.Equal(3, arr.Length);
        Assert.Equal((Int16)10, arr[0]);
        Assert.Equal((Int16)20, arr[1]);
        Assert.Equal((Int16)30, arr[2]);
    }

    [TestOrder(41)]
    [Fact]
    public void HL_ReadArray_Single()
    {
        Assert.NotNull(_client);
        _client!.Write("DB3.DBD10", 1.1f);
        _client.Write("DB3.DBD14", 2.2f);

        var arr = _client.ReadArray<Single>("DB3.DBD10", 2);
        Assert.Equal(2, arr.Length);
        Assert.Equal(1.1f, arr[0], precision: 4);
        Assert.Equal(2.2f, arr[1], precision: 4);
    }
    #endregion

    #region ReadString / WriteString
    [TestOrder(50)]
    [Fact]
    public void HL_ReadString_PresetValue()
    {
        Assert.NotNull(_client);
        var str = _client!.ReadString("DB2.STRING0.20", 20);
        Assert.Equal("Hello", str);
    }

    [TestOrder(51)]
    [Fact]
    public void HL_WriteString_And_Read_Back()
    {
        Assert.NotNull(_client);
        _client!.WriteString("DB2.STRING22.30", "World", 30);
        var str = _client.ReadString("DB2.STRING22.30", 30);
        Assert.Equal("World", str);
    }

    [TestOrder(52)]
    [Fact]
    public void HL_WriteString_Empty_And_Read_Back()
    {
        Assert.NotNull(_client);
        _client!.WriteString("DB2.STRING60.10", "", 10);
        var str = _client.ReadString("DB2.STRING60.10", 10);
        Assert.Equal("", str);
    }
    #endregion

    #region Read(Type) 反射重载
    [TestOrder(60)]
    [Fact]
    public void HL_Read_Type_Int16()
    {
        Assert.NotNull(_client);
        var val = _client!.Read("DB1.DBW0", typeof(Int16));
        Assert.IsType<Int16>(val);
        Assert.Equal((Int16)1000, (Int16)val!);
    }

    [TestOrder(61)]
    [Fact]
    public void HL_Read_Type_Single()
    {
        Assert.NotNull(_client);
        var val = _client!.Read("DB1.DBD10", typeof(Single));
        Assert.IsType<Single>(val);
        Assert.Equal(3.14f, (Single)val!, precision: 4);
    }

    [TestOrder(62)]
    [Fact]
    public void HL_Read_Type_Bool()
    {
        Assert.NotNull(_client);
        var val = _client!.Read("DB1.DBX30.0", typeof(Boolean));
        Assert.IsType<Boolean>(val);
        Assert.True((Boolean)val!);
    }

    [TestOrder(63)]
    [Fact]
    public void HL_Read_Type_UInt32()
    {
        Assert.NotNull(_client);
        var val = _client!.Read("DB1.DBD80", typeof(UInt32));
        Assert.IsType<UInt32>(val);
        Assert.Equal(3000000000u, (UInt32)val!);
    }

    [TestOrder(64)]
    [Fact]
    public void HL_Read_Type_Double()
    {
        Assert.NotNull(_client);
        var val = _client!.Read("DB1.DBD70", typeof(Double));
        Assert.IsType<Double>(val);
        Assert.Equal(1.23456789012345, (Double)val!, precision: 10);
    }

    [TestOrder(65)]
    [Fact]
    public void HL_Read_Type_String()
    {
        Assert.NotNull(_client);
        var val = _client!.Read("DB2.STRING0.20", typeof(String));
        Assert.IsType<String>(val);
        Assert.Equal("Hello", (String)val!);
    }

    [TestOrder(66)]
    [Fact]
    public void HL_Read_Type_Unsupported_Throws()
    {
        Assert.NotNull(_client);
        Assert.Throws<NotSupportedException>(() => _client!.Read("DB1.DBW0", typeof(Decimal)));
    }
    #endregion

    #region 纯逻辑测试（无网络）
    [Fact]
    public void HL_ReadArray_ZeroCount_Returns_Empty()
    {
        // 不需要网络，直接 new 一个不连接的 client
        using var client = new S7Client(CpuType.S7200, "127.0.0.1", 19998);
        var arr = client.ReadArray<Int16>("DB1.DBW0", 0);
        Assert.NotNull(arr);
        Assert.Empty(arr);
    }

    [Fact]
    public void HL_GetVarTypeByteSize_DateTimeLong_Unsupported()
    {
        // DateTimeLong 不在 switch 中，应抛出 NotSupportedException
        Assert.Throws<NotSupportedException>(() => S7Client.GetVarTypeByteSize(VarType.DateTimeLong));
    }
    #endregion
}
