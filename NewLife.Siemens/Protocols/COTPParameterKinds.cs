namespace NewLife.Siemens.Protocols;

/// <summary>COTP参数类型</summary>
public enum COTPParameterKinds : Byte
{
    /// <summary>数据单元大小</summary>
    TpduSize = 0xC0,

    /// <summary>源设备号/CPU机架号</summary>
    SrcTsap = 0xC1,

    /// <summary>目的设备号/CPU槽号</summary>
    DstTsap = 0xC2,
}