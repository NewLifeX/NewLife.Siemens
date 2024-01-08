namespace NewLife.Siemens.Messages;

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
