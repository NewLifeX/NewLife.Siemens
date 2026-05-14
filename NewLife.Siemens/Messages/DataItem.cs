using NewLife.Serialization;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;

namespace NewLife.Siemens.Messages;

/// <summary>数据项</summary>
public class DataItem
{
    #region 协议属性（用于 S7 报文编解码）
    /// <summary>错误码。0xFF表示成功，写入请求时置零</summary>
    public ReadWriteErrorCode Code { get; set; }

    /// <summary>传输数据类型。按字为04，按位为03</summary>
    public Byte TransportSize { get; set; }

    /// <summary>原始字节数据</summary>
    public Byte[]? Data { get; set; }
    #endregion

    #region 批量读写元数据（供 ReadMultipleVars / WriteMultipleVars 使用，不参与协议序列化）
    /// <summary>存储区域（如 DataBlock / Memory / Input / Output）</summary>
    public DataType DataType { get; set; }

    /// <summary>变量类型（决定字节大小与转换方式）</summary>
    public VarType VarType { get; set; }

    /// <summary>数据块编号（非 DataBlock 区域时为 0）</summary>
    public Int32 DbNumber { get; set; }

    /// <summary>起始字节地址</summary>
    public Int32 StartByteAdr { get; set; }

    /// <summary>位地址（仅 VarType=Bit 时使用，0-7）</summary>
    public Int32 BitAdr { get; set; } = -1;

    /// <summary>元素个数（默认为 1）</summary>
    public Int32 Count { get; set; } = 1;

    /// <summary>读取后的 .NET 值（Read 后填入；Write 前由调用方填入）</summary>
    public Object? Value { get; set; }
    #endregion

    #region 构造函数
    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => TransportSize > 0 ? $"{TransportSize}({Data.ToHex()})" : $"{Code}";

    /// <summary>根据批量元数据构建 DataItem</summary>
    /// <param name="dataType">存储区域</param>
    /// <param name="varType">变量类型</param>
    /// <param name="db">数据块编号</param>
    /// <param name="startByte">起始字节</param>
    /// <param name="bitAdr">位地址（非 Bit 类型时传 -1）</param>
    /// <param name="count">元素个数</param>
    public static DataItem Create(DataType dataType, VarType varType, Int32 db, Int32 startByte, Int32 bitAdr = -1, Int32 count = 1)
        => new()
        {
            DataType = dataType,
            VarType = varType,
            DbNumber = db,
            StartByteAdr = startByte,
            BitAdr = bitAdr,
            Count = count,
        };
    #endregion

    #region 方法
    /// <summary>读取</summary>
    /// <param name="reader"></param>
    public void Read(Binary reader)
    {
        if (reader.EndOfStream()) return;

        Code = (ReadWriteErrorCode)reader.ReadByte();

        // WriteResponse中只有Code
        if (reader.EndOfStream()) return;

        var b = reader.ReadByte();
        TransportSize = b;

        var len = reader.ReadUInt16();
        // BIT=0x03 / Byte/Word/DWord=0x04
        if (b == 0x04) len /= 8;

        Data = reader.ReadBytes(len);
    }

    /// <summary>写入</summary>
    /// <param name="writer"></param>
    public void Writer(Binary writer)
    {
        writer.WriteByte((Byte)Code);
        writer.WriteByte((Byte)TransportSize);

        var len = Data?.Length ?? 0;

        // BIT=0x03 / Byte/Word/DWord=0x04
        var b = (Byte)TransportSize;
        if (b == 0x04) len *= 8;

        writer.WriteUInt16((UInt16)len);

        if (Data != null) writer.Write(Data, 0, Data.Length);
    }
    #endregion
}
