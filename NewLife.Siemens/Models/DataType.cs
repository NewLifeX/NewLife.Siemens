namespace NewLife.Siemens.Models;

/// <summary>存储区数据类型</summary>
public enum DataType
{
    /// <summary>输入区</summary>
    Input = 129,

    /// <summary>输出区</summary>
    Output = 130,

    /// <summary>内存区 (M0, M0.0, ...)</summary>
    Memory = 131,

    /// <summary>数据块(DB1, DB2, ...)</summary>
    DataBlock = 132,

    /// <summary>定时器(T1, T2, ...)</summary>
    Timer = 29,

    /// <summary>计数器 (C1, C2, ...)</summary>
    Counter = 28
}