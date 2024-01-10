using NewLife.Data;
using NewLife.Serialization;

namespace NewLife.Siemens.Messages;

/// <summary>数据项（类型+长度+数值）</summary>
public class S7Parameter
{
    #region 属性
    /// <summary>功能码</summary>
    public S7Functions Code { get; set; }
    #endregion

    #region 构造
    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => $"[{Code}]";
    #endregion

    #region 方法
    /// <summary>读取</summary>
    /// <param name="pk"></param>
    /// <returns></returns>
    public Boolean Read(Packet pk) => Read(new Binary { Stream = pk.GetStream(), IsLittleEndian = false });

    /// <summary>读取</summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    public Boolean Read(Binary reader)
    {
        Code = (S7Functions)reader.ReadByte();

        OnRead(reader);

        return true;
    }

    /// <summary>读取</summary>
    /// <param name="reader"></param>
    protected virtual void OnRead(Binary reader) { }

    /// <summary>写入</summary>
    /// <param name="writer"></param>
    /// <returns></returns>
    public Boolean Write(Binary writer)
    {
        writer.WriteByte((Byte)Code);

        OnWrite(writer);

        return true;
    }

    /// <summary>写入</summary>
    /// <param name="writer"></param>
    protected virtual void OnWrite(Binary writer) { }
    #endregion
}