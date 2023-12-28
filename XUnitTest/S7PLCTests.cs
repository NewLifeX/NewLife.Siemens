using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using Xunit;

namespace XUnitTest;

public class S7PLCTests
{
    [Fact]
    public async void Read()
    {
        var s7 = new S7PLC(CpuType.S7200Smart, "127.0.0.1", 102);

        await s7.OpenAsync();
    }
}
