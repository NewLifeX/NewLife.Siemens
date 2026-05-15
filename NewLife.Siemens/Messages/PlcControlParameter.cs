using NewLife.Serialization;

namespace NewLife.Siemens.Messages;

/// <summary>PLC启动模式</summary>
public enum PlcControlMode : Byte
{
    /// <summary>热重启（保留当前程序数据，继续执行）</summary>
    HotRestart = 0xFD,

    /// <summary>冷启动（清除数据区，重新初始化后运行）</summary>
    ColdRestart = 0xFF,
}

/// <summary>PLC控制指令参数（启动 / 停机）</summary>
/// <remarks>
/// 协议格式（参数块，不含首字节功能码）：
///
/// PlcStop (0x29)，参数长度 16 字节：
///   [FuncCode=0x29] 00 00 00 00 00  09  50 5F 50 52 4F 47 52 41 4D
///                   ←5字节填充→  NameLen  ←"P_PROGRAM" 9字节→
///
/// PlcStart/PI (0x28)，参数长度 20 字节：
///   [FuncCode=0x28] 00 00 00 00 00 00 {mode} 00 00  09  50 5F 50 52 4F 47 52 41 4D
///                   ←6字节填充→   Mode↑    ←2→ NameLen  ←"P_PROGRAM" 9字节→
///   mode: 0xFD=热重启，0xFF=冷启动
/// </remarks>
public class PlcControlParameter : S7Parameter
{
    #region 属性
    /// <summary>启动模式（仅 PlcStart 指令有效）</summary>
    public PlcControlMode Mode { get; set; } = PlcControlMode.HotRestart;
    #endregion

    #region 静态字段
    // "P_PROGRAM" ASCII bytes: P=0x50 _=0x5F P=0x50 R=0x52 O=0x4F G=0x47 R=0x52 A=0x41 M=0x4D
    private static readonly Byte[] _serviceNameBytes = [0x50, 0x5F, 0x50, 0x52, 0x4F, 0x47, 0x52, 0x41, 0x4D];
    #endregion

    #region 构造
    /// <summary>实例化（默认为停机）</summary>
    public PlcControlParameter() => Code = S7Functions.PlcStop;

    /// <summary>已重载。</summary>
    public override String ToString() => Code == S7Functions.PlcStop
        ? "[PlcStop]"
        : $"[PlcStart Mode={Mode}]";
    #endregion

    #region 序列化
    /// <summary>读取参数内容（消费剩余字节，避免破坏帧解析）</summary>
    /// <param name="reader"></param>
    protected override void OnRead(Binary reader)
    {
        // 读取并丢弃参数体剩余字节
        var remaining = (Int32)(reader.Stream.Length - reader.Stream.Position);
        if (remaining > 0)
            reader.ReadBytes(remaining);
    }

    /// <summary>写入参数内容</summary>
    /// <param name="writer"></param>
    protected override void OnWrite(Binary writer)
    {
        if (Code == S7Functions.PlcStop)
        {
            // 5字节填充
            writer.Write(new Byte[5]);
            // 服务名长度
            writer.WriteByte(0x09);
            // "P_PROGRAM"
            writer.Write(_serviceNameBytes, 0, _serviceNameBytes.Length);
        }
        else // PlcStart
        {
            // 6字节填充（位置[1..6]）
            writer.Write(new Byte[6]);
            // 启动模式（位置[7]）
            writer.WriteByte((Byte)Mode);
            // 2字节填充（位置[8..9]）
            writer.Write(new Byte[2]);
            // 服务名长度（位置[10]）
            writer.WriteByte(0x09);
            // "P_PROGRAM"（位置[11..19]）
            writer.Write(_serviceNameBytes, 0, _serviceNameBytes.Length);
        }
    }
    #endregion
}
