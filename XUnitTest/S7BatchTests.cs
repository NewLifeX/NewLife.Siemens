using System;
using System.Collections.Generic;
using System.Threading;
using NewLife.Siemens.Messages;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using NewLife.UnitTest;
using Xunit;

namespace XUnitTest;

/// <summary>批量多变量读写 API 单元测试</summary>
[TestCaseOrderer("NewLife.UnitTest.PriorityOrderer", "NewLife.UnitTest")]
public class S7BatchTests
{
    private static S7Server? _server;
    private static S7Client? _client;
    private const Int32 TestPort = 10234;

    #region 服务端准备
    [TestOrder(1)]
    [Fact]
    public void Batch_StartServer()
    {
        var server = new S7Server { Port = TestPort };

        // DB10 预置测试数据
        server.SetValue("DB10.DBW0", (Int16)100);
        server.SetValue("DB10.DBW2", (Int16)200);
        server.SetValue("DB10.DBW4", (Int16)300);
        server.SetValue("DB10.DBD10", 1.5f);
        server.SetValue("DB10.DBD14", 2.5f);

        // Memory 区域
        server.SetValue("MB0", new Byte[] { 0x11 });
        server.SetValue("MB1", new Byte[] { 0x22 });

        server.Start();
        _server = server;

        Thread.Sleep(100);
    }

    [TestOrder(2)]
    [Fact]
    public async void Batch_ConnectClient()
    {
        var client = new S7Client(CpuType.S7200, "127.0.0.1", TestPort);
        await client.OpenAsync();
        _client = client;
        Assert.True(client.MaxPDUSize > 0);
    }
    #endregion

    #region ReadMultipleVars 批量读取
    [TestOrder(10)]
    [Fact]
    public void ReadMultipleVars_SingleItem()
    {
        Assert.NotNull(_client);

        var items = new List<DataItem>
        {
            DataItem.Create(DataType.DataBlock, VarType.Int, 10, 0),
        };

        _client!.ReadMultipleVars(items);

        Assert.Equal((Int16)100, items[0].Value);
    }

    [TestOrder(11)]
    [Fact]
    public void ReadMultipleVars_MultipleItems_SameDB()
    {
        Assert.NotNull(_client);

        var items = new List<DataItem>
        {
            DataItem.Create(DataType.DataBlock, VarType.Int, 10, 0),
            DataItem.Create(DataType.DataBlock, VarType.Int, 10, 2),
            DataItem.Create(DataType.DataBlock, VarType.Int, 10, 4),
        };

        _client!.ReadMultipleVars(items);

        Assert.Equal((Int16)100, items[0].Value);
        Assert.Equal((Int16)200, items[1].Value);
        Assert.Equal((Int16)300, items[2].Value);
    }

    [TestOrder(12)]
    [Fact]
    public void ReadMultipleVars_MixedAreas()
    {
        Assert.NotNull(_client);

        var items = new List<DataItem>
        {
            DataItem.Create(DataType.DataBlock, VarType.Int, 10, 0),
            DataItem.Create(DataType.Memory, VarType.Byte, 0, 0),
            DataItem.Create(DataType.Memory, VarType.Byte, 0, 1),
        };

        _client!.ReadMultipleVars(items);

        Assert.Equal((Int16)100, items[0].Value);
        Assert.Equal((Byte)0x11, items[1].Value);
        Assert.Equal((Byte)0x22, items[2].Value);
    }

    [TestOrder(13)]
    [Fact]
    public void ReadMultipleVars_Float()
    {
        Assert.NotNull(_client);

        var items = new List<DataItem>
        {
            DataItem.Create(DataType.DataBlock, VarType.Real, 10, 10),
            DataItem.Create(DataType.DataBlock, VarType.Real, 10, 14),
        };

        _client!.ReadMultipleVars(items);

        Assert.NotNull(items[0].Value);
        Assert.NotNull(items[1].Value);
        Assert.Equal(1.5f, (Single)items[0].Value!, precision: 4);
        Assert.Equal(2.5f, (Single)items[1].Value!, precision: 4);
    }

    [TestOrder(14)]
    [Fact]
    public void ReadMultipleVars_LargeBatch_AutoSplits()
    {
        Assert.NotNull(_client);

        // 构建 25 个 DB10.DBW 的读请求（超过 MaxItemsPerBatch=20）
        var items = new List<DataItem>();
        for (var i = 0; i < 25; i++)
            items.Add(DataItem.Create(DataType.DataBlock, VarType.Int, 10, i * 2));

        // 前三个有值，其余为0
        _client!.ReadMultipleVars(items);

        Assert.Equal(25, items.Count);
        // 前三个有预置值
        Assert.Equal((Int16)100, items[0].Value);
        Assert.Equal((Int16)200, items[1].Value);
        Assert.Equal((Int16)300, items[2].Value);
    }
    #endregion

    #region WriteMultipleVars 批量写入
    [TestOrder(20)]
    [Fact]
    public void WriteMultipleVars_RoundTrip()
    {
        Assert.NotNull(_client);

        // 写入三个值
        var writeItems = new List<DataItem>
        {
            DataItem.Create(DataType.DataBlock, VarType.Int, 10, 20),
            DataItem.Create(DataType.DataBlock, VarType.Int, 10, 22),
            DataItem.Create(DataType.DataBlock, VarType.Int, 10, 24),
        };
        writeItems[0].Value = (Int16)111;
        writeItems[1].Value = (Int16)222;
        writeItems[2].Value = (Int16)333;

        _client!.WriteMultipleVars(writeItems);

        // 回读验证
        var readItems = new List<DataItem>
        {
            DataItem.Create(DataType.DataBlock, VarType.Int, 10, 20),
            DataItem.Create(DataType.DataBlock, VarType.Int, 10, 22),
            DataItem.Create(DataType.DataBlock, VarType.Int, 10, 24),
        };
        _client.ReadMultipleVars(readItems);

        Assert.Equal((Int16)111, readItems[0].Value);
        Assert.Equal((Int16)222, readItems[1].Value);
        Assert.Equal((Int16)333, readItems[2].Value);
    }

    [TestOrder(21)]
    [Fact]
    public void WriteMultipleVars_MixedTypes()
    {
        Assert.NotNull(_client);

        var items = new List<DataItem>
        {
            DataItem.Create(DataType.DataBlock, VarType.Byte, 10, 30),
            DataItem.Create(DataType.DataBlock, VarType.Word, 10, 32),
        };
        items[0].Value = (Byte)0xFF;
        items[1].Value = (UInt16)65535;

        _client!.WriteMultipleVars(items);

        // 回读
        var readItems = new List<DataItem>
        {
            DataItem.Create(DataType.DataBlock, VarType.Byte, 10, 30),
            DataItem.Create(DataType.DataBlock, VarType.Word, 10, 32),
        };
        _client.ReadMultipleVars(readItems);

        Assert.Equal((Byte)0xFF, readItems[0].Value);
        Assert.Equal((UInt16)65535, readItems[1].Value);
    }

    [TestOrder(22)]
    [Fact]
    public void WriteMultipleVars_Empty_NoThrow()
    {
        Assert.NotNull(_client);
        _client!.WriteMultipleVars(new List<DataItem>());
        // 无异常即通过
    }
    #endregion

    #region ReadMultipleVars 空列表不抛出
    [Fact]
    public void ReadMultipleVars_Empty_NoThrow()
    {
        // 不需要 server，直接 new client（不连接）
        using var client = new S7Client(CpuType.S7200, "127.0.0.1", 19999);
        // 空列表不发起网络请求，不抛出
        client.ReadMultipleVars(new List<DataItem>());
    }

    [Fact]
    public void ReadMultipleVars_Null_NoThrow()
    {
        using var client = new S7Client(CpuType.S7200, "127.0.0.1", 19999);
        client.ReadMultipleVars(null!);
    }
    #endregion
}
