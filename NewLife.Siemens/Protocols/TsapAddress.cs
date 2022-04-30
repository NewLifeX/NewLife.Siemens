using NewLife.Siemens.Models;

namespace NewLife.Siemens.Protocols;

/// <summary>TSAP地址对</summary>
public class TsapAddress
{
    /// <summary>本地</summary>
    public UInt16 Local { get; set; }

    /// <summary>远程</summary>
    public UInt16 Remote { get; set; }

    /// <summary>
    /// 实例化
    /// </summary>
    /// <param name="local"></param>
    /// <param name="remote"></param>
    public TsapAddress(UInt16 local, UInt16 remote)
    {
        Local = local;
        Remote = remote;
    }

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

        switch (cpuType)
        {
            case CpuType.S7200:
                return new TsapAddress(0x1000, 0x1001);
            case CpuType.Logo0BA8:
                return new TsapAddress(0x0100, 0x0102);
            case CpuType.S7200Smart:
            case CpuType.S71200:
            case CpuType.S71500:
            case CpuType.S7300:
            case CpuType.S7400:
                return new TsapAddress(0x0100, (UInt16)(0x03 << 8 | (Byte)((rack << 5) | slot)));
            default:
                throw new NotSupportedException();
        }
    }
}