namespace NewLife.Siemens.Models;

/// <summary>变量类型</summary>
public enum VarType : Byte
{
    /// <summary>位。布尔型</summary>
    Bit = 1,

    /// <summary>字节类型</summary>
    Byte,

    /// <summary>字类型，2字节</summary>
    Word,

    /// <summary>双字，4字节</summary>
    DWord,

    /// <summary>整型，2字节</summary>
    Int,

    /// <summary>长整型，4字节</summary>
    DInt,

    /// <summary>单精度，4字节</summary>
    Real,

    /// <summary>双精度，8字节</summary>
    LReal,

    /// <summary>字符串（C格式）</summary>
    String,

    /// <summary>字符串（S7格式）</summary>
    S7String,

    /// <summary>字符串（S7宽）</summary>
    S7WString,

    /// <summary>定时器</summary>
    Timer,

    /// <summary>计数器</summary>
    Counter,

    /// <summary>时间日期</summary>
    DateTime,

    /// <summary>长时间</summary>
    DateTimeLong
}