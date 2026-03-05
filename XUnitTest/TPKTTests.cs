using System;
using NewLife;
using NewLife.Data;
using NewLife.Siemens.Protocols;
using Xunit;

namespace XUnitTest;

public class TPKTTests
{
    [Fact]
    public void DefaultVersion()
    {
        var tpkt = new TPKT();
        Assert.Equal(3, tpkt.Version);
        Assert.Equal(0, tpkt.Reserved);
    }

    [Fact]
    public void ReadHeader_WritesHeader_Roundtrip()
    {
        var buf = new Byte[4];
        var tpkt = new TPKT
        {
            Version = 3,
            Reserved = 0,
            Length = 100
        };
        tpkt.WriteHeader(buf);

        Assert.Equal(3, buf[0]);
        Assert.Equal(0, buf[1]);
        Assert.Equal(0, buf[2]); // 100 >> 8
        Assert.Equal(100, buf[3]); // 100 & 0xFF

        var tpkt2 = new TPKT();
        tpkt2.ReadHeader(buf);
        Assert.Equal(3, tpkt2.Version);
        Assert.Equal(0, tpkt2.Reserved);
        Assert.Equal(100, tpkt2.Length);
    }

    [Fact]
    public void ReadHeader_LargeLength()
    {
        var buf = new Byte[4];
        var tpkt = new TPKT
        {
            Length = 0x0116  // 278
        };
        tpkt.WriteHeader(buf);

        Assert.Equal(0x01, buf[2]);
        Assert.Equal(0x16, buf[3]);

        var tpkt2 = new TPKT();
        tpkt2.ReadHeader(buf);
        Assert.Equal(0x0116, tpkt2.Length);
    }

    [Fact]
    public void Read_FromPacket()
    {
        // Build a TPKT with 2 bytes of data
        var raw = new Byte[] { 3, 0, 0, 6, 0xAA, 0xBB };
        var pk = new Packet(raw);

        var tpkt = new TPKT();
        tpkt.Read(pk);

        Assert.Equal(3, tpkt.Version);
        Assert.Equal(0, tpkt.Reserved);
        Assert.Equal(6, tpkt.Length);
        Assert.NotNull(tpkt.Data);
        Assert.Equal(2, tpkt.Data.Total);

        var data = tpkt.Data.ReadBytes(0, 2);
        Assert.Equal(0xAA, data[0]);
        Assert.Equal(0xBB, data[1]);
    }

    [Fact]
    public void Read_EmptyData()
    {
        // A TPKT with only header (Length = 4, no payload)
        var raw = new Byte[] { 3, 0, 0, 4 };
        var pk = new Packet(raw);

        var tpkt = new TPKT();
        tpkt.Read(pk);

        Assert.Equal(3, tpkt.Version);
        Assert.Equal(4, tpkt.Length);
        Assert.Null(tpkt.Data);
    }

    [Fact]
    public void ToPacket_Serialization()
    {
        var payload = new Byte[] { 0x01, 0x02, 0x03 };
        var tpkt = new TPKT
        {
            Data = new Packet(payload)
        };

        var pk = tpkt.ToPacket();
        Assert.NotNull(pk);

        // Total should be header(4) + payload(3) = 7
        Assert.Equal(7, pk.Total);

        var bytes = pk.ToArray();
        Assert.Equal(3, bytes[0]);     // Version
        Assert.Equal(0, bytes[1]);     // Reserved
        Assert.Equal(0, bytes[2]);     // Length high
        Assert.Equal(7, bytes[3]);     // Length low
        Assert.Equal(0x01, bytes[4]);
        Assert.Equal(0x02, bytes[5]);
        Assert.Equal(0x03, bytes[6]);
    }

    [Fact]
    public void ToPacket_NullData()
    {
        var tpkt = new TPKT();
        var pk = tpkt.ToPacket();

        var bytes = pk.ToArray();
        Assert.Equal(4, bytes.Length);
        Assert.Equal(3, bytes[0]);
    }

    [Fact]
    public void Roundtrip_WriteRead()
    {
        var payload = new Byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var tpkt = new TPKT { Data = new Packet(payload) };

        var pk = tpkt.ToPacket();
        var bytes = pk.ToArray();

        var tpkt2 = new TPKT();
        tpkt2.Read(new Packet(bytes));

        Assert.Equal(3, tpkt2.Version);
        Assert.Equal(8, tpkt2.Length);
        Assert.NotNull(tpkt2.Data);
        Assert.Equal(4, tpkt2.Data.Total);
        Assert.Equal(payload.ToHex(), tpkt2.Data.ToHex());
    }
}
