using NewLife.Log;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using NewLife.UnitTest;
using Xunit;

namespace XUnitTest;

[TestCaseOrderer("NewLife.UnitTest.PriorityOrderer", "NewLife.UnitTest")]
public class S7PLCTests
{
    S7Server _server;

    [TestOrder(1)]
    [Fact]
    public void StartServer()
    {
        var server = new S7Server
        {
            Log = XTrace.Log,
            SessionLog = XTrace.Log,
            SocketLog = XTrace.Log,

            LogSend = true,
            LogReceive = true,
        };

        server.Start();

        _server = server;
    }

    //[TestOrder(2)]
    //[Fact]
    //public async void Read()
    //{
    //    var s7 = new S7PLC(CpuType.S7200Smart, "127.0.0.1", 102);

    //    await s7.OpenAsync();
    //}

    [TestOrder(3)]
    [Fact]
    public async void S7200SmartTest()
    {
        var s7 = new S7PLC(CpuType.S7200, "127.0.0.1", 102);

        await s7.OpenAsync();
    }
}
