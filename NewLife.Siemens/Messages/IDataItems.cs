using NewLife.Serialization;

namespace NewLife.Siemens.Messages;

public interface IDataItems
{
    void ReadItems(Binary reader);

    void WriteItems(Binary writer);
}
