using NewLife.Serialization;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;

namespace NewLife.Siemens.Messages;

/// <summary>数据项</summary>
public class DataItem
{
    #region 属性
    /// <summary>错误码。0xFF表示成功，写入请求时置零</summary>
    public ReadWriteErrorCode Code { get; set; }

    /// <summary>传输数据类型。按字为04，按位为03</summary>
    public Byte TransportSize { get; set; }

    /// <summary>数据</summary>
    public Byte[]? Data { get; set; }
    #endregion

    #region 构造函数
    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => TransportSize > 0 ? $"{TransportSize}({Data.ToHex()})" : $"{Code}";
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
