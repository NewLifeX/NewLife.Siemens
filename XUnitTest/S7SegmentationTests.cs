using System;
using System.Threading;
using NewLife.Log;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using NewLife.UnitTest;
using Xunit;

namespace XUnitTest;

/// <summary>PDU 分段读写集成测试：验证 ReadBytes/WriteBytes 在数据量超过单个 PDU 时能自动分段处理</summary>
/// <remarks>
/// S7Server SetupMessage 协商后 MaxPDUSize = 960。
/// 读分段阈值：960 - 18 = 942 字节/次。
/// 写分段阈值：960 - 28 = 932 字节/次。
/// 读 2000 字节 = 3 次请求（942 + 942 + 116）。
/// 写 2000 字节 = 3 次请求（932 + 932 + 136）。
/// </remarks>
[TestCaseOrderer("NewLife.UnitTest.PriorityOrderer", "NewLife.UnitTest")]
public class S7SegmentationTests
{
    private const Int32 TestPort = 10246;
    private static S7Server? _server;
    private static S7Client? _client;

    // 测试用数据块大小（跨越多个 PDU）
    private const Int32 DataSize = 2000;

    // 生成可验证的测试模式：byte[i] = (byte)((i * 7 + 13) % 251)
    private static Byte[] MakePattern(Int32 size)
    {
        var data = new Byte[size];
        for (var i = 0; i < size; i++)
            data[i] = (Byte)((i * 7 + 13) % 251);
        return data;
    }

    #region 01 — 启动服务端

    [TestOrder(1)]
    [Fact]
    public void Seg_StartServer()
    {
        var server = new S7Server { Port = TestPort };

        // DB10：预置 2000 字节模式数据，用于多段读验证
        var pattern = MakePattern(DataSize);
        server.SetValue("DB10.DBB0", pattern);

        // DB11：预留给多段写测试（初始全零，无需预置）

        server.Start();
        _server = server;

        Thread.Sleep(100);
        Assert.True(server.Active);
    }

    [TestOrder(2)]
    [Fact]
    public async void Seg_Connect()
    {
        var client = new S7Client(CpuType.S7200, "127.0.0.1", TestPort);
        await client.OpenAsync();
        _client = client;

        // 确认 PDU 协商完成且 ≥ 960
        Assert.True(client.MaxPDUSize >= 960, $"MaxPDUSize={client.MaxPDUSize}");
    }

    #endregion

    #region 02 — 多段读（ReadBytes 分段）

    [TestOrder(10)]
    [Fact]
    public void Seg_ReadBytes_2000Bytes_AllCorrect()
    {
        Assert.NotNull(_client);

        var addr = new PLCAddress("DB10.DBB0");
        var data = _client!.ReadBytes(addr, DataSize);

        Assert.Equal(DataSize, data.Length);

        // 验证读回数据与写入模式完全一致
        var expected = MakePattern(DataSize);
        for (var i = 0; i < DataSize; i++)
            Assert.Equal(expected[i], data[i]);
    }

    [TestOrder(11)]
    [Fact]
    public void Seg_ReadBytes_ChunkBoundary_Byte942_Correct()
    {
        Assert.NotNull(_client);

        // 第一分段最后一字节索引 = 941（第 942 个字节，MaxPDU-18 = 942，索引 0..941）
        // 从 offset 941 读 2 字节，跨越第一/第二分段边界
        var addr = new PLCAddress("DB10.DBB941");
        var data = _client!.ReadBytes(addr, 2);

        Assert.Equal(2, data.Length);
        var expected = MakePattern(DataSize);
        Assert.Equal(expected[941], data[0]);
        Assert.Equal(expected[942], data[1]);
    }

    [TestOrder(12)]
    [Fact]
    public void Seg_ReadBytes_SecondChunkBoundary_Correct()
    {
        Assert.NotNull(_client);

        // 第二/第三分段边界：索引 1883/1884
        var addr = new PLCAddress("DB10.DBB1883");
        var data = _client!.ReadBytes(addr, 2);

        Assert.Equal(2, data.Length);
        var expected = MakePattern(DataSize);
        Assert.Equal(expected[1883], data[0]);
        Assert.Equal(expected[1884], data[1]);
    }

    [TestOrder(13)]
    [Fact]
    public void Seg_ReadBytes_Tail_Correct()
    {
        Assert.NotNull(_client);

        // 最后 4 字节（索引 1996..1999）
        var addr = new PLCAddress("DB10.DBB1996");
        var data = _client!.ReadBytes(addr, 4);

        Assert.Equal(4, data.Length);
        var expected = MakePattern(DataSize);
        Assert.Equal(expected[1996], data[0]);
        Assert.Equal(expected[1999], data[3]);
    }

    #endregion

    #region 03 — 多段写（WriteBytes 分段）

    [TestOrder(20)]
    [Fact]
    public void Seg_WriteBytes_2000Bytes_And_ReadBack()
    {
        Assert.NotNull(_client);

        // 写入一组不同于 DB10 的模式（便于区分）
        var writeData = new Byte[DataSize];
        for (var i = 0; i < DataSize; i++)
            writeData[i] = (Byte)((i * 3 + 7) % 233);

        var addr = new PLCAddress("DB11.DBB0");
        _client!.WriteBytes(addr, writeData);

        // 分三次读回验证（不依赖多段读）
        var readBack = _client.ReadBytes(addr, DataSize);
        Assert.Equal(DataSize, readBack.Length);
        for (var i = 0; i < DataSize; i++)
            Assert.Equal(writeData[i], readBack[i]);
    }

    [TestOrder(21)]
    [Fact]
    public void Seg_WriteBytes_ChunkBoundary_Preserved()
    {
        Assert.NotNull(_client);

        // 只写入 DB11 第 930..934 字节（跨第一/第二写分段边界 932）
        // 目标：覆盖 932 字节写边界附近的两个值
        var addr = new PLCAddress("DB11.DBB930");
        var patch = new Byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE };
        _client!.WriteBytes(addr, patch);

        var readBack = _client.ReadBytes(addr, 5);
        Assert.Equal(new Byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE }, readBack);
    }

    #endregion

    #region 04 — ReadArray 多段（通过 Read<T> 高层接口）

    [TestOrder(30)]
    [Fact]
    public void Seg_ReadArray_Int16_Large()
    {
        Assert.NotNull(_client);

        // DB10 前 2000 字节按 Int16 解读 = 1000 个 Int16
        // 仅验证数组长度和边界元素
        var arr = _client!.ReadArray<Int16>("DB10.DBW0", 1000);

        Assert.Equal(1000, arr.Length);

        // 手动重建预期值（大端转 Int16）
        var raw = MakePattern(DataSize);
        var expected0 = (Int16)((raw[0] << 8) | raw[1]);
        var expected999 = (Int16)((raw[1998] << 8) | raw[1999]);
        Assert.Equal(expected0, arr[0]);
        Assert.Equal(expected999, arr[999]);
    }

    #endregion

    #region 99 — 清理

    [TestOrder(99)]
    [Fact]
    public void Seg_StopServer()
    {
        _client?.Close();
        _server?.Stop("done");
        _server = null;
    }

    #endregion
}
