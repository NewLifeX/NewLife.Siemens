using System.ComponentModel;
using NewLife.IoT;
using NewLife.IoT.Drivers;
using NewLife.IoT.Protocols;
using NewLife.IoT.ThingModels;
using NewLife.IoT.ThingSpecification;
using NewLife.Log;
using NewLife.Omron.Drivers;
using NewLife.Serialization;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;

namespace NewLife.Siemens.Drivers;

/// <summary>
/// Modbus协议封装
/// </summary>

[Driver("SiemensPLC")]
[DisplayName("西门子PLC")]
public class SiemensS7Driver : DisposeBase, IDriver
{
    protected S7PLC _plcConn;

    /// <summary>
    /// 打开通道数量
    /// </summary>
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
    /// 创建驱动参数对象，可序列化成Xml/Json作为该协议的参数模板
    /// </summary>
    /// <returns></returns>
    public virtual IDriverParameter CreateParameter() => new SiemensParameter
    {
        Address = "127.0.0.1:102",
        Rack = 0,
        Slot = 0,
    };

    /// <summary>
    /// 打开通道。一个ModbusTcp设备可能分为多个通道读取，需要共用Tcp连接，以不同节点区分
    /// </summary>
    /// <param name="device">通道</param>
    /// <param name="parameters">参数</param>
    /// <returns></returns>
    public virtual INode Open(IDevice device, IDictionary<String, Object> parameters)
    {
        var pm = JsonHelper.Convert<SiemensParameter>(parameters);

        var address = pm.Address;
        if (address.IsNullOrEmpty()) throw new ArgumentException("参数中未指定地址address");

        //var p = address.IndexOfAny(new[] { ':', '.' }); // p为3，最后截取不到正确ip
        var p = address.IndexOfAny(new[] { ':' });
        if (p < 0) throw new ArgumentException($"参数中地址address格式错误:{address}");

        var cpuType = pm.CpuType;

        if (!Enum.IsDefined(typeof(CpuType), cpuType))
        {
            throw new ArgumentException($"参数中未指定地址CpuType，必须为其中之一：{Enum.GetNames(typeof(CpuType)).Join()}");
        }

        var rack = pm.Rack;
        var slot = pm.Slot;

        var node = new SiemensNode
        {
            Address = address,
            Device = device,
            Parameter = pm,
        };

        if (_plcConn == null)
        {
            lock (this)
            {
                if (_plcConn == null)
                {
                    //var ip = address.Substring(0, p);
                    var ip = address[..p];

                    _plcConn = new S7PLC((CpuType)cpuType, ip, rack, slot)
                    {
                        Timeout = 5000,
                        Port = address.Substring(p + 1).ToInt(),
                    };

#if DEBUG
                    _plcConn.Debug = true;
#endif

                    _plcConn.OpenAsync().GetAwaiter().GetResult();

                    //if (!connect.IsSuccess) throw new Exception($"连接失败：{connect.Message}");
                }
            }
        }

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
            _plcConn?.Close();
            _plcConn.TryDispose();
            _plcConn = null;
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
        var dic = new Dictionary<String, Object>();

        if (points == null || points.Length == 0) return dic;

        foreach (var point in points)
        {
            var addr = GetAddress(point);
            if (addr.IsNullOrWhiteSpace())
            {
                dic[point.Name] = null;
                continue;
            }

            // 操作字节数组，不用设置bitNumber，但是解析需要带上
            if (addr.IndexOf('.') == -1) addr += ".0";

            var adr = new PLCAddress(addr);

            var dataType = adr.DataType;
            var db = adr.DbNumber;
            var startByteAdr = adr.StartByte;

            var data = _plcConn.ReadBytes(dataType, db, startByteAdr, (UInt16)point.Length);

            dic[point.Name] = data;
        }

        return dic;
    }

    /// <summary>
    /// 从点位中解析地址
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public virtual String GetAddress(IPoint point)
    {
        if (point == null) throw new ArgumentException("点位信息不能为空！");

        // 去掉冒号后面的位域
        var addr = point.Address;
        var p = addr.IndexOf(':');
        if (p > 0) addr = addr[..p];

        return addr;
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
        if (addr.IsNullOrWhiteSpace()) return null;

        // 操作字节数组，不用设置bitNumber，但是解析需要带上
        if (addr.IndexOf('.') == -1) addr += ".0";

        var adr = new PLCAddress(addr);

        var dataType = adr.DataType;
        var db = adr.DbNumber;
        var startByteAdr = adr.StartByte;

        if (value is Byte[] bytes)
        {
            _plcConn.WriteBytes(dataType, db, startByteAdr, bytes);
        }
        else
        {
            throw new ArgumentException("数据value不是字节数组！");
        }

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