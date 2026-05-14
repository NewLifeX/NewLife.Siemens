using System.Text;

namespace NewLife.Siemens.Protocols;

/// <summary>CPU运行状态</summary>
public enum S7CpuStatus : Byte
{
    /// <summary>未知/初始</summary>
    Unknown = 0x00,

    /// <summary>停止（STOP）</summary>
    Stop = 0x01,

    /// <summary>运行（RUN）</summary>
    Run = 0x03,

    /// <summary>暂停（HOLD）</summary>
    Halt = 0x02,
}

/// <summary>SZL 0x001C 组件信息结构</summary>
/// <remarks>
/// SZL 0x001C 记录包含 CPU 名称、版权、序列号等字符串，每条记录固定长度。
/// 此处简化解析：从原始字节中提取可打印字符串。
/// </remarks>
public class S7CpuInfo
{
    #region 属性
    /// <summary>CPU模块订单号（如"6ES7 315-2EH14-0AB0"）</summary>
    public String ArticleNumber { get; set; } = String.Empty;

    /// <summary>CPU序列号</summary>
    public String SerialNumber { get; set; } = String.Empty;

    /// <summary>固件版本（如"V3.3"）</summary>
    public String FirmwareVersion { get; set; } = String.Empty;

    /// <summary>原始SZL数据</summary>
    public Byte[] RawData { get; set; } = [];
    #endregion

    /// <summary>解析SZL 0x001C数据</summary>
    /// <param name="data">ReadSzlAsync返回的原始字节</param>
    /// <returns>CpuInfo实例</returns>
    public static S7CpuInfo ParseFromSzl001C(Byte[] data)
    {
        var info = new S7CpuInfo { RawData = data };

        // SZL 0x001C 响应格式（简化）：
        // [0-3]: SZL头（szl_id hi/lo + szl_index hi/lo）
        // [4-5]: 每条记录长度（SZL_DR_LEN）
        // [6-7]: 记录数量
        // [8...]: 记录数据
        //
        // 典型记录结构（每条记录包含：
        //   [0-1]: 子项索引（0x0001=CPU模块，0x0002=系统程序等）
        //   [2-21]: 20字节ASCII订单号
        //   [22-41]: 20字节ASCII序列号
        //   [42-43]: 硬件版本
        //   [44-45]: 固件版本
        //   ...
        // 由于不同PLC型号格式略有差异，此处只提取可识别字符串

        if (data == null || data.Length < 8)
            return info;

        // 尝试从第8字节起提取可打印ASCII字符串字段
        var pos = 8;
        if (pos + 2 <= data.Length)
        {
            // 跳过子项索引（2字节）
            pos += 2;
            info.ArticleNumber = ReadAsciiString(data, pos, 20);
            pos += 20;
            info.SerialNumber = ReadAsciiString(data, pos, 20);
            pos += 20;
            pos += 2; // 硬件版本
            if (pos + 2 <= data.Length)
            {
                var fwHi = data[pos];
                var fwLo = data[pos + 1];
                info.FirmwareVersion = $"V{fwHi}.{fwLo}";
            }
        }

        return info;
    }

    /// <summary>解析SZL 0x0174 CPU状态</summary>
    /// <param name="data">ReadSzlAsync返回的原始字节</param>
    /// <returns>CPU运行状态</returns>
    public static S7CpuStatus ParseFromSzl0174(Byte[] data)
    {
        // SZL 0x0174 格式：[0-7]=头，[8]=CPU状态字节
        // 状态字节：0x01=STOP，0x03=RUN，0x02=HOLD
        if (data == null || data.Length < 9)
            return S7CpuStatus.Unknown;

        var statusByte = data[8];
        return statusByte switch
        {
            0x01 => S7CpuStatus.Stop,
            0x03 => S7CpuStatus.Run,
            0x02 => S7CpuStatus.Halt,
            _ => S7CpuStatus.Unknown,
        };
    }

    /// <summary>解析SZL 0x0174 CPU状态（供S7Client调用）</summary>
    /// <param name="data">ReadSzlAsync返回的原始字节</param>
    /// <returns>CPU运行状态</returns>
    public static S7CpuStatus ParseCpuStatus(Byte[] data) => ParseFromSzl0174(data);

    private static String ReadAsciiString(Byte[] data, Int32 offset, Int32 maxLen)
    {
        if (offset >= data.Length) return String.Empty;
        var len = Math.Min(maxLen, data.Length - offset);
        var end = offset;
        while (end < offset + len && data[end] != 0) end++;
        return Encoding.ASCII.GetString(data, offset, end - offset).Trim();
    }
}
