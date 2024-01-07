using NewLife.Data;
using NewLife.Serialization;

namespace NewLife.Siemens.Protocols;

/// <summary>S7参数类型</summary>
public enum S7Functions : Byte
{
    /// <summary>设置通信</summary>
    Setup = 0xF0,

    /// <summary>读取变量</summary>
    ReadVar = 0x04,

    /// <summary>写入变量</summary>
    WriteVar = 0x05,
}

/// <summary>数据项（类型+长度+数值）</summary>
public class S7Parameter : IAccessor
{
    #region 属性
    /// <summary>功能码</summary>
    public S7Functions Code { get; set; }
    #endregion

    #region 方法
    public Boolean Read(Packet pk) => Read(pk.GetStream(), pk);

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
        stream = writer.Stream;

        writer.WriteByte((Byte)Code);

        var ms = new MemoryStream();
        writer.Stream = ms;

        OnWrite(writer);

        writer.Stream = stream;

        writer.WriteByte((Byte)ms.Length);

        ms.Position = 0;
        ms.CopyTo(writer.Stream);

        return true;
    }

    /// <summary>写入</summary>
    /// <param name="writer"></param>
    protected virtual void OnWrite(Binary writer) { }
    #endregion
}