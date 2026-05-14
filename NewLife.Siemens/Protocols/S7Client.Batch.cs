using NewLife.Remoting;
using NewLife.Siemens.Messages;
using NewLife.Siemens.Models;

namespace NewLife.Siemens.Protocols;

/// <summary>S7客户端 — 批量多变量读写 API</summary>
public partial class S7Client
{
    /// <summary>每批次最多请求变量数（S7 协议建议上限 20 项）</summary>
    private const Int32 MaxItemsPerBatch = 20;

    #region 批量读取
    /// <summary>一次或多次 PDU 批量读取多个变量，结果填入 DataItem.Value</summary>
    /// <param name="dataItems">描述各变量的 DataItem 列表（需填写 DataType/VarType/DbNumber/StartByteAdr/BitAdr/Count）</param>
    public void ReadMultipleVars(IList<DataItem> dataItems)
    {
        if (dataItems == null || dataItems.Count == 0) return;

        // 按批次拆分（每批最多 MaxItemsPerBatch 项）
        for (var batchStart = 0; batchStart < dataItems.Count; batchStart += MaxItemsPerBatch)
        {
            var batchCount = Math.Min(MaxItemsPerBatch, dataItems.Count - batchStart);
            var batch = new DataItem[batchCount];
            for (var i = 0; i < batchCount; i++)
                batch[i] = dataItems[batchStart + i];

            ReadBatch(batch);
        }
    }

    /// <summary>异步批量读取多个变量</summary>
    /// <param name="dataItems">DataItem 列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ReadMultipleVarsAsync(IList<DataItem> dataItems, CancellationToken cancellationToken = default)
    {
        if (dataItems == null || dataItems.Count == 0) return;

        for (var batchStart = 0; batchStart < dataItems.Count; batchStart += MaxItemsPerBatch)
        {
            var batchCount = Math.Min(MaxItemsPerBatch, dataItems.Count - batchStart);
            var batch = new DataItem[batchCount];
            for (var i = 0; i < batchCount; i++)
                batch[i] = dataItems[batchStart + i];

            await ReadBatchAsync(batch, cancellationToken).ConfigureAwait(false);
        }
    }

    private void ReadBatch(DataItem[] batch)
    {
        var request = new ReadRequest();
        foreach (var item in batch)
            request.Items.Add(BuildRequestItem(item));

        var rs = InvokeAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
        if (rs is not ReadResponse response) return;

        for (var i = 0; i < batch.Length && i < response.Items.Count; i++)
        {
            var respItem = response.Items[i];
            if (respItem.Code != ReadWriteErrorCode.Success)
                throw new ApiException((Int32)respItem.Code, respItem.Code.ToString());

            batch[i].Value = ParseDataItemValue(respItem, batch[i]);
        }
    }

    private async Task ReadBatchAsync(DataItem[] batch, CancellationToken cancellationToken)
    {
        var request = new ReadRequest();
        foreach (var item in batch)
            request.Items.Add(BuildRequestItem(item));

        var rs = await InvokeAsync(request, cancellationToken).ConfigureAwait(false);
        if (rs is not ReadResponse response) return;

        for (var i = 0; i < batch.Length && i < response.Items.Count; i++)
        {
            var respItem = response.Items[i];
            if (respItem.Code != ReadWriteErrorCode.Success)
                throw new ApiException((Int32)respItem.Code, respItem.Code.ToString());

            batch[i].Value = ParseDataItemValue(respItem, batch[i]);
        }
    }
    #endregion

    #region 批量写入
    /// <summary>一次或多次 PDU 批量写入多个变量（写入前需在 DataItem.Value 中设置值）</summary>
    /// <param name="dataItems">DataItem 列表（需填写元数据属性及 Value）</param>
    public void WriteMultipleVars(IList<DataItem> dataItems)
    {
        if (dataItems == null || dataItems.Count == 0) return;

        for (var batchStart = 0; batchStart < dataItems.Count; batchStart += MaxItemsPerBatch)
        {
            var batchCount = Math.Min(MaxItemsPerBatch, dataItems.Count - batchStart);
            var batch = new DataItem[batchCount];
            for (var i = 0; i < batchCount; i++)
                batch[i] = dataItems[batchStart + i];

            WriteBatch(batch);
        }
    }

    /// <summary>异步批量写入多个变量</summary>
    /// <param name="dataItems">DataItem 列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task WriteMultipleVarsAsync(IList<DataItem> dataItems, CancellationToken cancellationToken = default)
    {
        if (dataItems == null || dataItems.Count == 0) return;

        for (var batchStart = 0; batchStart < dataItems.Count; batchStart += MaxItemsPerBatch)
        {
            var batchCount = Math.Min(MaxItemsPerBatch, dataItems.Count - batchStart);
            var batch = new DataItem[batchCount];
            for (var i = 0; i < batchCount; i++)
                batch[i] = dataItems[batchStart + i];

            await WriteBatchAsync(batch, cancellationToken).ConfigureAwait(false);
        }
    }

    private void WriteBatch(DataItem[] batch)
    {
        var request = new WriteRequest();
        foreach (var item in batch)
        {
            request.Items.Add(BuildRequestItem(item));
            request.DataItems.Add(BuildDataItemForWrite(item));
        }

        var rs = InvokeAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
        if (rs is not WriteResponse response) return;

        for (var i = 0; i < batch.Length && i < response.Items.Count; i++)
        {
            var respItem = response.Items[i];
            if (respItem.Code != ReadWriteErrorCode.Success)
                throw new ApiException((Int32)respItem.Code, respItem.Code.ToString());
        }
    }

    private async Task WriteBatchAsync(DataItem[] batch, CancellationToken cancellationToken)
    {
        var request = new WriteRequest();
        foreach (var item in batch)
        {
            request.Items.Add(BuildRequestItem(item));
            request.DataItems.Add(BuildDataItemForWrite(item));
        }

        var rs = await InvokeAsync(request, cancellationToken).ConfigureAwait(false);
        if (rs is not WriteResponse response) return;

        for (var i = 0; i < batch.Length && i < response.Items.Count; i++)
        {
            var respItem = response.Items[i];
            if (respItem.Code != ReadWriteErrorCode.Success)
                throw new ApiException((Int32)respItem.Code, respItem.Code.ToString());
        }
    }
    #endregion

    #region 内部构建方法
    private static RequestItem BuildRequestItem(DataItem item)
    {
        var isBit = item.VarType == VarType.Bit;
        var elementSize = isBit ? 1 : GetVarTypeByteSize(item.VarType);
        var byteCount = elementSize * item.Count;

        var bitAddr = item.BitAdr >= 0 ? item.BitAdr : 0;
        var address = (UInt32)(item.StartByteAdr * 8 + bitAddr);

        return new RequestItem
        {
            SpecType = 0x12,
            SyntaxId = 0x10,
            TransportSize = (Byte)(isBit ? 1 : 2),
            Count = (UInt16)(isBit ? item.Count : byteCount),
            DbNumber = (UInt16)item.DbNumber,
            Area = item.DataType,
            Address = address,
        };
    }

    private static DataItem BuildDataItemForWrite(DataItem item)
    {
        var isBit = item.VarType == VarType.Bit;
        var rawData = item.Value != null ? ConvertToBytes(item.Value, item.VarType) : [];

        return new DataItem
        {
            TransportSize = (Byte)(isBit ? 0x03 : 0x04),
            Data = rawData,
        };
    }

    private static Object? ParseDataItemValue(DataItem respItem, DataItem meta)
    {
        if (respItem.Data == null || respItem.Data.Length == 0) return null;

        var isBit = meta.VarType == VarType.Bit;
        if (isBit) return respItem.Data[0] != 0;

        // 多元素数组
        if (meta.Count > 1)
        {
            var elementSize = GetVarTypeByteSize(meta.VarType);
            var arr = new Object[meta.Count];
            for (var i = 0; i < meta.Count; i++)
            {
                var slice = new Byte[elementSize];
                var srcOffset = i * elementSize;
                if (srcOffset + elementSize <= respItem.Data.Length)
                    Array.Copy(respItem.Data, srcOffset, slice, 0, elementSize);
                arr[i] = ConvertSingleValue(slice, meta.VarType)!;
            }
            return arr;
        }

        return ConvertSingleValue(respItem.Data, meta.VarType);
    }

    private static Object? ConvertSingleValue(Byte[] data, VarType varType)
    {
        var buf = (Byte[])data.Clone();
        if (buf.Length > 1) Array.Reverse(buf);

        return varType switch
        {
            VarType.Byte => buf.Length > 0 ? (Object)buf[0] : null,
            VarType.Word => buf.Length >= 2 ? (Object)BitConverter.ToUInt16(buf, 0) : null,
            VarType.DWord => buf.Length >= 4 ? (Object)BitConverter.ToUInt32(buf, 0) : null,
            VarType.Int => buf.Length >= 2 ? (Object)BitConverter.ToInt16(buf, 0) : null,
            VarType.DInt => buf.Length >= 4 ? (Object)BitConverter.ToInt32(buf, 0) : null,
            VarType.Real => buf.Length >= 4 ? (Object)BitConverter.ToSingle(buf, 0) : null,
            VarType.LReal => buf.Length >= 8 ? (Object)BitConverter.ToDouble(buf, 0) : null,
            VarType.Timer => buf.Length >= 2 ? (Object)BitConverter.ToUInt16(buf, 0) : null,
            VarType.Counter => buf.Length >= 2 ? (Object)BitConverter.ToUInt16(buf, 0) : null,
            _ => data,
        };
    }
    #endregion
}
