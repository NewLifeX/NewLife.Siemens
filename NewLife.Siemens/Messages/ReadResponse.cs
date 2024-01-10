using System.Drawing;
using NewLife.Serialization;

namespace NewLife.Siemens.Messages;

/// <summary>读取变量响应</summary>
/// <remarks></remarks>
public class ReadResponse : S7Parameter, IDataItems
{
    #region 属性
    /// <summary>项个数</summary>
    public Byte ItemCount { get; set; }

    /// <summary>数据项</summary>
    public IList<DataItem> Items { get; set; } = [];
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public ReadResponse() => Code = S7Functions.ReadVar;

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => $"[{Code}]{Items.FirstOrDefault()}";
    #endregion

    #region 方法
    /// <summary>读取</summary>
    /// <param name="reader"></param>
    protected override void OnRead(Binary reader)
    {
        ItemCount = reader.ReadByte();

        // 数据在Data部分
    }

    /// <summary>读取数据项</summary>
    /// <param name="reader"></param>
    public void ReadItems(Binary reader)
    {
        var list = new List<DataItem>();
        for (var i = 0; i < ItemCount; i++)
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
        var count = ItemCount = (Byte)Items.Count;
        writer.WriteByte(count);
    }

    /// <summary>写入数据项</summary>
    /// <param name="writer"></param>
    public void WriteItems(Binary writer)
    {
        for (var i = 0; i < ItemCount; i++)
        {
            Items[i].Writer(writer);
        }
    }
    #endregion
}
