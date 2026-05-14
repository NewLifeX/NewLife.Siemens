using System;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using NewLife.UnitTest;
using Xunit;

namespace XUnitTest;

/// <summary>PLC时钟读写单元测试（基于S7Server模拟器）</summary>
[TestCaseOrderer("NewLife.UnitTest.PriorityOrderer", "NewLife.UnitTest")]
public class S7ClockTests
{
    private static S7Server? _server;
    private static S7Client? _client;
    private const Int32 TestPort = 10240;

    #region 01 — BCD编码/解码纯逻辑测试（无网络）
    [Fact]
    public void BcdEncode_TypicalDateTime()
    {
        var dt = new DateTime(2024, 5, 14, 9, 30, 45, 123);
        var buf = S7DateTimeHelper.Encode(dt);

        Assert.Equal(8, buf.Length);
        Assert.Equal(0x24, buf[0]); // 年：24 → BCD 0x24
        Assert.Equal(0x05, buf[1]); // 月：05
        Assert.Equal(0x14, buf[2]); // 日：14 → BCD 0x14
        Assert.Equal(0x09, buf[3]); // 时：09
        Assert.Equal(0x30, buf[4]); // 分：30
        Assert.Equal(0x45, buf[5]); // 秒：45
        Assert.Equal(0x12, buf[6]); // ms高2位：12(BCD)
    }

    [Fact]
    public void BcdDecode_TypicalBytes()
    {
        // 2025-01-06 12:00:00.000，星期一
        var buf = new Byte[] { 0x25, 0x01, 0x06, 0x12, 0x00, 0x00, 0x00, 0x01 };
        var dt = S7DateTimeHelper.Decode(buf);

        Assert.Equal(2025, dt.Year);
        Assert.Equal(1, dt.Month);
        Assert.Equal(6, dt.Day);
        Assert.Equal(12, dt.Hour);
        Assert.Equal(0, dt.Minute);
        Assert.Equal(0, dt.Second);
        Assert.Equal(0, dt.Millisecond);
    }

    [Fact]
    public void BcdRoundTrip_EncodeDecode()
    {
        var original = new DateTime(2023, 11, 27, 8, 15, 59, 456);
        var encoded = S7DateTimeHelper.Encode(original);
        var decoded = S7DateTimeHelper.Decode(encoded);

        Assert.Equal(original.Year, decoded.Year);
        Assert.Equal(original.Month, decoded.Month);
        Assert.Equal(original.Day, decoded.Day);
        Assert.Equal(original.Hour, decoded.Hour);
        Assert.Equal(original.Minute, decoded.Minute);
        Assert.Equal(original.Second, decoded.Second);
        Assert.Equal(original.Millisecond, decoded.Millisecond);
    }

    [Fact]
    public void BcdRoundTrip_1990sYear()
    {
        var dt1999 = new DateTime(1999, 12, 31, 23, 59, 59, 0);
        var encoded = S7DateTimeHelper.Encode(dt1999);
        Assert.Equal(0x99, encoded[0]); // 99 → BCD 0x99

        var decoded = S7DateTimeHelper.Decode(encoded);
        Assert.Equal(1999, decoded.Year);
    }

    [Fact]
    public void BcdRoundTrip_2000sYear()
    {
        var dt2000 = new DateTime(2000, 1, 1, 0, 0, 0, 0);
        var encoded = S7DateTimeHelper.Encode(dt2000);
        Assert.Equal(0x00, encoded[0]); // 0 → BCD 0x00

        var decoded = S7DateTimeHelper.Decode(encoded);
        Assert.Equal(2000, decoded.Year);
    }

    [Fact]
    public void BcdEncode_MaxMillisecond()
    {
        var dt = new DateTime(2024, 1, 1, 0, 0, 0, 999);
        var buf = S7DateTimeHelper.Encode(dt);
        var decoded = S7DateTimeHelper.Decode(buf);
        Assert.Equal(999, decoded.Millisecond);
    }

    [Fact]
    public void BcdDecode_InsufficientData_Throws()
    {
        Assert.Throws<ArgumentException>(() => S7DateTimeHelper.Decode(new Byte[7]));
    }
    #endregion

    #region 02 — 服务端启动与时钟预置
    [TestOrder(10)]
    [Fact]
    public void Clock_StartServer()
    {
        var server = new S7Server { Port = TestPort, Log = XTrace.Log };
        server.SimulatedClock = new DateTime(2024, 5, 14, 10, 30, 0, 0);

        server.Start();
        _server = server;

        Thread.Sleep(100);
        Assert.True(server.Active);
    }
    #endregion

    #region 03 — 客户端连接
    [TestOrder(11)]
    [Fact]
    public async Task Clock_Connect()
    {
        var client = new S7Client(CpuType.S7200, "127.0.0.1", TestPort)
        {
            Log = XTrace.Log,
        };
        await client.OpenAsync();
        _client = client;
        Assert.NotNull(_client);
    }
    #endregion

    #region 04 — 读时钟
    [TestOrder(20)]
    [Fact]
    public async Task Clock_Read_ReturnsSimulatedTime()
    {
        Assert.NotNull(_client);
        var dt = await _client!.ReadClockAsync();

        Assert.Equal(2024, dt.Year);
        Assert.Equal(5, dt.Month);
        Assert.Equal(14, dt.Day);
        Assert.Equal(10, dt.Hour);
        Assert.Equal(30, dt.Minute);
        Assert.Equal(0, dt.Second);
    }
    #endregion

    #region 05 — 写时钟
    [TestOrder(30)]
    [Fact]
    public async Task Clock_Write_ThenRead()
    {
        Assert.NotNull(_client);

        var newTime = new DateTime(2026, 1, 1, 12, 0, 0, 0);
        await _client!.WriteClockAsync(newTime);

        var readBack = await _client.ReadClockAsync();
        Assert.Equal(newTime.Year, readBack.Year);
        Assert.Equal(newTime.Month, readBack.Month);
        Assert.Equal(newTime.Day, readBack.Day);
        Assert.Equal(newTime.Hour, readBack.Hour);
        Assert.Equal(newTime.Minute, readBack.Minute);
        Assert.Equal(newTime.Second, readBack.Second);
    }
    #endregion

    #region 06 — 未预置时钟：使用系统时间
    [TestOrder(40)]
    [Fact]
    public async Task Clock_Read_DefaultsToSystemTime()
    {
        var port2 = TestPort + 1;
        var server2 = new S7Server { Port = port2 };
        server2.Start();
        Thread.Sleep(100);

        try
        {
            var client2 = new S7Client(CpuType.S7200, "127.0.0.1", port2);
            await client2.OpenAsync();

            var before = DateTime.Now.AddSeconds(-5);
            var dt = await client2.ReadClockAsync();
            var after = DateTime.Now.AddSeconds(5);

            Assert.True(dt >= before && dt <= after, $"返回时间 {dt} 不在范围 [{before}, {after}]");
            client2.Close();
        }
        finally
        {
            server2.Stop("done");
        }
    }
    #endregion

    #region 99 — 清理
    [TestOrder(99)]
    [Fact]
    public void Clock_StopServer()
    {
        _client?.Close();
        _server?.Stop("done");
    }
    #endregion
}
