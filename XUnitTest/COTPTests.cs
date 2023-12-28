using System;
using NewLife;
using NewLife.Data;
using NewLife.Siemens.Protocols;
using Xunit;

namespace XUnitTest;

public class COTPTests
{
    [Fact]
    public void DtTest()
    {
        var cotp = new COTP
        {
            Type = PduType.Data
        };

        var buf = cotp.ToArray();

        var cotp2 = new COTP();
        var rs = cotp2.Read(buf);
        Assert.True(rs);
        Assert.Equal(cotp.Type, cotp2.Type);
    }

    [Fact]
    public void DecodeCR()
    {
        var str = "03 00 00 16 11 e0 00 00 00 01 00 c1 02 10 00 c2 02 03 00 c0 01 0a";
        var buf = str.ToHex();
        var pk = new Packet(buf);

        // 前面有TPKT头
        var tpkt = new TPKT();
        tpkt.Read(pk);
        Assert.Equal(3, tpkt.Version);
        Assert.Equal(0, tpkt.Reserved);
        Assert.Equal(0x16, tpkt.Length);

        var cotp = new COTP();
        var rs = cotp.Read(tpkt.Data);
        Assert.True(rs);
        Assert.Equal(PduType.ConnectionRequest, cotp.Type);
        Assert.Equal(0x0000, cotp.Destination);
        Assert.Equal(0x0001, cotp.Source);
        Assert.Equal(0x00, cotp.Option);

        var ps = cotp.Parameters;
        Assert.NotEmpty(ps);

        Assert.Equal(0xC1, ps[0].Type);
        Assert.Equal(0x1000, (UInt16)ps[0].Value);

        Assert.Equal(0xC2, ps[1].Type);
        Assert.Equal(0x0300, (UInt16)ps[1].Value);

        Assert.Equal(0xC0, ps[2].Type);
        Assert.Equal(0x0A, (Byte)ps[2].Value);
    }

    [Fact]
    public void DecodeCR2()
    {
        Byte[] plcHead1_200smart = [3, 0, 0, 22, 17, 224, 0, 0, 0, 1, 0, 193, 2, 16, 0, 194, 2, 3, 0, 192, 1, 10];
        var pk = new Packet(plcHead1_200smart);

        // 前面有TPKT头
        var tpkt = new TPKT();
        tpkt.Read(pk);
        Assert.Equal(3, tpkt.Version);
        Assert.Equal(0, tpkt.Reserved);
        Assert.Equal(0x16, tpkt.Length);

        var cotp = new COTP();
        var rs = cotp.Read(tpkt.Data);
        Assert.True(rs);
        Assert.Equal(PduType.ConnectionRequest, cotp.Type);
        Assert.Equal(0x0000, cotp.Destination);
        Assert.Equal(0x0001, cotp.Source);
        Assert.Equal(0x00, cotp.Option);

        var ps = cotp.Parameters;
        Assert.NotEmpty(ps);

        Assert.Equal(0xC1, ps[0].Type);
        Assert.Equal(0x1000, (UInt16)ps[0].Value);

        Assert.Equal(0xC2, ps[1].Type);
        Assert.Equal(0x0300, (UInt16)ps[1].Value);

        Assert.Equal(0xC0, ps[2].Type);
        Assert.Equal(0x0A, (Byte)ps[2].Value);
    }

    [Fact]
    public void DecodeCC()
    {
        var str = "03 00 00 16 11 d0 00 01 00 07 00 c0 01 0a c1 02 10 00 c2 02 03 00";
        var buf = str.ToHex();
        var pk = new Packet(buf);

        // 前面有TPKT头
        var tpkt = new TPKT();
        tpkt.Read(pk);
        Assert.Equal(3, tpkt.Version);
        Assert.Equal(0, tpkt.Reserved);
        Assert.Equal(0x16, tpkt.Length);

        var cotp = new COTP();
        var rs = cotp.Read(tpkt.Data);
        Assert.True(rs);
        Assert.Equal(PduType.ConnectionConfirmed, cotp.Type);
        Assert.Equal(0x0001, cotp.Destination);
        Assert.Equal(0x0007, cotp.Source);
        Assert.Equal(0x00, cotp.Option);

        var ps = cotp.Parameters;
        Assert.NotEmpty(ps);

        Assert.Equal(0xC0, ps[0].Type);
        Assert.Equal(0x0A, (Byte)ps[0].Value);

        Assert.Equal(0xC1, ps[1].Type);
        Assert.Equal(0x1000, (UInt16)ps[1].Value);

        Assert.Equal(0xC2, ps[2].Type);
        Assert.Equal(0x0300, (UInt16)ps[2].Value);
    }

    [Fact]
    public void DecodeDT()
    {
        Byte[] plcHead2_200smart = [3, 0, 0, 25, 2, 240, 128, 50, 1, 0, 0, 204, 193, 0, 8, 0, 0, 240, 0, 0, 1, 0, 1, 3, 192];
        var pk = new Packet(plcHead2_200smart);

        // 前面有TPKT头
        var tpkt = new TPKT();
        tpkt.Read(pk);
        Assert.Equal(3, tpkt.Version);
        Assert.Equal(0, tpkt.Reserved);
        Assert.Equal(25, tpkt.Length);

        var cotp = new COTP();
        var rs = cotp.Read(tpkt.Data);
        Assert.True(rs);
        Assert.Equal(PduType.Data, cotp.Type);
        Assert.True(cotp.LastDataUnit);

        Assert.NotNull(cotp.Data);
        Assert.Equal(17, cotp.Data.Total);
    }

    [Fact]
    public void ConnectTest()
    {
        var cotp = new COTP
        {
            Type = PduType.ConnectionRequest
        };

        var buf = cotp.ToArray();

        var cotp2 = new COTP();
        var rs = cotp2.Read(buf);
        Assert.True(rs);
        Assert.Equal(cotp.Type, cotp2.Type);
    }

    [Fact]
    public void ConfirmedTest()
    {
        var cotp = new COTP
        {
            Type = PduType.ConnectionConfirmed
        };

        var buf = cotp.ToArray();

        var cotp2 = new COTP();
        var rs = cotp2.Read(buf);
        Assert.True(rs);
        Assert.Equal(cotp.Type, cotp2.Type);
    }
}
