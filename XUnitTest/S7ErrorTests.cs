using System;
using System.Net.Sockets;
using System.Threading;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using NewLife.UnitTest;
using Xunit;

namespace XUnitTest;

/// <summary>S7Client 错误场景集成测试</summary>
/// <remarks>
/// 涵盖：连接错误、断线后自动重连、写入后关闭服务端异常传播。
/// 所有测试均使用内置 S7Server 模拟器，无需真实 PLC 硬件。
/// </remarks>
[TestCaseOrderer("NewLife.UnitTest.PriorityOrderer", "NewLife.UnitTest")]
public class S7ErrorTests
{
    private const Int32 TestPort = 10244;
    private static S7Server? _server;
    private static S7Client? _client;

    #region 01 — 启动服务端与连接

    [TestOrder(1)]
    [Fact]
    public void Err_StartServer()
    {
        var server = new S7Server { Port = TestPort };
        server.SetValue("DB1.DBW0", (Int16)42);
        server.Start();
        _server = server;

        Thread.Sleep(100);
        Assert.True(server.Active);
    }

    [TestOrder(2)]
    [Fact]
    public async void Err_Connect()
    {
        var client = new S7Client(CpuType.S7200, "127.0.0.1", TestPort);
        await client.OpenAsync();
        _client = client;
        Assert.True(client.MaxPDUSize > 0);
    }

    #endregion

    #region 02 — 连接错误（无服务端监听）

    [Fact]
    public async void Err_WrongPort_ThrowsException()
    {
        // 端口 19995 无监听 → SocketException 或 IOException
        var client = new S7Client(CpuType.S7200, "127.0.0.1", 19995);
        var ex = await Record.ExceptionAsync(() => client.OpenAsync());
        Assert.NotNull(ex);
        Assert.True(ex is SocketException || ex.InnerException is SocketException,
            $"期望 SocketException，实际: {ex.GetType().Name}: {ex.Message}");
    }

    #endregion

    #region 03 — 正常读写（基线验证）

    [TestOrder(10)]
    [Fact]
    public void Err_NormalRead_Works()
    {
        Assert.NotNull(_client);
        var addr = new PLCAddress("DB1.DBW0");
        var data = _client!.ReadBytes(addr, 2);
        Assert.Equal(2, data.Length);
        // 42 = 0x002A 大端
        Assert.Equal(0x00, data[0]);
        Assert.Equal(0x2A, data[1]);
    }

    [TestOrder(11)]
    [Fact]
    public void Err_NormalWrite_Works()
    {
        Assert.NotNull(_client);
        var addr = new PLCAddress("DB1.DBW0");
        _client!.WriteBytes(addr, new Byte[] { 0x01, 0x00 }); // 256
        var data = _client.ReadBytes(addr, 2);
        Assert.Equal(0x01, data[0]);
        Assert.Equal(0x00, data[1]);
    }

    #endregion

    #region 04 — 断线重连（Close 后再次读取应自动重连）

    [TestOrder(20)]
    [Fact]
    public void Err_Reconnect_After_Close()
    {
        Assert.NotNull(_client);
        Assert.NotNull(_server);

        // 主动断开
        _client!.Close();

        // 重置预置值，确保读到的是服务端当前值
        _server!.SetValue("DB1.DBW10", (Int16)7777);

        // 再次读取 → GetStream() 检测到 _stream==null，自动重新 OpenAsync()
        var addr = new PLCAddress("DB1.DBW10");
        var data = _client.ReadBytes(addr, 2);

        Assert.Equal(2, data.Length);
        // 7777 = 0x1E61 大端
        Assert.Equal(0x1E, data[0]);
        Assert.Equal(0x61, data[1]);
    }

    [TestOrder(21)]
    [Fact]
    public void Err_Reconnect_Write_After_Close()
    {
        Assert.NotNull(_client);

        // 再次 Close，验证写入也能触发重连
        _client!.Close();

        var addr = new PLCAddress("DB1.DBW20");
        _client.WriteBytes(addr, new Byte[] { 0xDE, 0xAD });

        var readBack = _client.ReadBytes(addr, 2);
        Assert.Equal(0xDE, readBack[0]);
        Assert.Equal(0xAD, readBack[1]);
    }

    #endregion

    #region 05 — 多次连续读（序列号递增无状态污染）

    [TestOrder(30)]
    [Fact]
    public void Err_Sequential_Reads_NoCorruption()
    {
        Assert.NotNull(_client);

        _server!.SetValue("DB1.DBW30", (Int16)1111);
        _server!.SetValue("DB1.DBW32", (Int16)2222);
        _server!.SetValue("DB1.DBW34", (Int16)3333);

        var addr30 = new PLCAddress("DB1.DBW30");
        var addr32 = new PLCAddress("DB1.DBW32");
        var addr34 = new PLCAddress("DB1.DBW34");

        // 连续发送 6 次请求，验证响应不会被交叉
        for (var i = 0; i < 2; i++)
        {
            var d30 = _client!.ReadBytes(addr30, 2);
            var d32 = _client.ReadBytes(addr32, 2);
            var d34 = _client.ReadBytes(addr34, 2);

            Assert.Equal(1111, (d30[0] << 8) | d30[1]);
            Assert.Equal(2222, (d32[0] << 8) | d32[1]);
            Assert.Equal(3333, (d34[0] << 8) | d34[1]);
        }
    }

    #endregion

    #region 99 — 清理

    [TestOrder(99)]
    [Fact]
    public void Err_StopServer()
    {
        _client?.Close();
        _server?.Stop("done");
        _server = null;
    }

    #endregion
}
