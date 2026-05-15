using NewLife.Siemens.Messages;

namespace NewLife.Siemens.Protocols;

/// <summary>S7客户端 — 程序块管理（列表、上传、下载）</summary>
/// <remarks>
/// 块管理功能属于 PG（Programming Guide）专用协议范畴，需要 PLC 支持 PG 接入模式。
/// 三步握手协议（StartUpload/Upload/EndUpload 和 StartDownload/Download/EndDownload）
/// 与 Siemens STEP 7 / TIA Portal 的 PG 接入方式完全一致，需要关闭 TIA Portal 的
/// "专有知识产权保护"（KOP/FBD/STL 密码）才能上传。
/// </remarks>
public partial class S7Client
{
    #region 块列表
    /// <summary>获取 PLC 中指定类型的程序块列表</summary>
    /// <param name="blockType">块类型（默认 DB）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>块信息数组</returns>
    /// <remarks>
    /// 使用 SZL 0x0022 读取指定类型的块编号列表。
    /// SZL 索引由块类型决定：DB=0x0A00，OB=0x0800，FB=0x0C00，FC=0x0E00 等。
    /// </remarks>
    public async Task<S7BlockInfo[]> ListBlocksAsync(S7BlockType blockType = S7BlockType.DB, CancellationToken cancellationToken = default)
    {
        // SZL 0x0022 各块类型的索引映射
        UInt16 szlIndex;
        switch (blockType)
        {
            case S7BlockType.OB:  szlIndex = 0x0800; break;
            case S7BlockType.DB:  szlIndex = 0x0A00; break;
            case S7BlockType.SFB: szlIndex = 0x0B00; break;
            case S7BlockType.FB:  szlIndex = 0x0C00; break;
            case S7BlockType.SFC: szlIndex = 0x0D00; break;
            case S7BlockType.FC:  szlIndex = 0x0E00; break;
            default:              szlIndex = 0x0A00; break;
        }

        WriteLog("ListBlocks: blockType={0} szlIndex=0x{1:X4}", blockType, szlIndex);

        var data = await ReadSzlAsync(0x0022, szlIndex, cancellationToken).ConfigureAwait(false);
        return S7BlockInfo.ParseFromSzl0022(data, blockType);
    }
    #endregion

    #region 块上传（从 PLC 读取程序块字节）
    /// <summary>从 PLC 上传程序块原始字节（三步 UPLOAD 协议）</summary>
    /// <param name="blockType">块类型</param>
    /// <param name="blockNumber">块编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>块的原始 MC7 字节流（可用于下载到其他 PLC 或离线分析）</returns>
    /// <remarks>
    /// 协议流程：
    ///   1. StartUpload (0x1D)：发起上传，响应中包含数据总长度和 JobId
    ///   2. Upload (0x1E)：循环接收数据帧，每帧含 MoreData 标志
    ///   3. EndUpload (0x1F)：通知 PLC 上传完成
    ///
    /// 注意：部分 PLC 需 PG 连接模式（TSAP 前缀 0x01xx 而非 0x03xx）才支持此功能。
    /// S7-1200/1500 需在 TIA Portal 关闭程序块的密码保护。
    /// </remarks>
    public async Task<Byte[]> UploadBlockAsync(S7BlockType blockType, Int32 blockNumber, CancellationToken cancellationToken = default)
    {
        // Step 1: StartUpload
        var (jobId, dataSize) = await StartUploadAsync(blockType, blockNumber, cancellationToken).ConfigureAwait(false);
        WriteLog("StartUpload: blockType={0} blockNum={1} jobId=0x{2:X8} size={3}", blockType, blockNumber, jobId, dataSize);

        // Step 2: Upload loop
        var buffer = new List<Byte>(dataSize > 0 ? dataSize : 4096);
        var moreData = true;
        while (moreData)
        {
            var (chunk, hasMore) = await UploadChunkAsync(jobId, cancellationToken).ConfigureAwait(false);
            buffer.AddRange(chunk);
            moreData = hasMore;
        }

        // Step 3: EndUpload
        await EndUploadAsync(jobId, cancellationToken).ConfigureAwait(false);
        WriteLog("UploadBlock 完成，总字节数={0}", buffer.Count);

        return [.. buffer];
    }

    private async Task<(UInt32 jobId, Int32 dataSize)> StartUploadAsync(S7BlockType blockType, Int32 blockNumber, CancellationToken ct)
    {
        // StartUpload 参数：function(0x1D) + flags(0x12 0x0C 0x00) + fileId('_'=0x5F) + blockType + blockNum(5 ASCII) + dest('A'=0x41)
        // 参考 Snap7 S7_START_UPLOAD 报文结构
        var blockNumStr = blockNumber.ToString("D5");
        var blockNumAscii = System.Text.Encoding.ASCII.GetBytes(blockNumStr);

        var paramBytes = new Byte[13];
        paramBytes[0] = 0x12;                    // spec type
        paramBytes[1] = 0x0C;                    // spec length
        paramBytes[2] = 0x00;                    // syntax id
        paramBytes[3] = 0x5F;                    // file identifier '_' (current block)
        paramBytes[4] = (Byte)blockType;         // block type
        Array.Copy(blockNumAscii, 0, paramBytes, 5, 5); // block number (5 ASCII digits)
        paramBytes[10] = 0x41;                   // destination file system 'A' = active
        paramBytes[11] = 0x00;
        paramBytes[12] = 0x00;

        var rawParam = new UploadRawParameter(S7Functions.StartUpload, paramBytes);
        var msg = new S7Message { Kind = S7Kinds.Job };
        msg.SetParameter(rawParam);

        var rs = await RequestAsync(msg, ct).ConfigureAwait(false);
        if (rs == null)
            throw new InvalidOperationException("StartUpload：PLC 无响应");
        if (rs.ErrorClass != 0 || rs.ErrorCode != 0)
            throw new InvalidOperationException($"StartUpload 失败：ErrorClass=0x{rs.ErrorClass:X2}");

        // 解析响应：jobId (4字节, offset 取决于响应格式)
        // 响应参数：[0]=function [1..4]=jobId [5..6]=dataLen
        var rspParam = rs.Parameters.OfType<UploadRawParameter>().FirstOrDefault();
        if (rspParam == null || rspParam.RawBytes.Length < 7)
            return (0, 0);

        var jobId = (UInt32)((rspParam.RawBytes[1] << 24) | (rspParam.RawBytes[2] << 16)
                           | (rspParam.RawBytes[3] << 8) | rspParam.RawBytes[4]);
        var dataSize = (Int32)((rspParam.RawBytes[5] << 8) | rspParam.RawBytes[6]);

        return (jobId, dataSize);
    }

    private async Task<(Byte[] chunk, Boolean moreData)> UploadChunkAsync(UInt32 jobId, CancellationToken ct)
    {
        // Upload 参数：function(0x1E) + reserved(1) + jobId(4)
        var paramBytes = new Byte[5];
        paramBytes[0] = 0x00;
        paramBytes[1] = (Byte)(jobId >> 24);
        paramBytes[2] = (Byte)(jobId >> 16);
        paramBytes[3] = (Byte)(jobId >> 8);
        paramBytes[4] = (Byte)jobId;

        var rawParam = new UploadRawParameter(S7Functions.Upload, paramBytes);
        var msg = new S7Message { Kind = S7Kinds.Job };
        msg.SetParameter(rawParam);

        var rs = await RequestAsync(msg, ct).ConfigureAwait(false);
        if (rs == null)
            throw new InvalidOperationException("Upload：PLC 无响应");
        if (rs.ErrorClass != 0 || rs.ErrorCode != 0)
            throw new InvalidOperationException($"Upload 失败：ErrorClass=0x{rs.ErrorClass:X2}");

        var rspParam = rs.Parameters.OfType<UploadRawParameter>().FirstOrDefault();
        if (rspParam == null) return ([], false);

        // 响应：[0]=function [1]=moreData(0x01=more, 0x00=last) [2..3]=dataLen [4..]=data
        var more = rspParam.RawBytes.Length > 1 && rspParam.RawBytes[1] == 0x01;
        Byte[] chunk = [];
        if (rspParam.RawBytes.Length > 4)
        {
            var dataLen = (Int32)((rspParam.RawBytes[2] << 8) | rspParam.RawBytes[3]);
            var start = 4;
            var end = Math.Min(start + dataLen, rspParam.RawBytes.Length);
            chunk = new Byte[end - start];
            Array.Copy(rspParam.RawBytes, start, chunk, 0, chunk.Length);
        }

        return (chunk, more);
    }

    private async Task EndUploadAsync(UInt32 jobId, CancellationToken ct)
    {
        // EndUpload 参数：function(0x1F) + errorCode(4) + jobId(4)
        var paramBytes = new Byte[8];
        paramBytes[0] = 0x00; paramBytes[1] = 0x00; paramBytes[2] = 0x00; paramBytes[3] = 0x00; // error = 0
        paramBytes[4] = (Byte)(jobId >> 24);
        paramBytes[5] = (Byte)(jobId >> 16);
        paramBytes[6] = (Byte)(jobId >> 8);
        paramBytes[7] = (Byte)jobId;

        var rawParam = new UploadRawParameter(S7Functions.EndUpload, paramBytes);
        var msg = new S7Message { Kind = S7Kinds.Job };
        msg.SetParameter(rawParam);

        var rs = await RequestAsync(msg, ct).ConfigureAwait(false);
        if (rs != null && (rs.ErrorClass != 0 || rs.ErrorCode != 0))
            WriteLog("EndUpload 警告：ErrorClass=0x{0:X2} ErrorCode=0x{1:X2}", rs.ErrorClass, rs.ErrorCode);
    }
    #endregion

    #region 块下载（向 PLC 写入程序块字节）
    /// <summary>向 PLC 下载程序块（三步 DOWNLOAD 协议）</summary>
    /// <param name="blockType">块类型</param>
    /// <param name="blockNumber">块编号</param>
    /// <param name="blockData">块的原始 MC7 字节流（通常来自 UploadBlockAsync 的结果）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <remarks>
    /// 协议流程：
    ///   1. StartDownload (0x1A)：发起下载，发送块大小
    ///   2. Download (0x1B)：循环发送数据帧（每帧最大 PDU - 开销），最后一帧 MoreData=false
    ///   3. EndDownload (0x1C)：通知 PLC 下载完成，PLC 编译并激活块
    ///
    /// 注意：下载会覆盖 PLC 中已有的同编号块；DB 块会被重新初始化（非保持型数据清零）。
    /// </remarks>
    public async Task DownloadBlockAsync(S7BlockType blockType, Int32 blockNumber, Byte[] blockData, CancellationToken cancellationToken = default)
    {
        // Step 1: StartDownload
        await StartDownloadAsync(blockType, blockNumber, blockData.Length, cancellationToken).ConfigureAwait(false);
        WriteLog("StartDownload: blockType={0} blockNum={1} size={2}", blockType, blockNumber, blockData.Length);

        // Step 2: Download chunks（每帧最大 = PDU - 32 字节开销）
        var chunkSize = MaxPDUSize - 32;
        var offset = 0;
        while (offset < blockData.Length)
        {
            var toSend = Math.Min(chunkSize, blockData.Length - offset);
            var chunk = new Byte[toSend];
            Array.Copy(blockData, offset, chunk, 0, toSend);
            var moreData = (offset + toSend) < blockData.Length;
            await DownloadChunkAsync(chunk, moreData, cancellationToken).ConfigureAwait(false);
            offset += toSend;
        }

        // Step 3: EndDownload
        await EndDownloadAsync(cancellationToken).ConfigureAwait(false);
        WriteLog("DownloadBlock 完成");
    }

    private async Task StartDownloadAsync(S7BlockType blockType, Int32 blockNumber, Int32 dataLength, CancellationToken ct)
    {
        // StartDownload 参数类似 StartUpload，但包含 MC7 文件大小（7 ASCII 字节）
        var blockNumStr = blockNumber.ToString("D5");
        var blockNumAscii = System.Text.Encoding.ASCII.GetBytes(blockNumStr);
        var fileLenStr = dataLength.ToString("D7");
        var fileLenAscii = System.Text.Encoding.ASCII.GetBytes(fileLenStr);

        var paramBytes = new Byte[20];
        paramBytes[0] = 0x12;
        paramBytes[1] = 0x0C;
        paramBytes[2] = 0x00;
        paramBytes[3] = 0x5F;                    // '_'
        paramBytes[4] = (Byte)blockType;
        Array.Copy(blockNumAscii, 0, paramBytes, 5, 5);
        paramBytes[10] = 0x41;                   // 'A' active
        paramBytes[11] = 0x00;
        paramBytes[12] = 0x07;                   // length of file size ASCII
        Array.Copy(fileLenAscii, 0, paramBytes, 13, 7);

        var rawParam = new UploadRawParameter(S7Functions.StartDownload, paramBytes);
        var msg = new S7Message { Kind = S7Kinds.Job };
        msg.SetParameter(rawParam);

        var rs = await RequestAsync(msg, ct).ConfigureAwait(false);
        if (rs == null)
            throw new InvalidOperationException("StartDownload：PLC 无响应");
        if (rs.ErrorClass != 0 || rs.ErrorCode != 0)
            throw new InvalidOperationException($"StartDownload 失败：ErrorClass=0x{rs.ErrorClass:X2}");
    }

    private async Task DownloadChunkAsync(Byte[] chunk, Boolean moreData, CancellationToken ct)
    {
        // Download 参数：function(0x1B) + moreData(0x01/0x00) + unknown(0x00) + dataLen(2) + data
        var paramBytes = new Byte[3 + chunk.Length];
        paramBytes[0] = moreData ? (Byte)0x01 : (Byte)0x00;
        paramBytes[1] = 0x00;
        paramBytes[2] = (Byte)(chunk.Length >> 8);
        // data follows
        Array.Copy(chunk, 0, paramBytes, 3, chunk.Length);

        var rawParam = new UploadRawParameter(S7Functions.Download, paramBytes);
        var msg = new S7Message { Kind = S7Kinds.Job };
        msg.SetParameter(rawParam);

        var rs = await RequestAsync(msg, ct).ConfigureAwait(false);
        if (rs == null)
            throw new InvalidOperationException("Download：PLC 无响应");
        if (rs.ErrorClass != 0 || rs.ErrorCode != 0)
            throw new InvalidOperationException($"Download 失败：ErrorClass=0x{rs.ErrorClass:X2}");
    }

    private async Task EndDownloadAsync(CancellationToken ct)
    {
        var rawParam = new UploadRawParameter(S7Functions.EndDownload, []);
        var msg = new S7Message { Kind = S7Kinds.Job };
        msg.SetParameter(rawParam);

        var rs = await RequestAsync(msg, ct).ConfigureAwait(false);
        if (rs != null && (rs.ErrorClass != 0 || rs.ErrorCode != 0))
            WriteLog("EndDownload 警告：ErrorClass=0x{0:X2} ErrorCode=0x{1:X2}", rs.ErrorClass, rs.ErrorCode);
    }
    #endregion
}

/// <summary>块传输专用参数容器（原始字节直接读写）</summary>
/// <remarks>上传/下载协议的参数结构复杂且存在版本差异，使用原始字节容器避免解析错误。</remarks>
internal class UploadRawParameter : Messages.S7Parameter
{
    /// <summary>原始参数字节（不含首字节功能码）</summary>
    public Byte[] RawBytes { get; set; } = [];

    public UploadRawParameter(Messages.S7Functions func, Byte[] raw)
    {
        Code = func;
        RawBytes = raw;
    }

    protected override void OnRead(NewLife.Serialization.Binary reader)
    {
        var remaining = (Int32)(reader.Stream.Length - reader.Stream.Position);
        if (remaining > 0)
            RawBytes = reader.ReadBytes(remaining);
    }

    protected override void OnWrite(NewLife.Serialization.Binary writer)
    {
        if (RawBytes.Length > 0)
            writer.Write(RawBytes, 0, RawBytes.Length);
    }
}
