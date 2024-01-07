using NewLife.Serialization;
using NewLife.Siemens.Protocols;

namespace NewLife.Siemens.Messages;

/// <summary>读取变量</summary>
/// <remarks></remarks>
public class ReadVarMessage : S7Parameter
{
    #region 属性
    /// <summary>Ack队列的大小（主叫）</summary>
    public DataItem[] Items { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public ReadVarMessage() => Code = S7Functions.Setup;
    #endregion

    #region 方法
    /// <summary>读取</summary>
    /// <param name="reader"></param>
    protected override void OnRead(Binary reader)
    {
        var count = reader.ReadByte();

        var list = new List<DataItem>();
        for (var i = 0; i < count; i++)
        {
            var di = new DataItem();
            di.Read(reader);

            list.Add(di);
        }
        Items = list.ToArray();
    }

    /// <summary>写入</summary>
    /// <param name="writer"></param>
    protected override void OnWrite(Binary writer)
    {
        var count = Items?.Length ?? 0;
        writer.WriteByte((Byte)count);

        for (var i = 0; i < count; i++)
        {
            Items[i].Writer(writer);
        }
    }
    #endregion
}
