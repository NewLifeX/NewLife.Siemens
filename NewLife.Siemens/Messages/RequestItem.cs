using NewLife.Serialization;
using NewLife.Siemens.Models;

namespace NewLife.Siemens.Messages;

/// <summary>请求数据项</summary>
/// <remarks>共12字节</remarks>
public class RequestItem
{
    #region 属性
    /// <summary>结构类型。总是0x12</summary>
    public Byte SpecType { get; set; } = 0x12;

    /// <summary>寻址模式。任意类型S7ANY用0x10</summary>
    public Byte SyntaxId { get; set; }

    /// <summary>传输数据类型。按字为02，按位为01</summary>
    public Byte TransportSize { get; set; }

    /// <summary>个数</summary>
    public UInt16 Count { get; set; }

    /// <summary>数据库地址</summary>
    public UInt16 DbNumber { get; set; }

    /// <summary>存储区域</summary>
    public DataType Area { get; set; }

    /// <summary>起始地址</summary>
    public UInt32 Address { get; set; }
    #endregion

    #region 构造函数
    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => $"{(TransportSize > 1 ? "BYTE" : "BIT")}({Area}:{DbNumber}:{Address}, {Count})";
    #endregion

    #region 方法
    /// <summary>读取</summary>
    /// <param name="reader"></param>
    public void Read(Binary reader)
    {
        SpecType = reader.ReadByte();

        var len = reader.ReadByte();

        SyntaxId = reader.ReadByte();
        TransportSize = reader.ReadByte();
        Count = reader.ReadUInt16();
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
        writer.WriteByte(SpecType);

        var len = 1 + 1 + 2 + 2 + 1 + 3;
        writer.WriteByte((Byte)len);
        writer.WriteByte(SyntaxId);
        writer.WriteByte(TransportSize);
        writer.Write(Count);
        writer.Write(DbNumber);
        writer.WriteByte((Byte)Area);

        var buf = Address.GetBytes(false);
        writer.Write(buf, 1, 3);
    }
    #endregion
}
