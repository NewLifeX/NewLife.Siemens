using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace BenchmarkTest;

/// <summary>NewLife.Siemens 性能基准测试入口</summary>
/// <remarks>
/// 运行方式（在仓库根目录）：
///   dotnet run -c Release --project BenchmarkTest
/// 
/// 或指定具体测试类：
///   dotnet run -c Release --project BenchmarkTest -- --filter *PlcAddress*
/// </remarks>
public class Program
{
    public static void Main(String[] args)
    {
        // 默认使用 Release 配置的 ManualConfig
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator); // 允许 Debug 下运行（CI 兼容）

        BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, config);
    }
}
