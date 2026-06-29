using BenchmarkDotNet.Attributes;
using NewLife.Siemens.Protocols;

namespace BenchmarkTest;

/// <summary>PLC 地址解析基准测试</summary>
/// <remarks>
/// 测试地址字符串解析为 PLCAddress 的吞吐量与分配。
/// 地址解析是每次读写操作的必经路径，性能直接影响高频采集场景。
/// </remarks>
[MemoryDiagnoser]
[ShortRunJob]
public class PlcAddressBenchmarks
{
    // 覆盖各种地址格式的典型输入
    private static readonly String[] Addresses =
    [
        "DB1.DBB0",
        "DB1.DBW10",
        "DB1.DBD20",
        "DB1.DBX0.3",
        "DB100.DBB255",
        "MB0",
        "MW4",
        "MD8",
        "M10.5",
        "IB0",
        "IW2",
        "ID4",
        "QB0",
        "QW2",
        "QD4",
        "T10",
        "C5",
        "DB50.STRING0.60",
    ];

    /// <summary>单地址解析——最常用路径（如 DB1.DBW0）</summary>
    [Benchmark]
    public PLCAddress Parse_DB_Word()
    {
        return new PLCAddress("DB1.DBW10");
    }

    /// <summary>单地址解析——位地址</summary>
    [Benchmark]
    public PLCAddress Parse_DB_Bit()
    {
        return new PLCAddress("DB1.DBX0.3");
    }

    /// <summary>单地址解析——标志区位</summary>
    [Benchmark]
    public PLCAddress Parse_M_Bit()
    {
        return new PLCAddress("M10.5");
    }

    /// <summary>单地址解析——STRING 格式</summary>
    [Benchmark]
    public PLCAddress Parse_DB_String()
    {
        return new PLCAddress("DB50.STRING0.60");
    }

    /// <summary>批量解析 18 种典型地址</summary>
    [Benchmark]
    public PLCAddress[] Parse_All_Types()
    {
        var result = new PLCAddress[Addresses.Length];
        for (var i = 0; i < Addresses.Length; i++)
        {
            result[i] = new PLCAddress(Addresses[i]);
        }
        return result;
    }
}
