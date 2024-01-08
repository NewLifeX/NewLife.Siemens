using NewLife.Serialization;

namespace NewLife.Siemens.Messages;

/// <summary>数据项</summary>
public class DataItem
{
    #region 属性
    /// <summary>错误码。0xFF表示成功</summary>
    public Byte Code { get; set; }

    /// <summary>传输大小。BIT</summary>
    public Byte TransportSize { get; set; }

    /// <summary>数据</summary>
    public Byte[] Data { get; set; }
    #endregion

    #region 方法
    /// <summary>读取</summary>
    /// <param name="reader"></param>
    public void Read(Binary reader)
    {
        Code = reader.ReadByte();
        TransportSize = reader.ReadByte();

        var len = reader.ReadUInt16();
        Data = reader.ReadBytes(len);
    }

    /// <summary>写入</summary>
    /// <param name="writer"></param>
    public void Writer(Binary writer)
    {
        writer.WriteByte(Code);
        writer.WriteByte(TransportSize);

        var len = Data?.Length ?? 0;
        writer.WriteUInt16((UInt16)len);

        if (Data != null) writer.Write(Data, 0, Data.Length);
    }
    #endregion
}
