using System;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Siemens.Messages;
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

    #region PLC 启停控制

    [TestOrder(10)]
    [Fact]
    public async Task PlcStop_RequiresAllowPlcControl()
    {
        Assert.NotNull(_client);
        // 默认 AllowPlcControl=false，应抛出 InvalidOperationException
        _client!.AllowPlcControl = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => _client.PlcStopAsync());
    }

    [TestOrder(11)]
    [Fact]
    public async Task PlcStop_ChangesServerStatus()
    {
        Assert.NotNull(_client);
        Assert.NotNull(_server);

        _client!.AllowPlcControl = true;
        await _client.PlcStopAsync();

        // 等待服务端处理
        Thread.Sleep(100);
        Assert.Equal(S7CpuStatus.Stop, _server!.CpuStatus);
    }

    [TestOrder(12)]
    [Fact]
    public async Task PlcStart_HotRestart_ChangesServerStatus()
    {
        Assert.NotNull(_client);
        Assert.NotNull(_server);

        _client!.AllowPlcControl = true;
        await _client.PlcHotRestartAsync();

        Thread.Sleep(100);
        Assert.Equal(S7CpuStatus.Run, _server!.CpuStatus);
    }

    [TestOrder(13)]
    [Fact]
    public async Task PlcStart_ColdRestart_ChangesServerStatus()
    {
        Assert.NotNull(_client);
        Assert.NotNull(_server);

        _client!.AllowPlcControl = true;
        await _client.PlcColdStartAsync();

        Thread.Sleep(100);
        Assert.Equal(S7CpuStatus.Run, _server!.CpuStatus);
    }

    [TestOrder(14)]
    [Fact]
    public async Task ReadCpuStatusAsync_AfterPlcStop_ReturnsStop()
    {
        Assert.NotNull(_client);

        _client!.AllowPlcControl = true;
        await _client.PlcStopAsync();
        Thread.Sleep(100);

        var status = await _client.ReadCpuStatusAsync();
        Assert.Equal(S7CpuStatus.Stop, status);
    }

    [TestOrder(15)]
    [Fact]
    public async Task ReadCpuStatusAsync_AfterPlcStart_ReturnsRun()
    {
        Assert.NotNull(_client);

        _client!.AllowPlcControl = true;
        await _client.PlcHotRestartAsync();
        Thread.Sleep(100);

        var status = await _client.ReadCpuStatusAsync();
        Assert.Equal(S7CpuStatus.Run, status);
    }

    #endregion

    #region ReadStruct / WriteStruct

    [TestOrder(20)]
    [Fact]
    public void GetS7StructByteSize_BasicAlignment()
    {
        // Bool=1, (pad=1), Int16=2, Single=4 → 1+1+2+4=8, 偶数不需再对齐
        var size = S7Client.GetS7StructByteSize(typeof(MotorData));
        Assert.Equal(8, size);
    }

    [TestOrder(21)]
    [Fact]
    public void ReadStruct_MapsDbBytes()
    {
        Assert.NotNull(_client);

        // 在 DB2 写入已知字节，然后用 ReadStruct 读取并验证字段映射
        // MotorData: Bool(1B) + pad(1B) + Int16(2B BE) + Single(4B BE)
        // Running=1, Speed=300(0x012C), Torque=1.5f(0x3FC00000)
        var db2Addr = new PLCAddress("DB2.DBB0");
        var raw = new Byte[] { 0x01, 0x00, 0x01, 0x2C, 0x3F, 0xC0, 0x00, 0x00 };
        _client!.WriteBytes(db2Addr, raw);

        var m = _client.ReadStruct<MotorData>("DB2");
        Assert.True(m.Running);
        Assert.Equal((Int16)300, m.Speed);
        Assert.Equal(1.5f, m.Torque, precision: 5);
    }

    [TestOrder(22)]
    [Fact]
    public void WriteStruct_ThenReadBack()
    {
        Assert.NotNull(_client);

        var m = new MotorData { Running = true, Speed = 500, Torque = 2.5f };
        _client!.WriteStruct("DB3", m);

        var m2 = _client.ReadStruct<MotorData>("DB3");
        Assert.Equal(m.Running, m2.Running);
        Assert.Equal(m.Speed, m2.Speed);
        Assert.Equal(m.Torque, m2.Torque, precision: 5);
    }

    #endregion

    [TestOrder(99)]
    [Fact]
    public void StopServer()
    {
        _client?.Close();
        _server?.Stop("done");
        _server = null;
    }
}

/// <summary>测试用 struct，模拟电机数据块</summary>
public struct MotorData
{
    /// <summary>运行标志</summary>
    public Boolean Running;

    /// <summary>转速（rpm）</summary>
    public Int16 Speed;

    /// <summary>力矩（N·m）</summary>
    public Single Torque;
}

