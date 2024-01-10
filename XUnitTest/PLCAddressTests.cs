using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using Xunit;

namespace XUnitTest;

public class PLCAddressTests
{
    [Fact]
    public void Test1()
    {
        var addr = new PLCAddress("DB1.DBD32");

        Assert.Equal(DataType.DataBlock, addr.DataType);
        Assert.Equal(1, addr.DbNumber);
        Assert.Equal(32, addr.StartByte);
        Assert.Equal(VarType.DWord, addr.VarType);
        Assert.Equal(-1, addr.BitNumber);
    }

    [Fact]
    public void Test2()
    {
        var addr = new PLCAddress("DB1.DBX5.0");

        Assert.Equal(DataType.DataBlock, addr.DataType);
        Assert.Equal(1, addr.DbNumber);
        Assert.Equal(5, addr.StartByte);
        Assert.Equal(VarType.Bit, addr.VarType);
        Assert.Equal(0, addr.BitNumber);
    }

    [Fact]
    public void Test3()
    {
        var addr = new PLCAddress("DB1.DBX5.1");

        Assert.Equal(DataType.DataBlock, addr.DataType);
        Assert.Equal(1, addr.DbNumber);
        Assert.Equal(5, addr.StartByte);
        Assert.Equal(VarType.Bit, addr.VarType);
        Assert.Equal(1, addr.BitNumber);
    }
}
