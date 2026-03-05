using System;
using System.Collections.Generic;
using System.IO;
using NewLife;
using NewLife.Data;
using NewLife.Serialization;
using NewLife.Siemens.Messages;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using Xunit;

namespace XUnitTest;

public class MessageTests
{
    #region S7Parameter base class
    [Fact]
    public void S7Parameter_ReadWrite_Roundtrip()
    {
        var pm = new S7Parameter { Code = S7Functions.Setup };
        var writer = new Binary { IsLittleEndian = false };
        pm.Write(writer);

        var bytes = writer.GetBytes();
        Assert.Single(bytes);
        Assert.Equal((Byte)S7Functions.Setup, bytes[0]);

        var pm2 = new S7Parameter();
        pm2.Read(new Binary { Stream = new MemoryStream(bytes), IsLittleEndian = false });
        Assert.Equal(S7Functions.Setup, pm2.Code);
    }

    [Fact]
    public void S7Parameter_ToString()
    {
        var pm = new S7Parameter { Code = S7Functions.ReadVar };
        Assert.Contains("ReadVar", pm.ToString());
    }
    #endregion

    #region SetupMessage
    [Fact]
    public void SetupMessage_DefaultCode()
    {
        var setup = new SetupMessage();
        Assert.Equal(S7Functions.Setup, setup.Code);
    }

    [Fact]
    public void SetupMessage_Roundtrip()
    {
        var setup = new SetupMessage
        {
            MaxAmqCaller = 8,
            MaxAmqCallee = 8,
            PduLength = 480
        };

        var writer = new Binary { IsLittleEndian = false };
        setup.Write(writer);
        var bytes = writer.GetBytes();

        var setup2 = new SetupMessage();
        setup2.Read(new Binary { Stream = new MemoryStream(bytes), IsLittleEndian = false });

        Assert.Equal(S7Functions.Setup, setup2.Code);
        Assert.Equal(8, setup2.MaxAmqCaller);
        Assert.Equal(8, setup2.MaxAmqCallee);
        Assert.Equal(480, setup2.PduLength);
    }

    [Fact]
    public void SetupMessage_SerializedBytes()
    {
        var setup = new SetupMessage
        {
            MaxAmqCaller = 3,
            MaxAmqCallee = 3,
            PduLength = 960
        };

        var writer = new Binary { IsLittleEndian = false };
        setup.Write(writer);
        var bytes = writer.GetBytes();

        // Code(F0) + Reserved(00) + Caller(0003) + Callee(0003) + PduLen(03C0) = 8 bytes
        Assert.Equal(8, bytes.Length);
        Assert.Equal(0xF0, bytes[0]);
        Assert.Equal(0x00, bytes[1]);
        Assert.Equal(0x00, bytes[2]);
        Assert.Equal(0x03, bytes[3]);
        Assert.Equal(0x00, bytes[4]);
        Assert.Equal(0x03, bytes[5]);
        Assert.Equal(0x03, bytes[6]);
        Assert.Equal(0xC0, bytes[7]);
    }
    #endregion

    #region RequestItem
    [Fact]
    public void RequestItem_Roundtrip()
    {
        var item = new RequestItem
        {
            SpecType = 0x12,
            SyntaxId = 0x10,
            TransportSize = 0x02,
            Count = 1,
            DbNumber = 1,
            Area = DataType.DataBlock,
            Address = 0x50
        };

        var writer = new Binary { IsLittleEndian = false };
        item.Writer(writer);
        var bytes = writer.GetBytes();

        var item2 = new RequestItem();
        item2.Read(new Binary { Stream = new MemoryStream(bytes), IsLittleEndian = false });

        Assert.Equal(0x12, item2.SpecType);
        Assert.Equal(0x10, item2.SyntaxId);
        Assert.Equal(0x02, item2.TransportSize);
        Assert.Equal(1, item2.Count);
        Assert.Equal(1, item2.DbNumber);
        Assert.Equal(DataType.DataBlock, item2.Area);
        Assert.Equal(0x50u, item2.Address);
    }

    [Fact]
    public void RequestItem_ToString()
    {
        var item = new RequestItem
        {
            TransportSize = 2,
            Area = DataType.DataBlock,
            DbNumber = 1,
            Address = 80,
            Count = 10
        };
        var str = item.ToString();
        Assert.Contains("BYTE", str);
        Assert.Contains("DataBlock", str);
    }

    [Fact]
    public void RequestItem_ToString_BitMode()
    {
        var item = new RequestItem
        {
            TransportSize = 1,
            Area = DataType.Input,
            DbNumber = 0,
            Address = 0,
            Count = 1
        };
        var str = item.ToString();
        Assert.Contains("BIT", str);
    }
    #endregion

    #region DataItem
    [Fact]
    public void DataItem_Roundtrip_BitTransport()
    {
        var di = new DataItem
        {
            Code = ReadWriteErrorCode.Success,
            TransportSize = 0x03,
            Data = new Byte[] { 0x01 }
        };

        var writer = new Binary { IsLittleEndian = false };
        di.Writer(writer);
        var bytes = writer.GetBytes();

        var di2 = new DataItem();
        di2.Read(new Binary { Stream = new MemoryStream(bytes), IsLittleEndian = false });

        Assert.Equal(ReadWriteErrorCode.Success, di2.Code);
        Assert.Equal(0x03, di2.TransportSize);
        Assert.Single(di2.Data);
        Assert.Equal(0x01, di2.Data[0]);
    }

    [Fact]
    public void DataItem_Roundtrip_ByteTransport()
    {
        var di = new DataItem
        {
            Code = ReadWriteErrorCode.Success,
            TransportSize = 0x04,
            Data = new Byte[] { 0xAA, 0xBB }
        };

        var writer = new Binary { IsLittleEndian = false };
        di.Writer(writer);
        var bytes = writer.GetBytes();

        var di2 = new DataItem();
        di2.Read(new Binary { Stream = new MemoryStream(bytes), IsLittleEndian = false });

        Assert.Equal(ReadWriteErrorCode.Success, di2.Code);
        Assert.Equal(0x04, di2.TransportSize);
        Assert.Equal(2, di2.Data.Length);
        Assert.Equal(0xAA, di2.Data[0]);
        Assert.Equal(0xBB, di2.Data[1]);
    }

    [Fact]
    public void DataItem_WriteResponse_OnlyCode()
    {
        // WriteResponse only writes Code byte
        var di = new DataItem
        {
            Code = ReadWriteErrorCode.Success,
            TransportSize = 0,
            Data = null
        };

        var writer = new Binary { IsLittleEndian = false };
        // Simulate writing just the Code byte as WriteResponse does
        writer.WriteByte((Byte)di.Code);
        var bytes = writer.GetBytes();

        // Reading back - when stream ends after Code, it should just set Code
        var di2 = new DataItem();
        di2.Read(new Binary { Stream = new MemoryStream(bytes), IsLittleEndian = false });

        Assert.Equal(ReadWriteErrorCode.Success, di2.Code);
    }

    [Fact]
    public void DataItem_ToString_WithTransportSize()
    {
        var di = new DataItem
        {
            TransportSize = 0x04,
            Data = new Byte[] { 0x01, 0x02 }
        };
        var str = di.ToString();
        Assert.Contains("4", str);
    }

    [Fact]
    public void DataItem_ToString_WithoutTransportSize()
    {
        var di = new DataItem
        {
            Code = ReadWriteErrorCode.Success,
            TransportSize = 0
        };
        var str = di.ToString();
        Assert.Contains("Success", str);
    }
    #endregion

    #region ReadRequest
    [Fact]
    public void ReadRequest_DefaultCode()
    {
        var rr = new ReadRequest();
        Assert.Equal(S7Functions.ReadVar, rr.Code);
    }

    [Fact]
    public void ReadRequest_MultipleItems_Roundtrip()
    {
        var rr = new ReadRequest();
        rr.Items = new List<RequestItem>
        {
            new RequestItem
            {
                SpecType = 0x12,
                SyntaxId = 0x10,
                TransportSize = 0x02,
                Count = 1,
                DbNumber = 1,
                Area = DataType.DataBlock,
                Address = 0x00
            },
            new RequestItem
            {
                SpecType = 0x12,
                SyntaxId = 0x10,
                TransportSize = 0x02,
                Count = 2,
                DbNumber = 2,
                Area = DataType.DataBlock,
                Address = 0x10
            }
        };

        var writer = new Binary { IsLittleEndian = false };
        rr.Write(writer);
        var bytes = writer.GetBytes();

        var rr2 = new ReadRequest();
        rr2.Read(new Binary { Stream = new MemoryStream(bytes), IsLittleEndian = false });

        Assert.Equal(S7Functions.ReadVar, rr2.Code);
        Assert.Equal(2, rr2.Items.Count);
        Assert.Equal(1, rr2.Items[0].DbNumber);
        Assert.Equal(2, rr2.Items[1].DbNumber);
        Assert.Equal(0x00u, rr2.Items[0].Address);
        Assert.Equal(0x10u, rr2.Items[1].Address);
    }
    #endregion

    #region ReadResponse
    [Fact]
    public void ReadResponse_DefaultCode()
    {
        var rr = new ReadResponse();
        Assert.Equal(S7Functions.ReadVar, rr.Code);
    }
    #endregion

    #region WriteRequest
    [Fact]
    public void WriteRequest_DefaultCode()
    {
        var wr = new WriteRequest();
        Assert.Equal(S7Functions.WriteVar, wr.Code);
    }
    #endregion

    #region WriteResponse
    [Fact]
    public void WriteResponse_DefaultCode()
    {
        var wr = new WriteResponse();
        Assert.Equal(S7Functions.WriteVar, wr.Code);
    }
    #endregion

    #region COTPParameter
    [Fact]
    public void COTPParameter_Constructor()
    {
        var p = new COTPParameter(COTPParameterKinds.TpduSize, 1, (Byte)0x0A);
        Assert.Equal(COTPParameterKinds.TpduSize, p.Kind);
        Assert.Equal(1, p.Length);
        Assert.Equal((Byte)0x0A, p.Value);
    }

    [Fact]
    public void COTPParameter_NullValue()
    {
        var p = new COTPParameter(COTPParameterKinds.SrcTsap, 2, null);
        Assert.Equal(COTPParameterKinds.SrcTsap, p.Kind);
        Assert.Null(p.Value);
    }
    #endregion
}
