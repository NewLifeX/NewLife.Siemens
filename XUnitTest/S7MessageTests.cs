﻿using System.IO;
using NewLife;
using NewLife.Siemens.Protocols;
using Xunit;

namespace XUnitTest;

public class S7MessageTests
{
    [Fact]
    public void Test1()
    {
        var str = "32 01 00 00 ff ff 00 08 00 00 f0 00 00 03 00 03 03 c0";
        var hex = str.ToHex();

        var msg = new S7Message();

        var rs = msg.Read(new MemoryStream(hex), null);
        Assert.True(rs);

        Assert.Equal(0x32, msg.ProtocolId);
        Assert.Equal(S7Kinds.Job, msg.Kind);
        Assert.Equal(0x0000, msg.Reserved);
        Assert.Equal(0xFFFF, msg.Sequence);

        Assert.Single(msg.Parameters);

        var pm = msg.Parameters[0] as S7SetupParameter;
        Assert.NotNull(pm);
        Assert.Equal(S7Functions.Setup, pm.Code);
        Assert.Equal(3, pm.MaxAmqCaller);
        Assert.Equal(3, pm.MaxAmqCallee);
        Assert.Equal(0x03C0, pm.PduLength);

        Assert.Null(msg.Data);

        // 序列化
        var buf = msg.GetBytes();
        Assert.Equal(hex.ToHex(), buf.ToHex());
    }
}
