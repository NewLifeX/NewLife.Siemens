using NewLife.IoT;
using NewLife.IoT.Drivers;

namespace NewLife.Siemens.Drivers;

/// <summary>西门子PLC节点</summary>
public class SiemensNode : INode
{
    /// <summary>主机地址</summary>
    public String? Address { get; set; }

    /// <summary>通道</summary>
    public IDriver Driver { get; set; } = null!;

    /// <summary>设备</summary>
    public IDevice? Device { get; set; }

    /// <summary>参数</summary>
    public IDriverParameter? Parameter { get; set; }

}