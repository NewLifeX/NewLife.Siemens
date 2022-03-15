using NewLife.IoT.Drivers;
using NewLife.IoT.Protocols;
using NewLife.IoT.ThingModels;
using NewLife.IoT.ThingSpecification;
using NewLife.Log;

namespace NewLife.Siemens.Drivers;

/// <summary>
/// Modbus协议封装
/// </summary>
public abstract class SiemensS7 : DisposeBase
{
    private Int32 _nodes;

    #region 构造
    /// <summary>
    /// 销毁时，关闭连接
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);
    }
    #endregion

    #region 方法
    /// <summary>
    /// 打开通道。一个ModbusTcp设备可能分为多个通道读取，需要共用Tcp连接，以不同节点区分
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="parameters">参数</param>
    /// <returns></returns>
    public virtual INode Open(IChannel channel, IDictionary<String, Object> parameters)
    {
        var node = new SiemensNode
        {
            Host = (Byte)parameters["host"],
            Channel = channel
        };

        Interlocked.Increment(ref _nodes);

        return node;
    }

    /// <summary>
    /// 关闭设备驱动
    /// </summary>
    /// <param name="node"></param>
    public virtual void Close(INode node)
    {
        if (Interlocked.Decrement(ref _nodes) <= 0)
        {
        }
    }

    /// <summary>
    /// 读取数据
    /// </summary>
    /// <param name="node">节点对象，可存储站号等信息，仅驱动自己识别</param>
    /// <param name="points">点位集合</param>
    /// <returns></returns>
    public virtual IDictionary<String, Object> Read(INode node, IPoint[] points)
    {
        if (points == null || points.Length == 0) return null;

        return null;
    }

    /// <summary>
    /// 从点位中解析地址
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public virtual UInt16 GetAddress(IPoint point)
    {
        if (point == null) return UInt16.MaxValue;

        // 去掉冒号后面的位域
        var addr = point.Address;
        var p = addr.IndexOf(':');
        if (p > 0) addr = addr[..p];

        // 按十六进制解析返回
        if (addr.StartsWithIgnoreCase("0x")) return addr[2..].ToHex().ToUInt16(0, false);

        // 直接转数字范围
        return (UInt16)addr.ToInt(UInt16.MaxValue);
    }

    /// <summary>
    /// 从点位中计算寄存器个数
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public virtual Int32 GetCount(IPoint point)
    {
        // 字节数转寄存器数，要除以2
        var count = point.Length / 2;
        if (count > 0) return count;

        if (point.Type.IsNullOrEmpty()) return 1;

        return point.Type.ToLower() switch
        {
            "byte" or "sbyte" or "bool" => 1,
            "short" or "ushort" or "int16" or "uint16" => 2 / 2,
            "int" or "uint" or "int32" or "uint32" => 4 / 2,
            "long" or "ulong" or "int64" or "uint64" => 8 / 2,
            "float" or "single" => 4 / 2,
            "double" or "decimal" => 8 / 2,
            _ => 1,
        };
    }

    /// <summary>
    /// 写入数据
    /// </summary>
    /// <param name="node">节点对象，可存储站号等信息，仅驱动自己识别</param>
    /// <param name="point">点位</param>
    /// <param name="value">数值</param>
    public virtual Object Write(INode node, IPoint point, Object value)
    {
        var addr = GetAddress(point);
        if (addr == UInt16.MaxValue) return null;

        if (value == null) return null;

        return null;
    }

    /// <summary>原始数据转寄存器数组</summary>
    /// <param name="data"></param>
    /// <param name="point"></param>
    /// <param name="spec"></param>
    /// <returns></returns>
    private UInt16[] ConvertToRegister(Object data, IPoint point, ThingSpec spec)
    {
        // 找到物属性定义
        var pi = spec?.Properties?.FirstOrDefault(e => e.Id.EqualIgnoreCase(point.Name));
        var type = pi?.DataType?.Type;
        if (type.IsNullOrEmpty()) type = point.Type;
        if (type.IsNullOrEmpty()) return null;

        switch (type.ToLower())
        {
            case "short":
            case "int16":
            case "ushort":
            case "uint16":
                {
                    var n = data.ToInt();
                    return new[] { (UInt16)n };
                }
            case "int":
            case "int32":
            case "uint":
            case "uint32":
                {
                    var n = data.ToInt();
                    return new[] { (UInt16)(n >> 16), (UInt16)(n & 0xFFFF) };
                }
            case "long":
            case "int64":
            case "ulong":
            case "uint64":
                {
                    var n = data.ToLong();
                    return new[] { (UInt16)(n >> 48), (UInt16)(n >> 32), (UInt16)(n >> 16), (UInt16)(n & 0xFFFF) };
                }
            case "float":
            case "single":
                {
                    var d = (Single)data.ToDouble();
                    //var n = BitConverter.SingleToInt32Bits(d);
                    var n = (UInt32)d;
                    return new[] { (UInt16)(n >> 16), (UInt16)(n & 0xFFFF) };
                }
            case "double":
            case "decimal":
                {
                    var d = data.ToDouble();
                    //var n = BitConverter.DoubleToInt64Bits(d);
                    var n = (UInt64)d;
                    return new[] { (UInt16)(n >> 48), (UInt16)(n >> 32), (UInt16)(n >> 16), (UInt16)(n & 0xFFFF) };
                }
            case "bool":
            case "boolean":
                {
                    return data.ToBoolean() ? new[] { (UInt16)0xFF00 } : new[] { (UInt16)0x00 };
                }
            default:
                return null;
        }
        //return type.ToLower() switch
        //{
        //    "short" or "int16" or "ushort" or "uint16" => new[] { (UInt16)n },
        //    "int" or "int32" or "uint" or "uint32" => new[] { (UInt16)(n >> 16), (UInt16)(n & 0xFFFF) },
        //    "long" or "int64" or "uint64" => { },
        //    "float" or "single" => BitConverter.SingleToInt32Bits((Single)data.ToDouble()).GetBytes(false),
        //    "double" or "decimal" => BitConverter.DoubleToInt64Bits(data.ToDouble()).GetBytes(false),
        //    "bool" or "boolean" => data.ToBoolean() ? new[] { (UInt16)0xFF00 } : new[] { (UInt16)0x00 },
        //    _ => null,
        //};
    }

    /// <summary>
    /// 控制设备，特殊功能使用
    /// </summary>
    /// <param name="node"></param>
    /// <param name="parameters"></param>
    /// <exception cref="NotImplementedException"></exception>
    public virtual void Control(INode node, IDictionary<String, Object> parameters) => throw new NotImplementedException();
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; }

    /// <summary>性能追踪器</summary>
    public ITracer Tracer { get; set; }
    #endregion
}