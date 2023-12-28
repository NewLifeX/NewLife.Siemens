namespace NewLife.Siemens.Protocols;

/// <summary>数据项（类型+长度+数值）</summary>
public class TLV
{
    /// <summary>类型</summary>
    public Byte Type { get; set; }

    /// <summary>长度</summary>
    public Byte Length { get; set; }

    /// <summary>数值</summary>
    public Object Value { get; set; }
}
