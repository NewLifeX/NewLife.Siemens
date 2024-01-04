using NewLife.Data;
using NewLife.Serialization;

namespace NewLife.Siemens.Protocols;

/// <summary>S7参数类型</summary>
public enum S7Functions : Byte
{
    /// <summary>设置通信</summary>
    Setup = 0xF0,
}

/// <summary>数据项（类型+长度+数值）</summary>
public class S7Parameter : IAccessor
{
    #region 属性
    /// <summary>功能码</summary>
    public S7Functions Code { get; set; }
    #endregion

    #region 方法
    /// <summary>读取</summary>
    /// <param name="stream"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public Boolean Read(Stream stream, Object context)
    {
        var reader = context as Binary;
        reader ??= new Binary { Stream = stream ?? (context as Packet)?.GetStream(), IsLittleEndian = false };

        Code = (S7Functions)reader.ReadByte();

        OnRead(reader);

        return true;
    }

    /// <summary>读取</summary>
    /// <param name="reader"></param>
    protected virtual void OnRead(Binary reader) { }

    /// <summary>写入</summary>
    /// <param name="stream"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public Boolean Write(Stream stream, Object context)
    {
        var writer = context as Binary;
        writer ??= new Binary { Stream = stream ?? (context as Packet)?.GetStream(), IsLittleEndian = false };

        writer.WriteByte((Byte)Code);

        OnWrite(writer);

        return true;
    }

    /// <summary>写入</summary>
    /// <param name="writer"></param>
    protected virtual void OnWrite(Binary writer) { }
    #endregion
}

/// <summary>设置通信</summary>
/// <remarks>各个字段都是大端</remarks>
public class S7SetupParameter : S7Parameter
{
    #region 属性
    /// <summary>Ack队列的大小（主叫）</summary>
    public Byte MaxAmqCaller { get; set; }

    /// <summary>Ack队列的大小（被叫）</summary>
    public Byte MaxAmqCallee { get; set; }

    /// <summary>PDU长度</summary>
    public Byte PduLength { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public S7SetupParameter() => Code = S7Functions.Setup;
    #endregion
}