using NewLife.Serialization;
using NewLife.Siemens.Protocols;

namespace NewLife.Siemens.Messages;

/// <summary>读取变量响应</summary>
/// <remarks></remarks>
public class ReadVarResponse : S7Parameter
{
    #region 属性
    /// <summary>数据项</summary>
    public IList<ResponseItem> Items { get; set; } = [];
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public ReadVarResponse() => Code = S7Functions.ReadVar;
    #endregion

    #region 方法
    /// <summary>读取</summary>
    /// <param name="reader"></param>
    protected override void OnRead(Binary reader)
    {
        var count = reader.ReadByte();

        var list = new List<ResponseItem>();
        for (var i = 0; i < count; i++)
        {
            var di = new ResponseItem();
            di.Read(reader);

            list.Add(di);
        }
        Items = list.ToArray();
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
    }
    #endregion
}
