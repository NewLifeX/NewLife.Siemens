using System.ComponentModel;
using System.Runtime.CompilerServices;
using NewLife.IoT;
using NewLife.IoT.Drivers;
using NewLife.IoT.ThingModels;
using NewLife.Omron.Drivers;
using NewLife.Serialization;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;

namespace NewLife.Siemens.Drivers;

/// <summary>西门子PLC协议封装</summary>
[Driver("SiemensPLC")]
[DisplayName("西门子PLC")]
public class SiemensS7Driver : DriverBase
{
    private S7PLC _plcConn;

    /// <summary>
    /// 打开通道数量
    /// </summary>
    private Int32 _nodes;

    #region 构造
    /// <summary>
    /// 销毁时，关闭连接
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing) => base.Dispose(disposing);
    #endregion

    #region 方法
    /// <summary>
    /// 创建驱动参数对象，可序列化成Xml/Json作为该协议的参数模板
    /// </summary>
    /// <returns></returns>
    protected override IDriverParameter OnCreateParameter() => new SiemensParameter
    {
        Address = "127.0.0.1:102",
        Rack = 0,
        Slot = 0,
    };

    /// <summary>
    /// 打开通道。一个ModbusTcp设备可能分为多个通道读取，需要共用Tcp连接，以不同节点区分
    /// </summary>
    /// <param name="device">通道</param>
    /// <param name="parameter">参数</param>
    /// <returns></returns>
    public override INode Open(IDevice device, IDriverParameter parameter)
    {
        var pm = parameter as SiemensParameter;

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

                    _plcConn = new S7PLC(cpuType, ip, rack, slot)
                    {
                        Timeout = 5000,
                        Port = address[(p + 1)..].ToInt(),
                    };

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
    public override void Close(INode node)
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
    public override IDictionary<String, Object> Read(INode node, IPoint[] points)
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
    /// 写入数据
    /// </summary>
    /// <param name="node">节点对象，可存储站号等信息，仅驱动自己识别</param>
    /// <param name="point">点位</param>
    /// <param name="value">数值</param>
    public override Object Write(INode node, IPoint point, Object value)
    {
        var addr = GetAddress(point);
        if (addr.IsNullOrWhiteSpace()) return null;

        // 操作字节数组，不用设置bitNumber，但是解析需要带上
        if (addr.IndexOf('.') == -1) addr += ".0";

        var adr = new PLCAddress(addr);

        var dataType = adr.DataType;
        var db = adr.DbNumber;
        var startByteAdr = adr.StartByte;

        Byte[] bytes = null;

        var typeStr = point.Type.ToLower();

        switch (typeStr)
        {
            case "boolean":
            case "bool":
                bytes = BitConverter.GetBytes(value.ToBoolean());
                break;
            case "short":
                bytes = BitConverter.GetBytes(Int16.Parse(value + ""));
                break;
            case "int":
                bytes = BitConverter.GetBytes(value.ToInt());
                break;
            case "float":
                bytes = BitConverter.GetBytes(Single.Parse(value + ""));
                break;
            case "byte":
                bytes = BitConverter.GetBytes(Byte.Parse(value + ""));
                break;
            case "long":
                bytes = BitConverter.GetBytes(Int64.Parse(value + ""));
                break;
            case "double":
                bytes = BitConverter.GetBytes(value.ToDouble());
                break;
            case "time":
                bytes = BitConverter.GetBytes(value.ToDateTime().Ticks);
                break;
            case "string":
            case "text":
                bytes = (value + "").GetBytes();
                break;
            default:
                throw new ArgumentException("数据value不是字节数组或有效类型！");
        }

        _plcConn.WriteBytes(dataType, db, startByteAdr, bytes);

        return null;
    }
    #endregion
}