using NewLife.Serialization;

namespace NewLife.Siemens.Messages;

/// <summary>设置通信</summary>
/// <remarks>各个字段都是大端。S7-1200/1500 固件 4.0+ 需要在标准 8 字节后附加 5 字节 GTM（Global Type Message）扩展。</remarks>
public class SetupMessage : S7Parameter
{
    #region 属性
    /// <summary>Ack队列的大小（主叫）</summary>
    public UInt16 MaxAmqCaller { get; set; }

    /// <summary>Ack队列的大小（被叫）</summary>
    public UInt16 MaxAmqCallee { get; set; }

    /// <summary>PDU长度</summary>
    public UInt16 PduLength { get; set; }

    /// <summary>是否有GTM扩展数据（参数总长13字节时为true）</summary>
    public Boolean HasGtm { get; set; }

    /// <summary>GTM扩展原始数据（5字节）。
    /// 客户端请求通常为 [0x00,0x00,0x00,0x00,0x00]；
    /// PLC 响应通常为 [0x00,0x00,0x00,0x01,0x00]（S7-1200）或 [0x00,0x00,0x00,0x02,0x00]（S7-1500）。</summary>
    public Byte[] GtmData { get; set; } = [];
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

        // 检测 GTM 扩展：标准 Setup 参数为 8 字节（1功能码+7数据），
        // 若还有至少 5 字节剩余则为 GTM 扩展
        if (reader.Stream is System.IO.MemoryStream ms && ms.Position + 5 <= ms.Length)
        {
            HasGtm = true;
            GtmData = reader.ReadBytes(5);
        }
    }

    /// <summary>写入</summary>
    /// <param name="writer"></param>
    protected override void OnWrite(Binary writer)
    {
        writer.WriteByte(0);

        writer.WriteUInt16(MaxAmqCaller);
        writer.WriteUInt16(MaxAmqCallee);
        writer.WriteUInt16(PduLength);

        // 写入 GTM 扩展（5字节）
        if (HasGtm && GtmData is { Length: >= 5 })
            writer.Write(GtmData, 0, 5);
    }
    #endregion
}
