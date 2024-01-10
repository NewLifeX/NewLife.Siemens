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

    /// <summary>变量类型</summary>
    public VarType Type { get; set; }

    /// <summary>数据</summary>
    public Byte[]? Data { get; set; }
    #endregion

    #region 构造函数
    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => Type > 0 ? $"{Type}({Data.ToHex()})" : $"{Code}";
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

        Type = (VarType)reader.ReadByte();

        var len = reader.ReadUInt16();
        Data = reader.ReadBytes(len);
    }

    /// <summary>写入</summary>
    /// <param name="writer"></param>
    public void Writer(Binary writer)
    {
        writer.WriteByte((Byte)Code);
        writer.WriteByte((Byte)Type);

        var len = Data?.Length ?? 0;
        writer.WriteUInt16((UInt16)len);

        if (Data != null) writer.Write(Data, 0, Data.Length);
    }
    #endregion
}
