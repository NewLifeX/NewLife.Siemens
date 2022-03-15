using NewLife.IoT.Drivers;

namespace NewLife.Siemens.Drivers;

/// <summary>
/// Modbus节点
/// </summary>
public class SiemensNode : INode
{
    /// <summary>主机地址</summary>
    public Byte Host { get; set; }

    /// <summary>通道</summary>
    public IChannel Channel { get; set; }
}