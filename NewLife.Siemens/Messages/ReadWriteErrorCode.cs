namespace NewLife.Siemens.Protocols;

/// <summary>读写错误码</summary>
public enum ReadWriteErrorCode : Byte
{
    /// <summary>保留</summary>
    Reserved = 0x00,

    /// <summary>硬件错误</summary>
    HardwareFault = 0x01,

    /// <summary>禁止访问对象</summary>
    AccessingObjectNotAllowed = 0x03,

    /// <summary>地址超出范围</summary>
    AddressOutOfRange = 0x05,

    /// <summary>数据类型不支持</summary>
    DataTypeNotSupported = 0x06,

    /// <summary>数据类型不一致</summary>
    DataTypeInconsistent = 0x07,

    /// <summary>对象不存在</summary>
    ObjectDoesNotExist = 0x0a,

    /// <summary>成功</summary>
    Success = 0xff
}
