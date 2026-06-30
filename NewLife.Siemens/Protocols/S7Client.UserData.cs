using NewLife.Siemens.Messages;
using NewLife.Siemens.Models;

namespace NewLife.Siemens.Protocols;

/// <summary>S7客户端 — UserData扩展功能（时钟读写、SZL诊断、PLC控制）</summary>
public partial class S7Client
{
    #region PLC 启停控制
    /// <summary>停止 PLC（STOP 状态）</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <remarks>
    /// 发送 PlcStop (0x29) Job 请求，携带 "P_PROGRAM" 服务名。
    /// 需提前设置 <see cref="AllowPlcControl"/> = true；否则抛出 InvalidOperationException。
    /// 仅在受控维护场景使用，停机将立即中断 PLC 程序执行。
    /// </remarks>
    public async Task PlcStopAsync(CancellationToken cancellationToken = default)
    {
        if (!AllowPlcControl)
            throw new InvalidOperationException("PLC 控制指令被禁止：请先设置 AllowPlcControl = true");

        WriteLog("发送 PlcStop 指令");

        var msg = new S7Message { Kind = S7Kinds.Job };
        msg.SetParameter(new PlcControlParameter { Code = S7Functions.PlcStop });

        var rs = await RequestAsync(msg, cancellationToken).ConfigureAwait(false);
        if (rs == null)
            throw new InvalidOperationException("PlcStop：PLC 无响应");
        if (rs.ErrorClass != 0 || rs.ErrorCode != 0)
            throw new InvalidOperationException($"PlcStop 失败：ErrorClass=0x{rs.ErrorClass:X2} ErrorCode=0x{rs.ErrorCode:X2}");
    }

    /// <summary>热重启 PLC（保留数据区，恢复 RUN 状态）</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <remarks>
    /// 发送 PlcStart (0x28) Job 请求，模式字节 0xFD（热重启）。
    /// 需提前设置 <see cref="AllowPlcControl"/> = true。
    /// PLC 需处于 STOP 状态，否则部分型号会返回错误。
    /// </remarks>
    public async Task PlcHotRestartAsync(CancellationToken cancellationToken = default)
    {
        if (!AllowPlcControl)
            throw new InvalidOperationException("PLC 控制指令被禁止：请先设置 AllowPlcControl = true");

        WriteLog("发送 PlcHotRestart 指令");

        var msg = new S7Message { Kind = S7Kinds.Job };
        msg.SetParameter(new PlcControlParameter
        {
            Code = S7Functions.PlcStart,
            Mode = PlcControlMode.HotRestart,
        });

        var rs = await RequestAsync(msg, cancellationToken).ConfigureAwait(false);
        if (rs == null)
            throw new InvalidOperationException("PlcHotRestart：PLC 无响应");
        if (rs.ErrorClass != 0 || rs.ErrorCode != 0)
            throw new InvalidOperationException($"PlcHotRestart 失败：ErrorClass=0x{rs.ErrorClass:X2} ErrorCode=0x{rs.ErrorCode:X2}");
    }

    /// <summary>冷启动 PLC（清除数据区，重新初始化后进入 RUN）</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <remarks>
    /// 发送 PlcStart (0x28) Job 请求，模式字节 0xFF（冷启动）。
    /// 需提前设置 <see cref="AllowPlcControl"/> = true。
    /// 冷启动会清除所有 DB 实例数据（非保持型），谨慎使用。
    /// </remarks>
    public async Task PlcColdStartAsync(CancellationToken cancellationToken = default)
    {
        if (!AllowPlcControl)
            throw new InvalidOperationException("PLC 控制指令被禁止：请先设置 AllowPlcControl = true");

        WriteLog("发送 PlcColdStart 指令");

        var msg = new S7Message { Kind = S7Kinds.Job };
        msg.SetParameter(new PlcControlParameter
        {
            Code = S7Functions.PlcStart,
            Mode = PlcControlMode.ColdRestart,
        });

        var rs = await RequestAsync(msg, cancellationToken).ConfigureAwait(false);
        if (rs == null)
            throw new InvalidOperationException("PlcColdStart：PLC 无响应");
        if (rs.ErrorClass != 0 || rs.ErrorCode != 0)
            throw new InvalidOperationException($"PlcColdStart 失败：ErrorClass=0x{rs.ErrorClass:X2} ErrorCode=0x{rs.ErrorCode:X2}");
    }
    #endregion

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

    #region 密码认证
    /// <summary>向 PLC 发送会话密码以获取操作权限（S7-1200/1500）</summary>
    /// <param name="password">TIA Portal 中设置的 8 位密码（ASCII，不足8位自动空格补齐）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>认证后的保护级别（NoProtection=成功获取完全权限）</returns>
    /// <remarks>
    /// 使用 S7 UserData CPU 功能组（Function=0x00, SubFunction=0x01=SetPassword）发送密码。
    /// 密码为 8 字节固定长度，不足 8 字符时右侧空格补齐。
    /// 认证失败时 PLC 返回原保护级别不变，可据此判断密码是否正确。
    ///
    /// 注意：
    /// - 仅 S7-1200/1500 固件 2.0+ 支持此功能
    /// - 需先在 TIA Portal 中为 CPU 配置访问级别和密码
    /// - 连接关闭后认证状态自动清除
    /// </remarks>
    public async Task<ProtectionLevel> SetPasswordAsync(String password, CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password));

        // 密码固定 8 字节，ASCII 编码，不足右侧空格补齐
        var pwdBytes = new Byte[8];
        var ascii = System.Text.Encoding.ASCII.GetBytes(password);
        var copyLen = Math.Min(ascii.Length, 8);
        Array.Copy(ascii, 0, pwdBytes, 0, copyLen);
        for (var i = copyLen; i < 8; i++)
            pwdBytes[i] = 0x20; // 空格补齐

        WriteLog("SetPassword: 发送密码认证（{0} 字符）", password.Length);

        var param = new UserDataParameter
        {
            ExtraByte = 0x01,
            Function = 0x00,     // CPU 功能组（请求）
            SubFunction = 0x01,  // SetPassword
            Data = pwdBytes,
        };

        var rs = await InvokeUserDataAsync(param, cancellationToken).ConfigureAwait(false);
        if (rs == null)
            throw new InvalidOperationException("SetPassword：PLC 无响应");

        // 响应数据：FF 09 00 04 [level] [reserved] [reserved] [reserved]
        var level = ProtectionLevel.NoProtection;
        if (rs.Data is { Length: >= 4 })
        {
            level = (ProtectionLevel)(rs.Data[0] & 0x03);
            WriteLog("SetPassword: 认证后保护级别={0}", level);
        }

        return level;
    }

    /// <summary>清除当前会话的密码认证状态，恢复 PLC 到原始保护级别</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <remarks>
    /// 使用 CPU 功能组 SubFunction=0x02（ClearPassword）。
    /// 连接关闭时认证状态自动清除，通常无需显式调用。
    /// </remarks>
    public async Task ClearPasswordAsync(CancellationToken cancellationToken = default)
    {
        WriteLog("ClearPassword: 清除密码认证");

        var param = new UserDataParameter
        {
            ExtraByte = 0x01,
            Function = 0x00,     // CPU 功能组（请求）
            SubFunction = 0x02,  // ClearPassword
        };

        var rs = await InvokeUserDataAsync(param, cancellationToken).ConfigureAwait(false);
        if (rs == null)
            WriteLog("ClearPassword: PLC 无响应（可能已断开）");
    }

    /// <summary>读取 PLC 当前保护级别（无需密码）</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>当前保护级别</returns>
    public async Task<ProtectionLevel> ReadProtectionLevelAsync(CancellationToken cancellationToken = default)
    {
        WriteLog("ReadProtectionLevel: 查询保护级别");

        // 使用 CPU 功能组 SubFunction=0x03 或读取 SZL 0x0232
        // 部分固件版本通过 SZL 0x0232 Index 0x0004 返回保护信息
        var data = await ReadSzlAsync(0x0232, 0x0004, cancellationToken).ConfigureAwait(false);

        // SZL 0232 Index 0004 格式：
        // [0..3]=szlId+index, [4..5]=recLen, [6..7]=recCount
        // [8]=level, [9]=reserved, ...
        if (data is { Length: >= 9 })
        {
            var level = (ProtectionLevel)(data[8] & 0x03);
            WriteLog("ReadProtectionLevel: 当前级别={0}", level);
            return level;
        }

        return ProtectionLevel.NoProtection;
    }
    #endregion
}
