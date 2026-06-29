using BenchmarkDotNet.Attributes;
using NewLife.Siemens.Protocols;

namespace BenchmarkTest;

/// <summary>S7 类型化读写基准测试（使用 S7Server 内存模拟）</summary>
/// <remarks>
/// 测试 Read&lt;T&gt; / Write / ReadStruct&lt;T&gt; 端到端性能。
/// 使用 S7Server 本地内存模式，无网络延迟，测量纯代码路径开销。
/// </remarks>
[MemoryDiagnoser]
[ShortRunJob]
public class S7TypeConversionBenchmarks
{
    private S7Client _client = null!;
    private S7Server _server = null!;

    [GlobalSetup]
    public void Setup()
    {
        _server = new S7Server { Port = 0 };
        _server.Start();

        // 预置测试数据
        _server.SetValue("DB1.DBW0", (Int16)12345);
        _server.SetValue("DB1.DBD4", 3.14159f);
        _server.SetValue("DB1.DBD8", (Int32)99999);
        _server.SetValue("M0.0", true);

        _client = new S7Client(Models.CpuType.S71200, "127.0.0.1", _server.Port);
        _client.OpenAsync().GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client.Close();
        _server.Stop("Benchmark done");
    }

    /// <summary>读取 Int16（典型温湿度值）</summary>
    [Benchmark]
    public Int16 Read_Int16()
    {
        return _client.Read<Int16>("DB1.DBW0");
    }

    /// <summary>读取 Single（模拟量）</summary>
    [Benchmark]
    public Single Read_Single()
    {
        return _client.Read<Single>("DB1.DBD4");
    }

    /// <summary>读取 Int32</summary>
    [Benchmark]
    public Int32 Read_Int32()
    {
        return _client.Read<Int32>("DB1.DBD8");
    }

    /// <summary>读取 Boolean 位</summary>
    [Benchmark]
    public Boolean Read_Boolean()
    {
        return _client.Read<Boolean>("M0.0");
    }

    /// <summary>写入 Int16</summary>
    [Benchmark]
    public void Write_Int16()
    {
        _client.Write("DB1.DBW0", (Int16)12345);
    }

    /// <summary>写入 Boolean 位</summary>
    [Benchmark]
    public void Write_Boolean()
    {
        _client.Write("M0.0", true);
    }
}
