namespace NewLife.Siemens.Protocols;

/// <summary>数据项（类型+长度+数值）</summary>
public class COTPParameter
{
    /// <summary>类型</summary>
    public COTPParameterKinds Kind { get; set; }

    /// <summary>长度</summary>
    public Byte Length { get; set; }

    /// <summary>数值</summary>
    public Object Value { get; set; }
}