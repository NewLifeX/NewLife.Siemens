using NewLife.Siemens.Models;

namespace NewLife.Siemens.Protocols;

/// <summary>TSAP地址对</summary>
/// <remarks>
/// 实例化
/// </remarks>
/// <param name="local"></param>
/// <param name="remote"></param>
public class TsapAddress(UInt16 local, UInt16 remote)
{
    /// <summary>本地</summary>
    public UInt16 Local { get; set; } = local;

    /// <summary>远程</summary>
    public UInt16 Remote { get; set; } = remote;

    /// <summary>获取默认地址对</summary>
    /// <param name="cpuType"></param>
    /// <param name="rack"></param>
    /// <param name="slot"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static TsapAddress GetDefaultTsapPair(CpuType cpuType, Int32 rack, Int32 slot)
    {
        if (rack < 0 || rack > 0x0F) throw new ArgumentOutOfRangeException(nameof(rack));
        if (slot < 0 || slot > 0x0F) throw new ArgumentOutOfRangeException(nameof(slot));

        return cpuType switch
        {
            CpuType.S7200 => new TsapAddress(0x1000, 0x1001),
            CpuType.Logo0BA8 => new TsapAddress(0x0100, 0x0102),
            CpuType.S7200Smart => new TsapAddress(0x1000, (UInt16)(0x03 << 8 | (Byte)((rack << 5) | slot))),
            CpuType.S71200 or CpuType.S71500 or CpuType.S7300 or CpuType.S7400 => new TsapAddress(0x0100, (UInt16)(0x03 << 8 | (Byte)((rack << 5) | slot))),
            _ => throw new NotSupportedException(),
        };
    }
}