using NewLife.Serialization;

namespace NewLife.Siemens.Messages;

/// <summary>支持访问数据项的接口</summary>
public interface IDataItems
{
    /// <summary>读取数据项</summary>
    /// <param name="reader"></param>
    void ReadItems(Binary reader);

    /// <summary>读取数据项</summary>
    /// <param name="writer"></param>
    void WriteItems(Binary writer);
}
