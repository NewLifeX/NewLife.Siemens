using NewLife.Serialization;

namespace NewLife.Siemens.Messages;

/// <summary>设置通信</summary>
/// <remarks>各个字段都是大端</remarks>
public class SetupMessage : S7Parameter
{
    #region 属性
    /// <summary>Ack队列的大小（主叫）</summary>
    public UInt16 MaxAmqCaller { get; set; }

    /// <summary>Ack队列的大小（被叫）</summary>
    public UInt16 MaxAmqCallee { get; set; }

    /// <summary>PDU长度</summary>
    public UInt16 PduLength { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public SetupMessage() => Code = S7Functions.Setup;
    #endregion

    #region 方法
    /// <summary>读取</summary>
    /// <param name="reader"></param>
    protected override void OnRead(Binary reader)
    {
        // 读取保留字节
        _ = reader.ReadByte();

        MaxAmqCaller = reader.ReadUInt16();
        MaxAmqCallee = reader.ReadUInt16();
        PduLength = reader.ReadUInt16();
    }

    /// <summary>写入</summary>
    /// <param name="writer"></param>
    protected override void OnWrite(Binary writer)
    {
        writer.WriteByte(0);

        writer.WriteUInt16(MaxAmqCaller);
        writer.WriteUInt16(MaxAmqCallee);
        writer.WriteUInt16(PduLength);
    }
    #endregion
}
