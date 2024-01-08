﻿using NewLife.Serialization;

namespace NewLife.Siemens.Messages;

/// <summary>写入变量响应</summary>
/// <remarks></remarks>
public class WriteResponse : S7Parameter
{
    #region 属性
    ///// <summary>项个数</summary>
    //public Byte ItemCount { get; set; }

    /// <summary>数据项</summary>
    public IList<DataItem> Items { get; set; } = [];
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public WriteResponse() => Code = S7Functions.WriteVar;
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

        Items = list;
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