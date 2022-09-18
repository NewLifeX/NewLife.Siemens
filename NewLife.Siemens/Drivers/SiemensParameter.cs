using System.ComponentModel;
using NewLife.IoT.Drivers;
using NewLife.Siemens.Models;

namespace NewLife.Omron.Drivers;

/// <summary>Omron参数</summary>
public class SiemensParameter : IDriverParameter
{
    /// <summary>地址。例如 127.0.0.1:9600</summary>
    [Description("地址。例如 127.0.0.1:9600")]
    public String Address { get; set; }

    /// <summary>西门子PLC种类</summary>
    [Description("西门子PLC种类")]
    public CpuType CpuType { get; set; }

    /// <summary>机架号。通常为0</summary>
    [Description("机架号。通常为0")]
    public Int16 Rack { get; set; }

    /// <summary>
    /// 插槽，对于S7300-S7400通常为2，对于S7-1200和S7-1500为0。如果使用外部以太网卡，则必须做相应设置
    /// </summary>
    [Description("插槽，对于S7300-S7400通常为2，对于S7-1200和S7-1500为0。如果使用外部以太网卡，则必须做相应设置")]
    public Int16 Slot { get; set; }
}