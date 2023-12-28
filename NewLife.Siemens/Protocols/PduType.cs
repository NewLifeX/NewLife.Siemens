namespace NewLife.Siemens.Protocols;

/// <summary>PDU类型</summary>
public enum PduType : Byte
{
    /// <summary>数据帧</summary>
    Data = 0xf0,

    /// <summary>CR连接请求帧</summary>
    ConnectionRequest = 0xe0,

    /// <summary>CC连接确认帧</summary>
    ConnectionConfirmed = 0xd0
}
