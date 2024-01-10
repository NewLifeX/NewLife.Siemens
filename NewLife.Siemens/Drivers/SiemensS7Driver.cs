using System.ComponentModel;
using NewLife.IoT;
using NewLife.IoT.Drivers;
using NewLife.IoT.ThingModels;
using NewLife.Log;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;

namespace NewLife.Siemens.Drivers;

/// <summary>西门子PLC协议封装</summary>
[Driver("SiemensPLC")]
[DisplayName("西门子PLC")]
public class SiemensS7Driver : DriverBase
{
    #region 属性
    private S7Client? _plc;

    /// <summary>
    /// 打开通道数量
    /// </summary>
    private Int32 _nodes;
    #endregion

    #region 构造
    /// <summary>
    /// 销毁时，关闭连接
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _plc.TryDispose();
    }
    #endregion

    #region 方法
    /// <summary>
    /// 创建驱动参数对象，可序列化成Xml/Json作为该协议的参数模板
    /// </summary>
    /// <returns></returns>
    protected override IDriverParameter OnCreateParameter() => new SiemensParameter
    {
        Address = "127.0.0.1:102",
        CpuType = CpuType.S7200Smart,
        Rack = 0,
        Slot = 0,
    };

    /// <summary>
    /// 打开通道。一个ModbusTcp设备可能分为多个通道读取，需要共用Tcp连接，以不同节点区分
    /// </summary>
    /// <param name="device">通道</param>
    /// <param name="parameter">参数</param>
    /// <returns></returns>
    public override INode Open(IDevice device, IDriverParameter? parameter)
    {
        if (parameter is not SiemensParameter pm) throw new ArgumentNullException(nameof(parameter));

        var address = pm.Address;
        if (address.IsNullOrEmpty()) throw new ArgumentException("参数中未指定地址address");

        var p = address.IndexOf(':');
        if (p < 0) throw new ArgumentException($"参数中地址address格式错误:{address}");

        if (!Enum.IsDefined(typeof(CpuType), pm.CpuType))
            throw new ArgumentException($"参数中未指定地址CpuType，必须为其中之一：{Enum.GetNames(typeof(CpuType)).Join()}");

        var rack = pm.Rack;
        var slot = pm.Slot;

        var node = new SiemensNode
        {
            Address = address,
            Device = device,
            Parameter = pm,
        };

        if (_plc == null)
        {
            lock (this)
            {
                if (_plc == null)
                {
                    var ip = address[..p];
                    var port = address[(p + 1)..].ToInt();

                    _plc = new S7Client(pm.CpuType, ip, port, rack, slot)
                    {
                        Timeout = 5000,
                    };
                    if (Log != null && Log.Level <= LogLevel.Debug) _plc.Log = Log;

                    _plc.OpenAsync().GetAwaiter().GetResult();
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
    public override void Close(INode node)
    {
        if (Interlocked.Decrement(ref _nodes) <= 0)
        {
            _plc?.Close();
            _plc.TryDispose();
            _plc = null;
        }
    }

    /// <summary>
    /// 读取数据
    /// </summary>
    /// <param name="node">节点对象，可存储站号等信息，仅驱动自己识别</param>
    /// <param name="points">点位集合</param>
    /// <returns></returns>
    public override IDictionary<String, Object?> Read(INode node, IPoint[] points)
    {
        var dic = new Dictionary<String, Object?>();

        if (points == null || points.Length == 0) return dic;
        if (_plc == null) throw new Exception("PLC未打开！");

        var spec = node.Device?.Specification;
        foreach (var point in points)
        {
            var addr = GetAddress(point);
            if (addr.IsNullOrWhiteSpace()) continue;

            var name = !point.Name.IsNullOrWhiteSpace() ? point.Name : point.Address;
            if (name.IsNullOrEmpty()) continue;

            // 操作字节数组，不用设置bitNumber，但是解析需要带上
            if (addr.IndexOf('.') == -1) addr += ".0";

            var plcAddress = new PLCAddress(addr);

            var data = _plc.ReadBytes(plcAddress, point.GetLength());

            // 借助物模型转换数据类型
            if (point.GetNetType() != null)
            {
                if (spec != null)
                    dic[name] = spec.DecodeByThingModel(data, point);
                else
                    dic[name] = point.Convert(data.Swap(true, true));
            }
            else
                dic[name] = data;
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
        var addr = point.Address;
        if (addr.IsNullOrEmpty()) throw new ArgumentException("点位信息不能为空！");

        // 去掉冒号后面的位域
        var p = addr.IndexOf(':');
        if (p > 0) addr = addr[..p];

        return addr;
    }

    /// <summary>
    /// 写入数据
    /// </summary>
    /// <param name="node">节点对象，可存储站号等信息，仅驱动自己识别</param>
    /// <param name="point">点位</param>
    /// <param name="value">数值</param>
    public override Object? Write(INode node, IPoint point, Object? value)
    {
        var addr = GetAddress(point);
        if (addr.IsNullOrWhiteSpace()) return null;
        if (_plc == null) throw new Exception("PLC未打开！");

        // 借助物模型转换数据类型
        var spec = node.Device?.Specification;
        if (value != null && value is not Byte[])
        {
            // 普通数值转为字节数组
            if (spec != null)
                value = spec.EncodeByThingModel(value, point);
            else
                value = point.GetBytes(value)?.Swap(true, true);
        }

        // 操作字节数组，不用设置bitNumber，但是解析需要带上
        if (addr.IndexOf('.') == -1) addr += ".0";

        var plcAddress = new PLCAddress(addr);

        Byte[]? bytes = null;
        if (value is Byte[] v)
            bytes = v;
        else
        {
            if (point.Type.IsNullOrEmpty()) throw new ArgumentNullException(nameof(point.Type));

            bytes = point.Type.ToLower() switch
            {
                "boolean" or "bool" => BitConverter.GetBytes(value.ToBoolean()),
                "short" => BitConverter.GetBytes(Int16.Parse(value + "")),
                "int" => BitConverter.GetBytes(value.ToInt()),
                "float" => BitConverter.GetBytes(Single.Parse(value + "")),
                "byte" => BitConverter.GetBytes(Byte.Parse(value + "")),
                "long" => BitConverter.GetBytes(Int64.Parse(value + "")),
                "double" => BitConverter.GetBytes(value.ToDouble()),
                "time" => BitConverter.GetBytes(value.ToDateTime().Ticks),
                "string" or "text" => (value + "").GetBytes(),
                _ => throw new ArgumentException($"数据value不是字节数组或有效类型[{point.Type}]！"),
            };
        }

        _plc.WriteBytes(plcAddress, bytes);

        return "OK";
    }
    #endregion
}