using System;
using System.IO;
using NewLife;
using NewLife.Siemens.Messages;
using NewLife.Siemens.Protocols;
using Xunit;
using NewLife.Siemens.Models;

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

        var pm = msg.Parameters[0] as SetupMessage;
        Assert.NotNull(pm);
        Assert.Equal(S7Functions.Setup, pm.Code);
        Assert.Equal(3, pm.MaxAmqCaller);
        Assert.Equal(3, pm.MaxAmqCallee);
        Assert.Equal(0x03C0, pm.PduLength);
        Assert.Equal(960, pm.PduLength);

        //Assert.Null(msg.Data);

        // 序列化
        var buf = msg.GetBytes();
        Assert.Equal(hex.ToHex(), buf.ToHex());
    }

    [Fact]
    public void Test2()
    {
        var hex = new Byte[] { 50, 1, 0, 0, 204, 193, 0, 8, 0, 0, 240, 0, 0, 1, 0, 1, 3, 192 };

        var msg = new S7Message();

        var rs = msg.Read(new MemoryStream(hex), null);
        Assert.True(rs);

        Assert.Equal(0x32, msg.ProtocolId);
        Assert.Equal(S7Kinds.Job, msg.Kind);
        Assert.Equal(0x0000, msg.Reserved);
        Assert.Equal(0xCCC1, msg.Sequence);

        Assert.Single(msg.Parameters);

        var pm = msg.Parameters[0] as SetupMessage;
        Assert.NotNull(pm);
        Assert.Equal(S7Functions.Setup, pm.Code);
        Assert.Equal(1, pm.MaxAmqCaller);
        Assert.Equal(1, pm.MaxAmqCallee);
        Assert.Equal(0x03C0, pm.PduLength);
        Assert.Equal(960, pm.PduLength);

        var pm2 = msg.GetParameter(S7Functions.Setup);
        Assert.NotNull(pm2);
        Assert.Equal(pm, pm2);

        //Assert.Null(msg.Data);

        // 序列化
        var buf = msg.GetBytes();
        Assert.Equal(hex.ToHex(), buf.ToHex());
    }

    [Fact]
    public void ReadVar()
    {
        var str = "32 01 00 00 00 01" +
            "00 0e 00 00" +
            // 读取请求项
            "04 01 12 0a 10 01 00 01 00 01 84 00 00 50";
        var hex = str.ToHex();

        var msg = new S7Message();

        var rs = msg.Read(hex);
        Assert.True(rs);

        Assert.Equal(0x32, msg.ProtocolId);
        Assert.Equal(S7Kinds.Job, msg.Kind);
        Assert.Equal(0x0000, msg.Reserved);
        Assert.Equal(1, msg.Sequence);

        Assert.Single(msg.Parameters);

        var pm = msg.Parameters[0] as ReadRequest;
        Assert.NotNull(pm);
        Assert.Equal(S7Functions.ReadVar, pm.Code);
        Assert.Single(pm.Items);

        var pm2 = msg.GetParameter(S7Functions.ReadVar);
        Assert.NotNull(pm2);
        Assert.Equal(pm, pm2);

        var di = pm.Items[0];
        Assert.Equal(0x12, di.SpecType);
        Assert.Equal(0x10, di.SyntaxId);
        Assert.Equal(1, di.TransportSize);
        Assert.Equal(1, di.Count);
        Assert.Equal(1, di.DbNumber);
        Assert.Equal(DataType.DataBlock, di.Area);
        Assert.Equal(0x50u, di.Address);

        //Assert.Null(msg.Data);

        // 序列化
        var buf = msg.GetBytes();
        Assert.Equal(hex.ToHex(), buf.ToHex());
    }

    [Fact]
    public void ReadVarResponse()
    {
        var str = "32 03 00 00 00 01" +
            // plen + dlen
            "00 02 00 05 " +
            // error code
            "00 00 " +
            // 读取请求项
            "04 01 " +
            // 数据项
            "ff 03 00 01 00";
        var hex = str.ToHex();

        var msg = new S7Message();

        var rs = msg.Read(hex);
        Assert.True(rs);

        Assert.Equal(0x32, msg.ProtocolId);
        Assert.Equal(S7Kinds.AckData, msg.Kind);
        Assert.Equal(0x0000, msg.Reserved);
        Assert.Equal(1, msg.Sequence);

        Assert.Equal(0, msg.ErrorClass);
        Assert.Equal(0, msg.ErrorCode);

        Assert.Single(msg.Parameters);

        var pm = msg.Parameters[0] as ReadResponse;
        Assert.NotNull(pm);
        Assert.Equal(S7Functions.ReadVar, pm.Code);
        Assert.Single(pm.Items);

        var pm2 = msg.GetParameter(S7Functions.ReadVar);
        Assert.NotNull(pm2);
        Assert.Equal(pm, pm2);

        var di = pm.Items[0];
        Assert.Equal(ReadWriteErrorCode.Success, di.Code);
        Assert.Equal(0x03, di.TransportSize);
        Assert.Single(di.Data);
        Assert.Equal(0x00, di.Data[0]);

        //Assert.NotNull(msg.Data);

        // 序列化
        var buf = msg.GetBytes();
        Assert.Equal(hex.ToHex(), buf.ToHex());
    }

    [Fact]
    public void ReadVarResponse2()
    {
        var str = "32 03 00 00 00 01" +
            // plen + dlen
            "00 02 00 06 " +
            // error code
            "00 00 " +
            // 读取请求项
            "04 01 " +
            // 数据项
            "ff 04 00 10 00 00";
        var hex = str.ToHex();

        var msg = new S7Message();

        var rs = msg.Read(hex);
        Assert.True(rs);

        Assert.Equal(0x32, msg.ProtocolId);
        Assert.Equal(S7Kinds.AckData, msg.Kind);
        Assert.Equal(0x0000, msg.Reserved);
        Assert.Equal(1, msg.Sequence);

        Assert.Equal(0, msg.ErrorClass);
        Assert.Equal(0, msg.ErrorCode);

        Assert.Single(msg.Parameters);

        var pm = msg.Parameters[0] as ReadResponse;
        Assert.NotNull(pm);
        Assert.Equal(S7Functions.ReadVar, pm.Code);
        Assert.Single(pm.Items);

        var pm2 = msg.GetParameter(S7Functions.ReadVar);
        Assert.NotNull(pm2);
        Assert.Equal(pm, pm2);

        var di = pm.Items[0];
        Assert.Equal(ReadWriteErrorCode.Success, di.Code);
        Assert.Equal(0x04, di.TransportSize);
        Assert.Equal(2, di.Data.Length);
        Assert.Equal(0x00, di.Data[0]);

        //Assert.NotNull(msg.Data);

        // 序列化
        var buf = msg.GetBytes();
        Assert.Equal(hex.ToHex(), buf.ToHex());
    }

    [Fact]
    public void WriteVar()
    {
        var str = "32 01 00 00 00 01 " +
            // plen + dlen
            "00 0e 00 05 " +
            // 写入请求项
            "05 01 12 0a 10 01 00 01 00 01 84 00 00 50 " +
            // 数据项
            "00 03 00 01 01";
        var hex = str.ToHex();

        var msg = new S7Message();

        var rs = msg.Read(hex);
        Assert.True(rs);

        Assert.Equal(0x32, msg.ProtocolId);
        Assert.Equal(S7Kinds.Job, msg.Kind);
        Assert.Equal(0x0000, msg.Reserved);
        Assert.Equal(1, msg.Sequence);

        Assert.Single(msg.Parameters);

        var pm = msg.Parameters[0] as WriteRequest;
        Assert.NotNull(pm);
        Assert.Equal(S7Functions.WriteVar, pm.Code);
        Assert.Single(pm.Items);

        var pm2 = msg.GetParameter(S7Functions.WriteVar);
        Assert.NotNull(pm2);
        Assert.Equal(pm, pm2);

        var ri = pm.Items[0];
        Assert.Equal(0x12, ri.SpecType);
        Assert.Equal(0x10, ri.SyntaxId);
        Assert.Equal(1, ri.TransportSize);
        Assert.Equal(1, ri.Count);
        Assert.Equal(1, ri.DbNumber);
        Assert.Equal(DataType.DataBlock, ri.Area);
        Assert.Equal(0x50u, ri.Address);

        var di = pm.DataItems[0];
        Assert.Equal(ReadWriteErrorCode.Reserved, di.Code);
        Assert.Equal(0x03, di.TransportSize);
        Assert.Single(di.Data);
        Assert.Equal(1, di.Data[0]);

        //Assert.Null(msg.Data);

        // 序列化
        var buf = msg.GetBytes();
        Assert.Equal(hex.ToHex(), buf.ToHex());
    }

    [Fact]
    public void WriteVarResponse()
    {
        var str = "32 03 00 00 00 01" +
            "00 02 00 01 " +
            "00 00 " +
            "05 01 " +
            "ff";
        var hex = str.ToHex();

        var msg = new S7Message();

        var rs = msg.Read(hex);
        Assert.True(rs);

        Assert.Equal(0x32, msg.ProtocolId);
        Assert.Equal(S7Kinds.AckData, msg.Kind);
        Assert.Equal(0x0000, msg.Reserved);
        Assert.Equal(1, msg.Sequence);
        Assert.Equal(0, msg.ErrorClass);
        Assert.Equal(0, msg.ErrorCode);

        Assert.Single(msg.Parameters);

        var pm = msg.Parameters[0] as WriteResponse;
        Assert.NotNull(pm);
        Assert.Equal(S7Functions.WriteVar, pm.Code);
        Assert.Single(pm.Items);

        var pm2 = msg.GetParameter(S7Functions.WriteVar);
        Assert.NotNull(pm2);
        Assert.Equal(pm, pm2);

        var di = pm.Items[0];
        Assert.Equal(ReadWriteErrorCode.Success, di.Code);
        //Assert.Equal(VarType.Bit, di.Type);
        Assert.Null(di.Data);
        //Assert.Equal(0x00, di.Data[0]);

        //Assert.NotNull(msg.Data);

        // 序列化
        var buf = msg.GetBytes();
        Assert.Equal(hex.ToHex(), buf.ToHex());
    }
}
