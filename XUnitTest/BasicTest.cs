using System;
using System.IO;
using System.Linq;
using NewLife;
using NewLife.Data;
using NewLife.Serialization;
using NewLife.Siemens.Messages;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using Xunit;

namespace XUnitTest;

public class BasicTest
{
    [Fact]
    public void PLCAddress_ParseStatic()
    {
        PLCAddress.Parse("DB1.DBB0", out var dataType, out var dbNumber, out var varType, out var address, out var bitNumber);
        Assert.Equal(DataType.DataBlock, dataType);
        Assert.Equal(1, dbNumber);
        Assert.Equal(VarType.Byte, varType);
        Assert.Equal(0, address);
        Assert.Equal(-1, bitNumber);
    }

    [Fact]
    public void TPKT_COTP_S7Message_FullStack()
    {
        // Build a complete S7 message wrapped in COTP and TPKT
        var msg = new S7Message
        {
            Kind = S7Kinds.Job,
            Sequence = 1
        };
        msg.Setup(1, 960);

        var cotp = msg.ToCOTP();
        Assert.Equal(PduType.Data, cotp.Type);
        Assert.True(cotp.LastDataUnit);

        var pk = cotp.ToPacket(true);
        var bytes = pk.ToArray();

        // Parse back
        var tpkt = new TPKT();
        tpkt.Read(new Packet(bytes));
        Assert.Equal(3, tpkt.Version);

        var cotp2 = new COTP();
        cotp2.Read(tpkt.Data);
        Assert.Equal(PduType.Data, cotp2.Type);

        var msg2 = new S7Message();
        msg2.Read(cotp2.Data);
        Assert.Equal(S7Kinds.Job, msg2.Kind);
        Assert.Equal(1, msg2.Sequence);
        Assert.Single(msg2.Parameters);

        var pm = msg2.Parameters[0] as SetupMessage;
        Assert.NotNull(pm);
        Assert.Equal(1, pm.MaxAmqCaller);
        Assert.Equal(1, pm.MaxAmqCallee);
        Assert.Equal(960, pm.PduLength);
    }

    [Fact]
    public void EnumValues_Consistent()
    {
        // Quick sanity check that key enum values haven't changed
        Assert.Equal(132, (Int32)DataType.DataBlock);
        Assert.Equal(1, (Byte)VarType.Bit);
        Assert.Equal(0x32, new S7Message().ProtocolId);
    }
}