namespace NewLife.Siemens.Protocols;

/// <summary>S7 DATE_AND_TIME BCD编码/解码辅助类</summary>
/// <remarks>
/// S7 DATE_AND_TIME格式（8字节，全部BCD编码）：
///   Byte 0: 年（2位，00-89=2000-2089，90-99=1990-1999）
///   Byte 1: 月（01-12）
///   Byte 2: 日（01-31）
///   Byte 3: 时（00-23）
///   Byte 4: 分（00-59）
///   Byte 5: 秒（00-59）
///   Byte 6: 毫秒高2位BCD（0-99）
///   Byte 7: 高半字节=毫秒低1位BCD，低半字节=星期（1=周一...7=周日）
/// </remarks>
public static class S7DateTimeHelper
{
    /// <summary>将 DateTime 编码为 S7 DATE_AND_TIME 8字节BCD数组</summary>
    /// <param name="dt">要编码的时间</param>
    /// <returns>8字节BCD数组</returns>
    public static Byte[] Encode(DateTime dt)
    {
        var ms = dt.Millisecond;
        var buf = new Byte[8];
        buf[0] = ToBcd(dt.Year % 100);
        buf[1] = ToBcd(dt.Month);
        buf[2] = ToBcd(dt.Day);
        buf[3] = ToBcd(dt.Hour);
        buf[4] = ToBcd(dt.Minute);
        buf[5] = ToBcd(dt.Second);
        buf[6] = ToBcd(ms / 10);               // 毫秒高2位
        buf[7] = (Byte)((ToBcd(ms % 10) << 4) | ToDayOfWeek(dt.DayOfWeek));
        return buf;
    }

    /// <summary>从 S7 DATE_AND_TIME 8字节BCD数组解码为 DateTime</summary>
    /// <param name="buf">8字节BCD数组</param>
    /// <returns>解码后的时间（Kind=Unspecified）</returns>
    public static DateTime Decode(Byte[] buf)
    {
        if (buf == null || buf.Length < 8)
            throw new ArgumentException("DATE_AND_TIME数据不足8字节", nameof(buf));

        var y2 = FromBcd(buf[0]);
        var year = y2 >= 90 ? 1900 + y2 : 2000 + y2;
        var month = FromBcd(buf[1]);
        var day = FromBcd(buf[2]);
        var hour = FromBcd(buf[3]);
        var minute = FromBcd(buf[4]);
        var second = FromBcd(buf[5]);
        var ms = FromBcd(buf[6]) * 10 + (buf[7] >> 4);

        return new DateTime(year, month, day, hour, minute, second, ms);
    }

    private static Byte ToBcd(Int32 value) => (Byte)(((value / 10) << 4) | (value % 10));

    private static Int32 FromBcd(Byte b) => (b >> 4) * 10 + (b & 0x0F);

    /// <summary>将 .NET DayOfWeek 转换为 S7 星期编码（1=周一...7=周日）</summary>
    private static Byte ToDayOfWeek(DayOfWeek dow) => dow == DayOfWeek.Sunday ? (Byte)7 : (Byte)dow;
}
