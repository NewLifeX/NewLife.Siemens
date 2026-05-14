using System;
using System.Threading;
using NewLife.Log;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using NewLife.UnitTest;
using Xunit;

namespace XUnitTest;

/// <summary>S7Client 基础连接与属性测试（基于 S7Server 模拟器）</summary>
[TestCaseOrderer("NewLife.UnitTest.PriorityOrderer", "NewLife.UnitTest")]
public class S7PLCTests
{
    private static S7Server? _server;
    private static S7Client? _client;
    private const Int32 TestPort = 10250;

    [TestOrder(1)]
    [Fact]
    public void StartServer()
    {
        var server = new S7Server
        {
            Port = TestPort,
            Log = XTrace.Log,
            SessionLog = XTrace.Log,
            SocketLog = XTrace.Log,
            LogSend = true,
            LogReceive = true,
        };

        server.SetValue("DB1.DBW0", (Int16)1234);
        server.SetValue("MB0", new Byte[] { 0x55 });
        server.Start();

        _server = server;

        Thread.Sleep(100);
        Assert.True(server.Active);
    }

    [TestOrder(2)]
    [Fact]
    public async void S7200_ConnectAndCheckPDU()
    {
        var s7 = new S7Client(CpuType.S7200, "127.0.0.1", TestPort)
        {
            Log = XTrace.Log,
        };

        await s7.OpenAsync();
        _client = s7;

        Assert.True(s7.MaxPDUSize > 0, $"PDU 协商应 > 0，实际={s7.MaxPDUSize}");
    }

    [TestOrder(3)]
    [Fact]
    public void S7200_ReadBytes_Roundtrip()
    {
        Assert.NotNull(_client);

        // 1234 = 0x04D2 大端
        var addr = new PLCAddress("DB1.DBW0");
        var data = _client!.ReadBytes(addr, 2);

        Assert.Equal(2, data.Length);
        Assert.Equal(0x04, data[0]);
        Assert.Equal(0xD2, data[1]);
    }

    [TestOrder(4)]
    [Fact]
    public void S7200_WriteBytes_And_ReadBack()
    {
        Assert.NotNull(_client);

        var addr = new PLCAddress("DB1.DBW4");
        _client!.WriteBytes(addr, new Byte[] { 0xFF, 0xEE });

        var data = _client.ReadBytes(addr, 2);
        Assert.Equal(0xFF, data[0]);
        Assert.Equal(0xEE, data[1]);
    }

    [TestOrder(5)]
    [Fact]
    public void S7200_HighLevel_Read_Int16()
    {
        Assert.NotNull(_client);
        var val = _client!.Read<Int16>("DB1.DBW0");
        Assert.Equal((Int16)1234, val);
    }

    [TestOrder(6)]
    [Fact]
    public void S7200_HighLevel_Write_And_Read()
    {
        Assert.NotNull(_client);
        _client!.Write("DB1.DBW8", (Int16)9999);
        Assert.Equal((Int16)9999, _client.Read<Int16>("DB1.DBW8"));
    }

    [TestOrder(99)]
    [Fact]
    public void StopServer()
    {
        _client?.Close();
        _server?.Stop("done");
        _server = null;
    }
}

