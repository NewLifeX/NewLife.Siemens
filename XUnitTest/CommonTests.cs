using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Siemens.Models;
using Xunit;

namespace XUnitTest;

public class CommonTests
{
    #region PlcException - tested indirectly since Common folder is excluded, but PlcException.cs may still be accessible
    // PlcException is in Common namespace which is excluded from compilation
    // Test basic enum-based ErrorCode behavior instead
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
    #endregion
}
