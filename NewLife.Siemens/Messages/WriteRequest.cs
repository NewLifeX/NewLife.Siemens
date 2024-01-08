using NewLife.Serialization;

namespace NewLife.Siemens.Messages;

/// <summary>写入变量请求</summary>
/// <remarks></remarks>
public class WriteRequest : S7Parameter
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

        if (!reader.EndOfStream())
        {
            var list2 = new List<DataItem>();
            for (var i = 0; i < count; i++)
            {
                var di = new DataItem();
                di.Read(reader);

                list2.Add(di);
            }
            DataItems = list2.ToArray();
        }
    }

    /// <summary>写入</summary>
    /// <param name="writer"></param>
    protected override void OnWrite(Binary writer)
    {
        var count = Items?.Count ?? 0;
        writer.WriteByte((Byte)count);

        for (var i = 0; i < count; i++)
        {
            Items[i].Writer(writer);
        }

        for (var i = 0; i < count; i++)
        {
            DataItems[i].Writer(writer);
        }
    }
    #endregion
}
