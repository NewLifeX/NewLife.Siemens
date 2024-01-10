using NewLife.Serialization;

namespace NewLife.Siemens.Messages;

/// <summary>读取变量</summary>
/// <remarks></remarks>
public class ReadRequest : S7Parameter
{
    #region 属性
    /// <summary>项个数</summary>
    public Byte ItemCount { get; set; }

    /// <summary>数据项</summary>
    public IList<RequestItem> Items { get; set; } = [];
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public ReadRequest() => Code = S7Functions.ReadVar;

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => $"[{Code}]{Items.FirstOrDefault()}";
    #endregion

    #region 方法
    /// <summary>读取</summary>
    /// <param name="reader"></param>
    protected override void OnRead(Binary reader)
    {
        var count = ItemCount = reader.ReadByte();

        var list = new List<RequestItem>();
        for (var i = 0; i < count; i++)
        {
            var di = new RequestItem();
            di.Read(reader);

            list.Add(di);
        }

        Items = list;
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
    #endregion
}
