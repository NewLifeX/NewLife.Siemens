using System;
using System.Threading;
using NewLife.Log;
using NewLife.Siemens.Messages;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using S7.Net;
using Xunit;
using DataType = NewLife.Siemens.Models.DataType;
using VarType = NewLife.Siemens.Models.VarType;

namespace XUnitTest;

/// <summary>竞品交叉兼容测试：验证 NewLife.Siemens 与 S7netplus 的互操作性</summary>
/// <remarks>
/// 交叉模式矩阵（网络协议 N1-N4）：
/// - N2（你→我）：S7netplus 客户端连接 NewLife.Siemens S7Server —— 最关键，证明我们兼容业界标准
/// - N3（我→我）：S7Client ↔ S7Server 自通信（已有 S7IntegrationTests 覆盖）
///
/// 竞品 S7netplus 是 .NET 生态中 Star 数最多的 S7 协议库（1600+ Star），
/// 通过交叉测试验证：第三方客户端能否与我们的服务器正常通信。
/// </remarks>
[Trait("Category", "CrossCompatibility")]
public class S7CrossCompatibilityTests
{
    #region 辅助
    private S7Server CreateServer(Int32 port)
    {
        var server = new S7Server
        {
            Port = port,
            Log = XTrace.Log,
        };
        return server;
    }

    private S7Client CreateClient(CpuType cpu, Int32 port)
    {
        return new S7Client(cpu, "127.0.0.1", port)
        {
            Timeout = 10_000,
            Log = XTrace.Log,
        };
    }

    /// <summary>等待服务器启动</summary>
    private void WaitForServer(Int32 port, Int32 timeoutMs = 5000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                client.Connect("127.0.0.1", port);
                return;
            }
            catch
            {
                Thread.Sleep(200);
            }
        }
    }
    #endregion

    #region N2 — S7netplus 客户端 ↔ NewLife.Siemens S7Server

    /// <summary>S7netplus Plc 客户端可以连接本库 S7Server 并读取预置数据</summary>
    /// <remarks>
    /// 交叉测试最核心用例：证明第三方 S7 协议客户端（S7netplus）能与我们的 S7Server 正常通信。
    /// S7netplus 使用 `Plc(CpuType, IP, port, rack, slot)` 构造函数，支持自定义端口。
    /// </remarks>
    [Fact]
    [DisplayName("N2-连接与读取_S7netplus可连接S7Server并读取预置数据")]
    public void N2_ConnectAndRead_S7netplus_Connects_And_Reads()
    {
        // ═══ 1. 启动 S7Server + 预置数据 ═══
        const Int32 port = 10238;
        using var server = CreateServer(port);
        server.Start();
        WaitForServer(port);

        server.SetValue("DB1.DBW0", (Int16)2026);           // Int16
        server.SetValue("DB1.DBD10", 3.14f);                // Single
        server.SetValue("DB1.DBD20", 100000);                // Int32
        server.SetValue("DB1.DBX5.0", new Byte[] { 1 });    // Bit = true
        server.SetValue("DB1.DBD30", 2.718281828459045);     // Double

        // ═══ 2. S7netplus 客户端连接并读取 ═══
        // S7netplus 版本 0.20.0 支持端口参数构造函数：
        //   Plc(CpuType cpu, string ip, int port, Int16 rack, Int16 slot)
        using var s7net = new Plc(S7.Net.CpuType.S71200, "127.0.0.1", port, 0, 0);
        s7net.Open();

        try
        {
            Assert.True(s7net.IsConnected);

            // 读取 Int16 (word)
            var wordVal = (Int16)s7net.Read("DB1.DBW0");
            Assert.Equal((Int16)2026, wordVal);

            // 读取 Int32 (dword)
            var dintVal = (Int32)s7net.Read("DB1.DBD20");
            Assert.Equal(100000, dintVal);

            // 读取 Single (real)
            var realVal = (Single)s7net.Read("DB1.DBD10");
            Assert.Equal(3.14f, realVal, 3);

            // 读取位
            var bit0Val = (Boolean)s7net.Read("DB1.DBX5.0");
            Assert.True(bit0Val);

            var bit1Val = (Boolean)s7net.Read("DB1.DBX5.1");
            Assert.False(bit1Val);
        }
        finally
        {
            s7net.Close();
        }
    }

    /// <summary>S7netplus Plc 写入数据后，本库 S7Client 能正确读回</summary>
    /// <remarks>
    /// 验证 S7netplus 写入的数据格式与我们的 S7Client 读格式兼容。
    /// 这是真正的跨库互操作验证：第三方的写 → 我们的读。
    /// </remarks>
    [Fact]
    [DisplayName("N2-写入回读_S7netplus写入后本库S7Client可读回")]
    public void N2_WriteThenRead_OurClient_Reads_S7netplus_Written_Data()
    {
        // ═══ 1. 启动 S7Server ═══
        const Int32 port = 10239;
        using var server = CreateServer(port);
        server.Start();
        WaitForServer(port);

        // ═══ 2. S7netplus 客户端写入数据 ═══
        using var s7net = new Plc(S7.Net.CpuType.S71200, "127.0.0.1", port, 0, 0);
        s7net.Open();

        try
        {
            // S7netplus Write 方法：Write(string variable, object value)
            s7net.Write("DB2.DBW0", (Int16)(-999));
            s7net.Write("DB2.DBD10", 123.456f);
            s7net.Write("DB2.DBD20", -50000);
            s7net.Write("DB2.DBD30", 1.2345);  // Double
        }
        finally
        {
            s7net.Close();
        }

        // ═══ 3. 本库 S7Client 读回并验证 ═══
        using var ourClient = CreateClient(CpuType.S71200, port);
        ourClient.OpenAsync().Wait();

        var readBackInt16 = ourClient.Read<Int16>("DB2.DBW0");
        Assert.Equal((Int16)(-999), readBackInt16);

        var readBackSingle = ourClient.Read<Single>("DB2.DBD10");
        Assert.Equal(123.456f, readBackSingle, 3);

        var readBackInt32 = ourClient.Read<Int32>("DB2.DBD20");
        Assert.Equal(-50000, readBackInt32);

        var readBackDouble = ourClient.Read<Double>("DB2.DBD30");
        Assert.Equal(1.2345, readBackDouble, 6);

        ourClient.Close();
    }

    /// <summary>S7netplus 读取与本库 S7Client 读取结果逐字节一致</summary>
    /// <remarks>
    /// 对同一 S7Server，分别用 S7netplus 和本库 S7Client 读取相同地址的原始字节，
    /// 验证两库在协议实现层面的一致性。
    /// </remarks>
    [Fact]
    [DisplayName("N2-字节级一致_S7netplus与本库ReadBytes结果一致")]
    public void N2_ByteLevelConsistency_S7netplus_And_OurClient_SameBytes()
    {
        const Int32 port = 10243;
        using var server = CreateServer(port);
        server.Start();
        WaitForServer(port);

        // 预置一段确定性数据
        var testData = new Byte[20];
        for (var i = 0; i < testData.Length; i++)
            testData[i] = (Byte)(i + 1);
        server.SetValue("DB4.DBB0", testData);

        // ═══ S7netplus 读取 ═══
        Byte[] s7netBytes;
        using (var s7net = new Plc(S7.Net.CpuType.S71200, "127.0.0.1", port, 0, 0))
        {
            s7net.Open();
            // S7netplus ReadBytes: (DataType, DB, startByteAdr, count)
            s7netBytes = s7net.ReadBytes(S7.Net.DataType.DataBlock, 4, 0, 20);
            s7net.Close();
        }

        // ═══ 本库读取 ═══
        using var ourClient = CreateClient(CpuType.S71200, port);
        ourClient.OpenAsync().Wait();
        var ourBytes = ourClient.ReadBytes(new PLCAddress("DB4.DBB0"), 20);
        ourClient.Close();

        // ═══ 逐字节对比 ═══
        Assert.Equal(20, s7netBytes.Length);
        Assert.Equal(20, ourBytes.Length);
        for (var i = 0; i < 20; i++)
            Assert.Equal(s7netBytes[i], ourBytes[i]);
    }

    /// <summary>批量多变量读写：N2 模式下跨库一致性验证</summary>
    [Fact]
    [DisplayName("N2-批量读写_S7Server多变量批量读写一致性")]
    public void N2_BatchReadWrite_Consistency()
    {
        // ═══ 准备 ═══
        const Int32 port = 10240;
        using var server = CreateServer(port);
        server.Start();
        WaitForServer(port);

        // 预置多个地址的数据
        server.SetValue("DB3.DBW0", (Int16)10);
        server.SetValue("DB3.DBW2", (Int16)20);
        server.SetValue("DB3.DBW4", (Int16)30);
        server.SetValue("DB3.DBW6", (Int16)40);
        server.SetValue("DB3.DBW8", (Int16)50);

        // ═══ 先由 S7netplus 读取各地址 ═══
        using (var s7net = new Plc(S7.Net.CpuType.S71200, "127.0.0.1", port, 0, 0))
        {
            s7net.Open();
            var v0 = (Int16)s7net.Read("DB3.DBW0");
            var v2 = (Int16)s7net.Read("DB3.DBW2");
            var v4 = (Int16)s7net.Read("DB3.DBW4");
            s7net.Close();

            Assert.Equal((Int16)10, v0);
            Assert.Equal((Int16)20, v2);
            Assert.Equal((Int16)30, v4);
        }

        // ═══ 再由本库 S7Client 批量读取 ═══
        using var ourClient = CreateClient(CpuType.S71200, port);
        ourClient.OpenAsync().Wait();

        var items = new[]
        {
            DataItem.Create(DataType.DataBlock, VarType.Word, 3, 0),
            DataItem.Create(DataType.DataBlock, VarType.Word, 3, 2),
            DataItem.Create(DataType.DataBlock, VarType.Word, 3, 4),
            DataItem.Create(DataType.DataBlock, VarType.Word, 3, 6),
            DataItem.Create(DataType.DataBlock, VarType.Word, 3, 8),
        };

        ourClient.ReadMultipleVars(items);

        // 验证与本库读取一致（与 S7netplus 读取结果吻合）
        Assert.Equal((Int16)10, items[0].Value);
        Assert.Equal((Int16)20, items[1].Value);
        Assert.Equal((Int16)30, items[2].Value);
        Assert.Equal((Int16)40, items[3].Value);
        Assert.Equal((Int16)50, items[4].Value);

        ourClient.Close();
    }
    #endregion

    #region N3 — 自通信扩展验证（补充边界场景）

    /// <summary>S7Server 大块数据读写超过 PDU 限制时自动分段</summary>
    [Fact]
    [DisplayName("N3-超PDU分段_S7Server大块数据自动分段读写")]
    public void N3_LargeData_AutoSegmentation()
    {
        const Int32 port = 10241;
        using var server = CreateServer(port);
        server.Start();
        WaitForServer(port);

        using var client = CreateClient(CpuType.S71200, port);
        client.OpenAsync().Wait();

        // 写入 500 字节数据（超过默认 PDU 240 的限制）
        var writeData = new Byte[500];
        for (var i = 0; i < writeData.Length; i++)
            writeData[i] = (Byte)(i % 256);

        var addr = new PLCAddress("DB5.DBB0");
        client.WriteBytes(addr, writeData);

        // 读回并验证
        var readData = client.ReadBytes(addr, 500);
        Assert.Equal(writeData.Length, readData.Length);
        for (var i = 0; i < writeData.Length; i++)
            Assert.Equal(writeData[i], readData[i]);

        client.Close();
    }

    /// <summary>S7Server 位操作正确性（Set/Get Bit）</summary>
    [Fact]
    [DisplayName("N3-位操作_S7Server位读写正确性")]
    public void N3_BitOperations_Correctness()
    {
        const Int32 port = 10242;
        using var server = CreateServer(port);
        server.Start();
        WaitForServer(port);

        // 预置位数据
        server.SetValue("DB10.DBX0.0", new Byte[] { 1 });  // bit 0 = true
        server.SetValue("DB10.DBX0.3", new Byte[] { 1 });  // bit 3 = true
        server.SetValue("DB10.DBX0.7", new Byte[] { 0 });  // bit 7 = false

        using var client = CreateClient(CpuType.S71200, port);
        client.OpenAsync().Wait();

        // 读取位
        var bit0 = client.Read<Boolean>("DB10.DBX0.0");
        var bit3 = client.Read<Boolean>("DB10.DBX0.3");
        var bit7 = client.Read<Boolean>("DB10.DBX0.7");

        Assert.True(bit0);
        Assert.True(bit3);
        Assert.False(bit7);

        // 写入位然后读回
        client.Write("DB10.DBX0.5", true);
        var bit5 = client.Read<Boolean>("DB10.DBX0.5");
        Assert.True(bit5);

        client.Close();
    }

    /// <summary>S7Server 不同 CPU 类型的 TSAP 协商都正常</summary>
    [Fact]
    [DisplayName("N3-多CPU类型_不同CPU类型TSAP协商均正常")]
    public void N3_MultipleCpuTypes_TsapNegotiation()
    {
        var cpuTypes = new[] { CpuType.S7200, CpuType.S7200Smart, CpuType.S7300, CpuType.S7400, CpuType.S71200, CpuType.S71500, CpuType.Logo0BA8 };

        foreach (var cpuType in cpuTypes)
        {
            // 每个 CPU 类型用不同端口避免冲突
            var port = 10250 + (Int32)cpuType;
            using var server = CreateServer(port);
            server.Start();
            WaitForServer(port);

            server.SetValue("M0.0", new Byte[] { 1 });

            using var client = CreateClient(cpuType, port);
            client.OpenAsync().Wait();

            // 简单读取验证连接正常
            var bytes = client.ReadBytes(new PLCAddress("MB0"), 1);
            Assert.Single(bytes);

            client.Close();
        }
    }
    #endregion

    #region F1 — S7Client 协议帧级验证（我写 → 竞品可读）
    // 注：完整 N1 测试需将 S7Client 发出的帧交由 S7netplus 服务端解析，
    // 但 S7netplus 不提供 Server 模式。此处通过 S7Server 验证字节帧的正确性。
    #endregion
}
