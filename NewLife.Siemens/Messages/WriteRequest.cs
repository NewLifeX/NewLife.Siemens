using NewLife.Serialization;

namespace NewLife.Siemens.Messages;

/// <summary>写入变量请求</summary>
/// <remarks></remarks>
public class WriteRequest : S7Parameter, IDataItems
{
    #region 属性
    /// <summary>请求项</summary>
    public IList<RequestItem> Items { get; set; } = [];

    /// <summary>数据项</summary>
    public IList<DataItem> DataItems { get; set; } = [];
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public WriteRequest() => Code = S7Functions.WriteVar;

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => $"[{Code}]{Items.FirstOrDefault()}";
    #endregion

    #region 方法
    /// <summary>读取</summary>
    /// <param name="reader"></param>
    protected override void OnRead(Binary reader)
    {
        var count = reader.ReadByte();

        var list = new List<RequestItem>();
        for (var i = 0; i < count; i++)
        {
            var di = new RequestItem();
            di.Read(reader);

            list.Add(di);
        }
        Items = list.ToArray();
    }

    /// <summary>读取数据项</summary>
    /// <param name="reader"></param>
    public void ReadItems(Binary reader)
    {
        var list = new List<DataItem>();
        for (var i = 0; i < Items.Count; i++)
        {
            var di = new DataItem();
            di.Read(reader);

            list.Add(di);
        }
        DataItems = list.ToArray();
    }

    /// <summary>写入</summary>
    /// <param name="writer"></param>
    protected override void OnWrite(Binary writer)
    {
        var count = Items.Count;
        writer.WriteByte((Byte)count);

        for (var i = 0; i < count; i++)
        {
            Items[i].Writer(writer);
        }
    }

    /// <summary>写入数据项</summary>
    /// <param name="writer"></param>
    public void WriteItems(Binary writer)
    {
        for (var i = 0; i < Items.Count; i++)
        {
            DataItems[i].Writer(writer);
        }
    }
    #endregion
}
