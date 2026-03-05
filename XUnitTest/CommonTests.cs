using System;
using System.IO;
using NewLife.Siemens.Models;
using Xunit;

namespace XUnitTest;

public class CommonTests
{
    #region ErrorCode
    [Fact]
    public void ErrorCode_AllValues_AreDefined()
    {
        var values = Enum.GetValues(typeof(ErrorCode));
        Assert.Equal(9, values.Length);
    }

    [Fact]
    public void ErrorCode_CanCastToInt()
    {
        Assert.Equal(0, (Int32)ErrorCode.NoError);
        Assert.Equal(50, (Int32)ErrorCode.WriteData);
    }

    [Fact]
    public void ErrorCode_AllMembers_HaveExpectedValues()
    {
        Assert.Equal(1, (Int32)ErrorCode.WrongCPU_Type);
        Assert.Equal(2, (Int32)ErrorCode.ConnectionError);
        Assert.Equal(3, (Int32)ErrorCode.IPAddressNotAvailable);
        Assert.Equal(10, (Int32)ErrorCode.WrongVarFormat);
        Assert.Equal(11, (Int32)ErrorCode.WrongNumberReceivedBytes);
        Assert.Equal(20, (Int32)ErrorCode.SendData);
        Assert.Equal(30, (Int32)ErrorCode.ReadData);
    }

    [Fact]
    public void ErrorCode_Enum_ContainsKey()
    {
        Assert.True(Enum.IsDefined(typeof(ErrorCode), 0));
        Assert.True(Enum.IsDefined(typeof(ErrorCode), 50));
        Assert.False(Enum.IsDefined(typeof(ErrorCode), 99));
    }
    #endregion
}
