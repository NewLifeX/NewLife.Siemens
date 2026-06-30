using BenchmarkDotNet.Attributes;
using NewLife.Log;
using NewLife.Siemens.Messages;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using S7.Net;
using DataType = NewLife.Siemens.Models.DataType;
using VarType = NewLife.Siemens.Models.VarType;

namespace BenchmarkTest;

/// <summary>NewLife.Siemens vs S7netplus 性能对比基准</summary>
/// <remarks>
/// 对比 NewLife.Siemens 与 S7netplus 在典型工控场景下的性能表现。
///
/// 测试方法：
/// - 本地操作（无网络）：地址解析、消息构建的吞吐量与内存分配
/// - 网络操作（本地 S7Server）：读写延迟、批量读取吞吐量
///
/// 运行方式（Release 模式）：
///   dotnet run -c Release --project BenchmarkTest --filter *CrossLibBenchmarks*
/// </remarks>
[MemoryDiagnoser]
[ShortRunJob]
public class CrossLibBenchmarks
{
    private S7Server? _server;
    private S7Client? _ourClient;
    private S7.Net.Plc? _s7netClient;
    private const Int32 Port = 10280;

    #region 生命周期
    /// <summary>启动 S7Server 并预置测试数据，建立双方客户端连接</summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        // 启动 S7Server
        _server = new S7Server
        {
            Port = Port,
            Log = XTrace.Log,
        };
        _server.Start();

        // 等待服务器就绪
        var deadline = Environment.TickCount64 + 5000;
        while (Environment.TickCount64 < deadline)
        {
            try
            {
                using var test = new System.Net.Sockets.TcpClient();
                test.Connect("127.0.0.1", Port);
                break;
            }
            catch { Thread.Sleep(100); }
        }

        // 预置测试数据
        _server.SetValue("DB1.DBW0", (Int16)3456);
        _server.SetValue("DB1.DBD10", 123.456f);
        _server.SetValue("DB1.DBD20", 9876543);

        // 建立我方客户端连接
        _ourClient = new S7Client(CpuType.S71200, "127.0.0.1", Port)
        {
            Timeout = 10_000,
            Log = XTrace.Log,
        };
        _ourClient.OpenAsync().Wait();

        // 建立 S7netplus 客户端连接
        _s7netClient = new S7.Net.Plc(S7.Net.CpuType.S71200, "127.0.0.1", Port, 0, 0);
        _s7netClient.Open();
    }

    /// <summary>断开连接并停止服务器</summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _s7netClient?.Close();
        _s7netClient?.Dispose();
        _ourClient?.Close();
        _ourClient?.Dispose();
        _server?.Dispose();
    }
    #endregion

    #region 本地操作（无网络 IO）

    /// <summary>NewLife.Siemens PLCAddress 解析——典型 DB Word 地址</summary>
    [Benchmark(Description = "PLCAddress.Parse_DBWord")]
    public PLCAddress Our_ParseDBWord()
    {
        return new PLCAddress("DB1.DBW10");
    }

    /// <summary>NewLife.Siemens PLCAddress 解析——位地址</summary>
    [Benchmark(Description = "PLCAddress.Parse_DBBit")]
    public PLCAddress Our_ParseDBBit()
    {
        return new PLCAddress("DB1.DBX0.3");
    }

    /// <summary>NewLife.Siemens ReadRequest 构建</summary>
    [Benchmark(Description = "Build_ReadRequest_Word")]
    public ReadRequest Our_BuildReadRequest()
    {
        // 模拟构建一个 ReadRequest（Word 类型）
        var addr = new PLCAddress("DB1.DBW0");
        return BuildReadRequest(addr.DataType, addr.DbNumber, addr.VarType, addr.StartByte * 8);
    }

    private static ReadRequest BuildReadRequest(DataType dataType, Int32 db, VarType varType, Int32 address)
    {
        var ri = new RequestItem
        {
            SyntaxId = 0x10,
            TransportSize = 2, // Byte
            Count = 1,
            DbNumber = (UInt16)db,
            Area = dataType,
            Address = (UInt32)address,
        };
        var request = new ReadRequest();
        request.Items.Add(ri);
        return request;
    }
    #endregion

    #region 网络操作（少量 IO，对比读写延迟）

    /// <summary>NewLife.Siemens —— 单次小读（Int16）</summary>
    [Benchmark(Description = "Read_Int16_Our")]
    public Int16 Our_ReadInt16()
    {
        return _ourClient!.Read<Int16>("DB1.DBW0");
    }

    /// <summary>S7netplus —— 单次小读（Int16）</summary>
    [Benchmark(Description = "Read_Int16_S7netplus")]
    public Int16 S7netplus_ReadInt16()
    {
        return (Int16)_s7netClient!.Read("DB1.DBW0");
    }

    /// <summary>NewLife.Siemens —— 单次读 Single</summary>
    [Benchmark(Description = "Read_Single_Our")]
    public Single Our_ReadSingle()
    {
        return _ourClient!.Read<Single>("DB1.DBD10");
    }

    /// <summary>S7netplus —— 单次读 Single</summary>
    [Benchmark(Description = "Read_Single_S7netplus")]
    public Single S7netplus_ReadSingle()
    {
        return (Single)_s7netClient!.Read("DB1.DBD10");
    }

    /// <summary>NewLife.Siemens —— 单次字节级写入</summary>
    [Benchmark(Description = "Write_Byte_Our")]
    public void Our_WriteByte()
    {
        _ourClient!.WriteBytes(new PLCAddress("DB1.DBB100"), new Byte[] { 0x42 });
    }

    /// <summary>S7netplus —— 单次字节级写入</summary>
    [Benchmark(Description = "Write_Byte_S7netplus")]
    public void S7netplus_WriteByte()
    {
        _s7netClient!.WriteBytes(S7.Net.DataType.DataBlock, 1, 100, new Byte[] { 0x42 });
    }
    #endregion
}
