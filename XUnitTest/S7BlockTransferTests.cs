using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using NewLife.UnitTest;
using Xunit;

namespace XUnitTest;

/// <summary>S7Client 块传输（ListBlocks / Upload / Download）端到端测试</summary>
[TestCaseOrderer("NewLife.UnitTest.PriorityOrderer", "NewLife.UnitTest")]
public class S7BlockTransferTests
{
    private static S7Server? _server;
    private static S7Client? _client;
    private const Int32 TestPort = 10252;

    #region 01 — 服务端启动与块预置
    [TestOrder(10)]
    [Fact]
    public void Block_StartServer()
    {
        var server = new S7Server { Port = TestPort, Log = XTrace.Log };

        // 预置 DB1（32字节）和 DB5（16字节）供上传测试使用
        var db1Data = Enumerable.Range(1, 32).Select(i => (Byte)i).ToArray();
        server.SetBlock(S7BlockType.DB, 1, db1Data);

        var db5Data = Enumerable.Range(10, 16).Select(i => (Byte)i).ToArray();
        server.SetBlock(S7BlockType.DB, 5, db5Data);

        // 预先写一些 DB 内存数据（用于 ListBlocks 时 DB3 也出现在列表里）
        server.SetValue("DB3.DBW0", (Int16)9999);

        server.Start();
        _server = server;

        Thread.Sleep(100);
        Assert.True(server.Active);
    }
    #endregion

    #region 02 — 客户端连接
    [TestOrder(11)]
    [Fact]
    public async Task Block_Connect()
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

    #region 03 — ListBlocks
    [TestOrder(20)]
    [Fact]
    public async Task Block_ListBlocks_ContainsPresetDbs()
    {
        Assert.NotNull(_client);

        var blocks = await _client!.ListBlocksAsync(S7BlockType.DB);
        Assert.NotNull(blocks);

        var nums = blocks.Select(b => b.BlockNumber).ToArray();
        // DB1 和 DB5 通过 SetBlock 预置，DB3 通过 SetValue 写入内存区
        Assert.Contains(1, nums);
        Assert.Contains(5, nums);
        Assert.Contains(3, nums);
    }

    [TestOrder(21)]
    [Fact]
    public async Task Block_ListBlocks_AllHaveCorrectType()
    {
        Assert.NotNull(_client);

        var blocks = await _client!.ListBlocksAsync(S7BlockType.DB);
        Assert.All(blocks, b => Assert.Equal(S7BlockType.DB, b.BlockType));
    }

    [TestOrder(22)]
    [Fact]
    public async Task Block_ListBlocks_EmptyForOB()
    {
        Assert.NotNull(_client);

        // 服务器没有预置任何 OB 块
        var blocks = await _client!.ListBlocksAsync(S7BlockType.OB);
        Assert.NotNull(blocks);
        Assert.Empty(blocks);
    }
    #endregion

    #region 04 — UploadBlock（从服务端读取块）
    [TestOrder(30)]
    [Fact]
    public async Task Block_Upload_DB1_MatchesPreset()
    {
        Assert.NotNull(_client);

        var data = await _client!.UploadBlockAsync(S7BlockType.DB, 1);
        Assert.NotNull(data);
        Assert.Equal(32, data.Length);

        // 验证内容与预置一致（1..32）
        for (var i = 0; i < 32; i++)
            Assert.Equal((Byte)(i + 1), data[i]);
    }

    [TestOrder(31)]
    [Fact]
    public async Task Block_Upload_DB5_MatchesPreset()
    {
        Assert.NotNull(_client);

        var data = await _client!.UploadBlockAsync(S7BlockType.DB, 5);
        Assert.Equal(16, data.Length);

        for (var i = 0; i < 16; i++)
            Assert.Equal((Byte)(i + 10), data[i]);
    }

    [TestOrder(32)]
    [Fact]
    public async Task Block_Upload_NonExistent_ReturnsEmpty()
    {
        Assert.NotNull(_client);

        // 未预置的块应返回空字节数组（服务端返回空数据）
        var data = await _client!.UploadBlockAsync(S7BlockType.DB, 99);
        Assert.NotNull(data);
        Assert.Empty(data);
    }
    #endregion

    #region 05 — DownloadBlock（向服务端写入块）然后再上传验证
    [TestOrder(40)]
    [Fact]
    public async Task Block_Download_ThenUpload_Roundtrip()
    {
        Assert.NotNull(_client);
        Assert.NotNull(_server);

        // 构造 64 字节测试块数据
        var original = Enumerable.Range(0, 64).Select(i => (Byte)(i * 2)).ToArray();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await _client!.DownloadBlockAsync(S7BlockType.DB, 10, original, cts.Token);

        // 验证服务端已存储
        var stored = _server!.GetBlock(S7BlockType.DB, 10);
        Assert.NotNull(stored);
        Assert.Equal(original, stored);
    }

    [TestOrder(41)]
    [Fact]
    public async Task Block_Download_ThenUpload_ViaClient()
    {
        Assert.NotNull(_client);

        // 先下载 DB11
        var payload = new Byte[48];
        for (var i = 0; i < 48; i++) payload[i] = (Byte)(i + 0x10);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await _client!.DownloadBlockAsync(S7BlockType.DB, 11, payload, cts.Token);

        // 再通过 Upload 接口验证内容一致
        var uploaded = await _client.UploadBlockAsync(S7BlockType.DB, 11);
        Assert.Equal(payload, uploaded);
    }
    #endregion

    #region 99 — 清理
    [TestOrder(99)]
    [Fact]
    public void Block_StopServer()
    {
        _client?.Close();
        _server?.Stop("done");
        _server = null;
    }
    #endregion
}
