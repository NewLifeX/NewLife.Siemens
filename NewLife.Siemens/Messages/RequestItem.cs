using NewLife.Serialization;
using NewLife.Siemens.Models;

namespace NewLife.Siemens.Messages;

/// <summary>请求数据项</summary>
/// <remarks>共12字节</remarks>
public class RequestItem
{
    #region 属性
    /// <summary>标识</summary>
    public Byte Id { get; set; }

    /// <summary>DB块号</summary>
    public Byte SyntaxId { get; set; }

    /// <summary>起始地址</summary>
    public Byte TransportSize { get; set; }

    /// <summary>长度</summary>
    public UInt16 Length { get; set; }

    /// <summary>DB块号</summary>
    public UInt16 DbNumber { get; set; }

    /// <summary>区块</summary>
    public DataType Area { get; set; }

    /// <summary>起始地址</summary>
    public UInt32 Address { get; set; }
    #endregion

    #region 方法
    /// <summary>读取</summary>
    /// <param name="reader"></param>
    public void Read(Binary reader)
    {
        Id = reader.ReadByte();

        var len = reader.ReadByte();

        SyntaxId = reader.ReadByte();
        TransportSize = reader.ReadByte();
        Length = reader.ReadUInt16();
        DbNumber = reader.ReadUInt16();
        Area = (DataType)reader.ReadByte();

        var buf = reader.ReadBytes(3);
        var buf2 = new Byte[4];
        buf.CopyTo(buf2, 1);
        Address = buf2.ToUInt32(0, false);
    }

    /// <summary>写入</summary>
    /// <param name="writer"></param>
    public void Writer(Binary writer)
    {
        writer.WriteByte(Id);

        var len = 1 + 1 + 2 + 2 + 1 + 3;
        writer.WriteByte((Byte)len);
        writer.WriteByte(SyntaxId);
        writer.WriteByte(TransportSize);
        writer.Write(Length);
        writer.Write(DbNumber);
        writer.WriteByte((Byte)Area);

        var buf = Address.GetBytes(false);
        writer.Write(buf, 1, 3);
    }
    #endregion
}
