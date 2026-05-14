using NewLife.Siemens.Messages;

namespace NewLife.Siemens.Protocols;

/// <summary>S7客户端 — UserData扩展功能（时钟读写、SZL诊断）</summary>
public partial class S7Client
{
    #region 时钟读写
    /// <summary>读取PLC内部时钟（异步）</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>PLC当前时间（UTC+0，BCD解码）</returns>
    /// <remarks>
    /// 使用S7 UserData协议（Kind=0x07），参数块：00 01 12 04，无数据块。
    /// 响应数据块：FF 09 00 08 [8字节DATE_AND_TIME BCD]
    /// </remarks>
    public async Task<DateTime> ReadClockAsync(CancellationToken cancellationToken = default)
    {
        var rs = await InvokeUserDataAsync(new UserDataParameter
        {
            ExtraByte = 0x01,
            Function = 0x12,     // 时钟功能组（请求）
            SubFunction = 0x04,  // 读时钟
        }, cancellationToken).ConfigureAwait(false);

        if (rs?.Data == null || rs.Data.Length < 8)
            throw new InvalidOperationException("读取时钟响应数据不足");

        return S7DateTimeHelper.Decode(rs.Data);
    }

    /// <summary>写入PLC内部时钟（异步）</summary>
    /// <param name="dateTime">要设置的时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <remarks>
    /// 使用S7 UserData协议（Kind=0x07），参数块：00 01 12 02，数据块：FF 09 00 08 [8字节BCD]。
    /// 注意：S7-200/Logo0BA8 不支持此功能；S7-1200/1500 需完全访问模式。
    /// </remarks>
    public async Task WriteClockAsync(DateTime dateTime, CancellationToken cancellationToken = default)
    {
        var param = new UserDataParameter
        {
            ExtraByte = 0x01,
            Function = 0x12,     // 时钟功能组（请求）
            SubFunction = 0x02,  // 写时钟
            Data = S7DateTimeHelper.Encode(dateTime),
        };

        var rs = await InvokeUserDataAsync(param, cancellationToken).ConfigureAwait(false);

        if (rs != null && rs.ReturnCode != 0xFF)
            throw new InvalidOperationException($"写入时钟失败，ReturnCode=0x{rs.ReturnCode:X2}");
    }
    #endregion

    #region SZL 诊断读取
    /// <summary>读取SZL系统状态列表原始数据（异步）</summary>
    /// <param name="szlId">SZL标识符（如 0x001C=CPU信息，0x0111=模块信息）</param>
    /// <param name="szlIndex">SZL索引（通常为0x0000）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>SZL原始字节（含SZL头：4字节SZL-ID/Index + 4字节len/count + 记录数据）</returns>
    public async Task<Byte[]> ReadSzlAsync(UInt16 szlId, UInt16 szlIndex = 0x0000, CancellationToken cancellationToken = default)
    {
        // SZL请求数据块：FF 09 00 04 [szlId_hi] [szlId_lo] [szlIdx_hi] [szlIdx_lo]
        var reqData = new Byte[4];
        reqData[0] = (Byte)(szlId >> 8);
        reqData[1] = (Byte)(szlId & 0xFF);
        reqData[2] = (Byte)(szlIndex >> 8);
        reqData[3] = (Byte)(szlIndex & 0xFF);

        var rs = await InvokeUserDataAsync(new UserDataParameter
        {
            ExtraByte = 0x01,
            Function = 0x11,     // SZL功能组（请求）
            SubFunction = 0x0E,  // 读SZL记录
            Data = reqData,
        }, cancellationToken).ConfigureAwait(false);

        if (rs?.Data == null)
            throw new InvalidOperationException($"读取SZL(0x{szlId:X4})无响应数据");

        return rs.Data;
    }

    /// <summary>读取CPU信息（型号、序列号、固件版本）</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>CpuInfo结构体</returns>
    public async Task<S7CpuInfo> ReadCpuInfoAsync(CancellationToken cancellationToken = default)
    {
        // SZL 0x001C = 组件信息（CPU型号、序列号、版权、名称）
        var data = await ReadSzlAsync(0x001C, 0x0000, cancellationToken).ConfigureAwait(false);
        return S7CpuInfo.ParseFromSzl001C(data);
    }

    /// <summary>读取CPU运行状态</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>CPU运行状态（Run/Stop/Halt）</returns>
    public async Task<S7CpuStatus> ReadCpuStatusAsync(CancellationToken cancellationToken = default)
    {
        // SZL 0x0174 = CPU状态
        var data = await ReadSzlAsync(0x0174, 0x0000, cancellationToken).ConfigureAwait(false);
        return S7CpuInfo.ParseCpuStatus(data);
    }
    #endregion

    #region 辅助方法
    /// <summary>发起UserData请求（Kind=0x07）</summary>
    /// <param name="request">UserData参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应的UserData参数（含数据块）</returns>
    private async Task<UserDataParameter?> InvokeUserDataAsync(UserDataParameter request, CancellationToken cancellationToken = default)
    {
        var msg = new S7Message
        {
            Kind = S7Kinds.UserData,
        };
        msg.SetParameter(request);

        var rs = await RequestAsync(msg, cancellationToken).ConfigureAwait(false);
        return rs?.Parameters.OfType<UserDataParameter>().FirstOrDefault();
    }
    #endregion
}
