namespace NewLife.Siemens.Models;

/// <summary>S7-1200/1500 密码保护级别</summary>
/// <remarks>
/// 在 TIA Portal 中为 CPU 设置访问级别，控制允许的操作范围。
/// 通过 UserData CPU 功能组（Function=0x00）读取/设置。
/// </remarks>
public enum ProtectionLevel : Byte
{
    /// <summary>无保护。允许所有操作（默认）</summary>
    NoProtection = 0,

    /// <summary>写保护。允许读操作，禁止写操作</summary>
    WriteProtected = 1,

    /// <summary>读写保护。禁止读写操作（HMI 访问仍可用）</summary>
    ReadWriteProtected = 2,

    /// <summary>完全保护。禁止所有 PG/HMI 访问（需密码才能解锁）</summary>
    FullProtection = 3,
}
