namespace NewLife.Siemens.Protocols;

/// <summary>S7报文类型</summary>
public enum S7Kinds : Byte
{
    /// <summary>作业请求。由主设备发送的请求（读写存储器、块，启动停止设备，设置通信）</summary>
    Job = 0x01,

    /// <summary>确认响应。没有数据的简单确认</summary>
    Ack = 0x02,

    /// <summary>确认数据响应。没有可选数据，一般响应Job请求</summary>
    AckData = 0x03,

    /// <summary>原始协议的扩展。参数字段包含请求响应ID</summary>
    /// <remarks>用于编程/调试，SZL读取，安全功能，时间设置，循环读取</remarks>
    UserData = 0x07,
}
