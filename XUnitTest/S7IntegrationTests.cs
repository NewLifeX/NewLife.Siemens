using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using NewLife.Log;
using NewLife.Siemens.Messages;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using NewLife.UnitTest;
using Xunit;

namespace XUnitTest;

/// <summary>端到端集成测试：基于内置 S7Server 模拟器，无需真实 PLC 硬件</summary>
[TestCaseOrderer("NewLife.UnitTest.PriorityOrderer", "NewLife.UnitTest")]
public class S7IntegrationTests
{
    private static S7Server? _server;
    private static S7Client? _client;
    private const Int32 TestPort = 10236;

    #region 01 — 服务端启动与数据预置
    [TestOrder(1)]
    [Fact]
    public void E2E_StartServer()
    {
        var server = new S7Server
        {
            Port = TestPort,
            Log = XTrace.Log,
        };

        // ── DB1 数据预置 ──────────────────────────────────────────────
        // DB1.DBW0  = 2024       (Int16, 用于 ReadBytes 回环验证)
        server.SetValue("DB1.DBW0", (Int16)2024);
        // DB1.DBW2  = -999       (负数 Int16)
        server.SetValue("DB1.DBW2", (Int16)(-999));
        // DB1.DBD10 = 9.99f      (Single / Real)
        server.SetValue("DB1.DBD10", 9.99f);
        // DB1.DBX20.0 = true     (Bit)
        server.SetValue("DB1.DBX20.0", new Byte[] { 1 });
        // DB1.DBX20.5 = false    (Bit)
        server.SetValue("DB1.DBX20.5", new Byte[] { 0 });

        // ── Memory 区域 ───────────────────────────────────────────────
        server.SetValue("MB0", new Byte[] { 0xCA, 0xFE }); // MW0 = 0xCAFE

        server.Start();
        _server = server;

        Thread.Sleep(100); // 等待服务端进入监听状态
        Assert.True(server.Active);
    }
    #endregion

    #region 02 — 客户端连接
    [TestOrder(2)]
    [Fact]
    public async void E2E_Connect()
    {
        var client = new S7Client(CpuType.S7200, "127.0.0.1", TestPort)
        {
            Log = XTrace.Log,
        };

        await client.OpenAsync();
        _client = client;

        // PDU 已协商完毕，且 > 0
        Assert.True(client.MaxPDUSize > 0, $"MaxPDUSize={client.MaxPDUSize} 应 > 0");
    }
    #endregion

    #region 03 — 字节级读写回环
    [TestOrder(3)]
    [Fact]
    public void E2E_ReadBytes_DBW0()
    {
        Assert.NotNull(_client);
        var addr = new PLCAddress("DB1.DBW0");
        var data = _client!.ReadBytes(addr, 2);

        Assert.Equal(2, data.Length);
        // 2024 = 0x07E8，大端 => {0x07, 0xE8}
        Assert.Equal(0x07, data[0]);
        Assert.Equal(0xE8, data[1]);
    }

    [TestOrder(4)]
    [Fact]
    public void E2E_WriteBytes_And_ReadBytes_Roundtrip()
    {
        Assert.NotNull(_client);
        var addr = new PLCAddress("DB1.DBW50");
        var writeData = new Byte[] { 0x12, 0x34 };

        _client!.WriteBytes(addr, writeData);

        var readData = _client.ReadBytes(addr, 2);
        Assert.Equal(writeData, readData);
    }

    [TestOrder(5)]
    [Fact]
    public void E2E_ReadBytes_MultiSegment()
    {
        Assert.NotNull(_client);

        // 写入连续 10 个字节到 DB1.DBB60
        var addr = new PLCAddress("DB1.DBB60");
        var expected = new Byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        _client!.WriteBytes(addr, expected);

        var actual = _client.ReadBytes(addr, 10);
        Assert.Equal(expected, actual);
    }
    #endregion

    #region 04 — 高层 API 读写
    [TestOrder(6)]
    [Fact]
    public void E2E_Read_Int16_Positive()
    {
        Assert.NotNull(_client);
        var val = _client!.Read<Int16>("DB1.DBW0");
        Assert.Equal((Int16)2024, val);
    }

    [TestOrder(7)]
    [Fact]
    public void E2E_Read_Int16_Negative()
    {
        Assert.NotNull(_client);
        var val = _client!.Read<Int16>("DB1.DBW2");
        Assert.Equal((Int16)(-999), val);
    }

    [TestOrder(8)]
    [Fact]
    public void E2E_Read_Single()
    {
        Assert.NotNull(_client);
        var val = _client!.Read<Single>("DB1.DBD10");
        Assert.Equal(9.99f, val, precision: 4);
    }

    [TestOrder(9)]
    [Fact]
    public void E2E_Read_UInt16_Memory()
    {
        Assert.NotNull(_client);
        var val = _client!.Read<UInt16>("MW0");
        Assert.Equal((UInt16)0xCAFE, val);
    }

    [TestOrder(10)]
    [Fact]
    public void E2E_Write_Int16_And_Verify()
    {
        Assert.NotNull(_client);
        _client!.Write("DB1.DBW100", (Int16)7788);
        var val = _client.Read<Int16>("DB1.DBW100");
        Assert.Equal((Int16)7788, val);
    }

    [TestOrder(11)]
    [Fact]
    public void E2E_Write_Single_And_Verify()
    {
        Assert.NotNull(_client);
        _client!.Write("DB1.DBD110", -273.15f);
        var val = _client.Read<Single>("DB1.DBD110");
        Assert.Equal(-273.15f, val, precision: 3);
    }

    [TestOrder(12)]
    [Fact]
    public void E2E_Write_UInt16_MaxValue()
    {
        Assert.NotNull(_client);
        _client!.Write("DB1.DBW120", UInt16.MaxValue);
        var val = _client.Read<UInt16>("DB1.DBW120");
        Assert.Equal(UInt16.MaxValue, val);
    }
    #endregion

    #region 05 — 批量读写
    [TestOrder(13)]
    [Fact]
    public void E2E_BatchRead_MultipleVars()
    {
        Assert.NotNull(_client);

        var items = new List<DataItem>
        {
            DataItem.Create(DataType.DataBlock, VarType.Int, 1, 0),   // DB1.DBW0 = 2024
            DataItem.Create(DataType.DataBlock, VarType.Int, 1, 2),   // DB1.DBW2 = -999
            DataItem.Create(DataType.Memory,    VarType.Byte, 0, 0),  // MB0 = 0xCA
        };

        _client!.ReadMultipleVars(items);

        Assert.Equal((Int16)2024, items[0].Value);
        Assert.Equal((Int16)(-999), items[1].Value);
        Assert.Equal((Byte)0xCA, items[2].Value);
    }

    [TestOrder(14)]
    [Fact]
    public void E2E_BatchWrite_RoundTrip()
    {
        Assert.NotNull(_client);

        var writeItems = new List<DataItem>
        {
            DataItem.Create(DataType.DataBlock, VarType.Int, 1, 200),
            DataItem.Create(DataType.DataBlock, VarType.Int, 1, 202),
            DataItem.Create(DataType.DataBlock, VarType.Int, 1, 204),
        };
        writeItems[0].Value = (Int16)555;
        writeItems[1].Value = (Int16)666;
        writeItems[2].Value = (Int16)777;

        _client!.WriteMultipleVars(writeItems);

        // 通过高层 API 单次验证
        Assert.Equal((Int16)555, _client.Read<Int16>("DB1.DBW200"));
        Assert.Equal((Int16)666, _client.Read<Int16>("DB1.DBW202"));
        Assert.Equal((Int16)777, _client.Read<Int16>("DB1.DBW204"));
    }
    #endregion

    #region 06 — 字符串读写
    [TestOrder(15)]
    [Fact]
    public void E2E_WriteString_And_ReadBack()
    {
        Assert.NotNull(_client);
        _client!.WriteString("DB1.STRING300.50", "Siemens S7", 50);
        var str = _client.ReadString("DB1.STRING300.50", 50);
        Assert.Equal("Siemens S7", str);
    }
    #endregion

    #region 07 — 断线自动重连
    [TestOrder(16)]
    [Fact]
    public void E2E_AutoReconnect_After_ForceClose()
    {
        Assert.NotNull(_client);

        // 强制关闭底层连接
        _client!.Close();

        // 下次 Read 会自动触发 OpenAsync → 重连
        var val = _client.Read<Int16>("DB1.DBW0");
        Assert.Equal((Int16)2024, val);
    }
    #endregion

    #region 08 — 大批量超 PDU 分段
    [TestOrder(17)]
    [Fact]
    public void E2E_BatchRead_25Items_AutoSplit()
    {
        Assert.NotNull(_client);

        var items = new List<DataItem>();
        for (var i = 0; i < 25; i++)
            items.Add(DataItem.Create(DataType.DataBlock, VarType.Int, 1, i * 2));

        // 不应抛出异常，自动拆成 2 批（20 + 5）
        _client!.ReadMultipleVars(items);
        Assert.Equal(25, items.Count);
    }
    #endregion

    #region 09 — 干净断开
    [TestOrder(18)]
    [Fact]
    public void E2E_CleanDisconnect()
    {
        Assert.NotNull(_client);

        // Close 后不应有任何异常
        _client!.Close();
        _client = null;
    }

    [TestOrder(19)]
    [Fact]
    public void E2E_StopServer()
    {
        Assert.NotNull(_server);
        _server!.Stop("test done");
        _server = null;
    }
    #endregion
}
