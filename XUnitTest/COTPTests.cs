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

        var pk = cotp.ToPacket(false);

        var cotp2 = new COTP();
        var rs = cotp2.Read(pk);
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
        Assert.Equal(pk.Slice(4).ToHex(), tpkt.Data.ToHex());

        var cotp = new COTP();
        var rs = cotp.Read(tpkt.Data);
        Assert.True(rs);
        Assert.Equal(PduType.ConnectionRequest, cotp.Type);
        Assert.Equal(0x0000, cotp.Destination);
        Assert.Equal(0x0001, cotp.Source);
        Assert.Equal(0x00, cotp.Option);

        var ps = cotp.Parameters;
        Assert.NotEmpty(ps);

        Assert.Equal(COTPParameterKinds.SrcTsap, ps[0].Kind);
        Assert.Equal(0x1000, (UInt16)ps[0].Value);

        Assert.Equal(COTPParameterKinds.DstTsap, ps[1].Kind);
        Assert.Equal(0x0300, (UInt16)ps[1].Value);

        Assert.Equal(COTPParameterKinds.TpduSize, ps[2].Kind);
        Assert.Equal(0x0A, (Byte)ps[2].Value);
    }

    Byte[] plcHead1_200smart = [3, 0, 0, 22, 17, 224, 0, 0, 0, 1, 0, 193, 2, 16, 0, 194, 2, 3, 0, 192, 1, 10];
    Byte[] plcHead2_200smart = [3, 0, 0, 25, 2, 240, 128, 50, 1, 0, 0, 204, 193, 0, 8, 0, 0, 240, 0, 0, 1, 0, 1, 3, 192];
    [Fact]
    public void Decode_200smart_CR()
    {
        var pk = new Packet(plcHead1_200smart);

        // 前面有TPKT头
        var tpkt = new TPKT();
        tpkt.Read(pk);
        Assert.Equal(3, tpkt.Version);
        Assert.Equal(0, tpkt.Reserved);
        Assert.Equal(0x16, tpkt.Length);
        Assert.Equal(pk.Slice(4).ToHex(), tpkt.Data.ToHex());

        var cotp = new COTP();
        var rs = cotp.Read(tpkt.Data);
        Assert.True(rs);
        Assert.Equal(PduType.ConnectionRequest, cotp.Type);
        Assert.Equal(0x0000, cotp.Destination);
        Assert.Equal(0x0001, cotp.Source);
        Assert.Equal(0x00, cotp.Option);

        var ps = cotp.Parameters;
        Assert.NotEmpty(ps);

        Assert.Equal(COTPParameterKinds.SrcTsap, ps[0].Kind);
        Assert.Equal(0x1000, (UInt16)ps[0].Value);

        Assert.Equal(COTPParameterKinds.DstTsap, ps[1].Kind);
        Assert.Equal(0x0300, (UInt16)ps[1].Value);

        Assert.Equal(COTPParameterKinds.TpduSize, ps[2].Kind);
        Assert.Equal(0x0A, (Byte)ps[2].Value);
    }

    [Fact]
    public void Decode_200smart_Data()
    {
        var pk = new Packet(plcHead2_200smart);

        // 前面有TPKT头
        var tpkt = new TPKT();
        tpkt.Read(pk);
        Assert.Equal(3, tpkt.Version);
        Assert.Equal(0, tpkt.Reserved);
        Assert.Equal(0x19, tpkt.Length);
        Assert.Equal(pk.Slice(4).ToHex(), tpkt.Data.ToHex());

        var cotp = new COTP();
        var rs = cotp.Read(tpkt.Data);
        Assert.True(rs);
        Assert.Equal(PduType.Data, cotp.Type);
        Assert.True(cotp.LastDataUnit);

        Assert.NotNull(cotp.Data);
        Assert.Equal(18, cotp.Data.Total);

        var msg = new S7Message();
        var rs2 = msg.Read(cotp.Data);
        Assert.True(rs2);

        Assert.Equal(0x32, msg.ProtocolId);
        Assert.Equal(S7Kinds.Job, msg.Kind);
        Assert.Equal(0x0000, msg.Reserved);
        Assert.Equal(0xCCC1, msg.Sequence);

        Assert.Single(msg.Parameters);
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
        Assert.Equal(pk.Slice(4).ToHex(), tpkt.Data.ToHex());

        var cotp = new COTP();
        var rs = cotp.Read(tpkt.Data);
        Assert.True(rs);
        Assert.Equal(PduType.ConnectionConfirmed, cotp.Type);
        Assert.Equal(0x0001, cotp.Destination);
        Assert.Equal(0x0007, cotp.Source);
        Assert.Equal(0x00, cotp.Option);

        var ps = cotp.Parameters;
        Assert.NotEmpty(ps);

        Assert.Equal(COTPParameterKinds.TpduSize, ps[0].Kind);
        Assert.Equal(0x0A, (Byte)ps[0].Value);

        Assert.Equal(COTPParameterKinds.SrcTsap, ps[1].Kind);
        Assert.Equal(0x1000, (UInt16)ps[1].Value);

        Assert.Equal(COTPParameterKinds.DstTsap, ps[2].Kind);
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
        Assert.Equal(pk.Slice(4).ToHex(), tpkt.Data.ToHex());

        var cotp = new COTP();
        var rs = cotp.Read(tpkt.Data);
        Assert.True(rs);
        Assert.Equal(PduType.Data, cotp.Type);
        Assert.True(cotp.LastDataUnit);

        Assert.NotNull(cotp.Data);
        Assert.Equal(18, cotp.Data.Total);
    }

    [Fact]
    public void ConnectTest()
    {
        var cotp = new COTP
        {
            Type = PduType.ConnectionRequest
        };

        var pk = cotp.ToPacket(false);

        var cotp2 = new COTP();
        var rs = cotp2.Read(pk);
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

        var pk = cotp.ToPacket(false);

        var cotp2 = new COTP();
        var rs = cotp2.Read(pk);
        Assert.True(rs);
        Assert.Equal(cotp.Type, cotp2.Type);
    }

    #region GetParameter / SetParameter
    [Fact]
    public void GetParameter_ReturnsNull_WhenNotFound()
    {
        var cotp = new COTP { Type = PduType.ConnectionRequest };
        var result = cotp.GetParameter(COTPParameterKinds.TpduSize);
        Assert.Null(result);
    }

    [Fact]
    public void GetParameter_ReturnsExisting()
    {
        var cotp = new COTP { Type = PduType.ConnectionRequest };
        cotp.SetParameter(COTPParameterKinds.TpduSize, (Byte)0x0A);

        var result = cotp.GetParameter(COTPParameterKinds.TpduSize);
        Assert.NotNull(result);
        Assert.Equal(COTPParameterKinds.TpduSize, result.Kind);
        Assert.Equal((Byte)0x0A, result.Value);
    }

    [Fact]
    public void SetParameter_AddNew()
    {
        var cotp = new COTP { Type = PduType.ConnectionRequest };
        Assert.Empty(cotp.Parameters);

        cotp.SetParameter(new COTPParameter(COTPParameterKinds.TpduSize, 1, (Byte)0x0A));
        Assert.Single(cotp.Parameters);
        Assert.Equal(COTPParameterKinds.TpduSize, cotp.Parameters[0].Kind);
    }

    [Fact]
    public void SetParameter_ReplaceExisting()
    {
        var cotp = new COTP { Type = PduType.ConnectionRequest };
        cotp.SetParameter(new COTPParameter(COTPParameterKinds.TpduSize, 1, (Byte)0x0A));
        cotp.SetParameter(new COTPParameter(COTPParameterKinds.TpduSize, 1, (Byte)0x0B));

        Assert.Single(cotp.Parameters);
        Assert.Equal((Byte)0x0B, cotp.Parameters[0].Value);
    }

    [Fact]
    public void SetParameter_Byte()
    {
        var cotp = new COTP { Type = PduType.ConnectionRequest };
        cotp.SetParameter(COTPParameterKinds.TpduSize, (Byte)0x0A);

        var p = cotp.GetParameter(COTPParameterKinds.TpduSize);
        Assert.NotNull(p);
        Assert.Equal(1, p.Length);
        Assert.Equal((Byte)0x0A, p.Value);
    }

    [Fact]
    public void SetParameter_UInt16()
    {
        var cotp = new COTP { Type = PduType.ConnectionRequest };
        cotp.SetParameter(COTPParameterKinds.SrcTsap, (UInt16)0x1000);

        var p = cotp.GetParameter(COTPParameterKinds.SrcTsap);
        Assert.NotNull(p);
        Assert.Equal(2, p.Length);
        Assert.Equal((UInt16)0x1000, p.Value);
    }

    [Fact]
    public void SetParameter_UInt32()
    {
        var cotp = new COTP { Type = PduType.ConnectionRequest };
        cotp.SetParameter(COTPParameterKinds.DstTsap, (UInt32)0x12345678);

        var p = cotp.GetParameter(COTPParameterKinds.DstTsap);
        Assert.NotNull(p);
        Assert.Equal(4, p.Length);
        Assert.Equal((UInt32)0x12345678, p.Value);
    }
    #endregion

    #region ToString
    [Fact]
    public void ToString_Data()
    {
        var cotp = new COTP
        {
            Type = PduType.Data,
            Data = new Packet(new Byte[] { 1, 2, 3 })
        };
        var str = cotp.ToString();
        Assert.Contains("Data", str);
        Assert.Contains("3", str);
    }

    [Fact]
    public void ToString_CR()
    {
        var cotp = new COTP
        {
            Type = PduType.ConnectionRequest
        };
        var str = cotp.ToString();
        Assert.Contains("ConnectionRequest", str);
    }
    #endregion

    #region CR with parameters roundtrip
    [Fact]
    public void CR_WithParameters_Roundtrip()
    {
        var cotp = new COTP
        {
            Type = PduType.ConnectionRequest,
            Destination = 0x0000,
            Source = 0x0001,
            Option = 0x00
        };
        cotp.SetParameter(COTPParameterKinds.SrcTsap, (UInt16)0x1000);
        cotp.SetParameter(COTPParameterKinds.DstTsap, (UInt16)0x0300);
        cotp.SetParameter(COTPParameterKinds.TpduSize, (Byte)0x0A);

        var pk = cotp.ToPacket(false);

        var cotp2 = new COTP();
        var rs = cotp2.Read(pk);
        Assert.True(rs);

        Assert.Equal(PduType.ConnectionRequest, cotp2.Type);
        Assert.Equal(0x0000, cotp2.Destination);
        Assert.Equal(0x0001, cotp2.Source);
        Assert.Equal(0x00, cotp2.Option);
        Assert.Equal(3, cotp2.Parameters.Count);

        var src = cotp2.GetParameter(COTPParameterKinds.SrcTsap);
        Assert.NotNull(src);
        Assert.Equal((UInt16)0x1000, src.Value);

        var dst = cotp2.GetParameter(COTPParameterKinds.DstTsap);
        Assert.NotNull(dst);
        Assert.Equal((UInt16)0x0300, dst.Value);

        var tpdu = cotp2.GetParameter(COTPParameterKinds.TpduSize);
        Assert.NotNull(tpdu);
        Assert.Equal((Byte)0x0A, tpdu.Value);
    }
    #endregion

    #region COTPParameter constructor
    [Fact]
    public void COTPParameter_ConstructorSetsFields()
    {
        var p = new COTPParameter(COTPParameterKinds.TpduSize, 1, (Byte)10);
        Assert.Equal(COTPParameterKinds.TpduSize, p.Kind);
        Assert.Equal(1, p.Length);
        Assert.Equal((Byte)10, p.Value);
    }

    [Fact]
    public void COTPParameter_MutableProperties()
    {
        var p = new COTPParameter(COTPParameterKinds.SrcTsap, 2, (UInt16)0x1000);
        p.Kind = COTPParameterKinds.DstTsap;
        p.Length = 4;
        p.Value = (UInt32)0x12345678;

        Assert.Equal(COTPParameterKinds.DstTsap, p.Kind);
        Assert.Equal(4, p.Length);
        Assert.Equal((UInt32)0x12345678, p.Value);
    }
    #endregion

    #region DT with data roundtrip
    [Fact]
    public void DT_WithData_Roundtrip()
    {
        var payload = new Byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var cotp = new COTP
        {
            Type = PduType.Data,
            LastDataUnit = true,
            Number = 0,
            Data = new Packet(payload)
        };

        var pk = cotp.ToPacket(false);
        var cotp2 = new COTP();
        var rs = cotp2.Read(pk);

        Assert.True(rs);
        Assert.Equal(PduType.Data, cotp2.Type);
        Assert.True(cotp2.LastDataUnit);
        Assert.NotNull(cotp2.Data);
        Assert.Equal(payload.ToHex(), cotp2.Data.ToHex());
    }

    [Fact]
    public void DT_WithTPKT_Roundtrip()
    {
        var payload = new Byte[] { 0x01, 0x02, 0x03 };
        var cotp = new COTP
        {
            Type = PduType.Data,
            LastDataUnit = true,
            Data = new Packet(payload)
        };

        var pk = cotp.ToPacket(true);
        var bytes = pk.ToArray();

        // Parse TPKT first
        var tpkt = new TPKT();
        tpkt.Read(new Packet(bytes));
        Assert.Equal(3, tpkt.Version);

        // Then COTP
        var cotp2 = new COTP();
        cotp2.Read(tpkt.Data);
        Assert.Equal(PduType.Data, cotp2.Type);
        Assert.True(cotp2.LastDataUnit);
        Assert.Equal(payload.ToHex(), cotp2.Data.ToHex());
    }
    #endregion
}
