using NewLife.Serialization;

namespace NewLife.Siemens.Messages;

/// <summary>数据项</summary>
public class DataItem
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
    public Byte Area { get; set; }

    /// <summary>起始地址</summary>
    public UInt32 Address { get; set; }
    #endregion

    #region 方法
    public void Read(Binary reader)
    {
        Id = reader.ReadByte();

        var len = reader.ReadByte();

        SyntaxId = reader.ReadByte();
        TransportSize = reader.ReadByte();
        Length = reader.ReadUInt16();
        DbNumber = reader.ReadUInt16();
        Area = reader.ReadByte();
        Address = reader.ReadBytes(3).ToUInt32();
    }

    public void Writer(Binary writer)
    {
        writer.WriteByte(Id);

        var len = 1 + 1 + 2 + 2 + 1 + 3;
        writer.WriteByte((Byte)len);
        writer.Write(SyntaxId);
        writer.Write(TransportSize);
        writer.Write(Length);
        writer.Write(DbNumber);
        writer.Write(Area);
        writer.Write(Address.GetBytes(false).Take(3).ToArray());
    }
    #endregion
}
