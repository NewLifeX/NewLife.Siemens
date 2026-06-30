using System;
using NewLife.Log;
using NewLife.Net;
using NewLife.Serialization;
using NewLife.Siemens.Messages;
using NewLife.Siemens.Models;

namespace NewLife.Siemens.Protocols;

/// <summary>S7服务端。用于仿真</summary>
public class S7Server : NetServer<S7Session>
{
    #region 内存区域
    private readonly Byte[] _memory = new Byte[65536];
    private readonly Byte[] _input = new Byte[65536];
    private readonly Byte[] _output = new Byte[65536];
    private readonly Byte[] _timer = new Byte[512];
    private readonly Byte[] _counter = new Byte[512];
    private readonly Dictionary<Int32, Byte[]> _db = [];
    private readonly Object _memLock = new();

    /// <summary>获取指定区域的内存缓冲区</summary>
    /// <param name="area">存储区域</param>
    /// <param name="dbNum">数据块编号（仅 DataBlock 区域使用）</param>
    public Byte[] GetMemory(DataType area, Int32 dbNum = 0)
    {
        return area switch
        {
            DataType.Memory => _memory,
            DataType.Input => _input,
            DataType.Output => _output,
            DataType.Timer => _timer,
            DataType.Counter => _counter,
            DataType.DataBlock => GetDb(dbNum),
            _ => throw new ArgumentException($"不支持的存储区域: {area}", nameof(area)),
        };
    }

    private Byte[] GetDb(Int32 dbNum)
    {
        lock (_memLock)
        {
            if (!_db.TryGetValue(dbNum, out var buf))
            {
                buf = new Byte[65536];
                _db[dbNum] = buf;
            }
            return buf;
        }
    }

    /// <summary>通过地址字符串写入初始值（供测试预置数据）</summary>
    /// <param name="address">PLC 地址，如 "DB1.DBW0"</param>
    /// <param name="data">要写入的字节（大端序，与 S7 协议一致）</param>
    public void SetValue(String address, Byte[] data)
    {
        var addr = new PLCAddress(address);
        var mem = GetMemory(addr.DataType, addr.DbNumber);
        var byteOffset = addr.StartByte;

        if (addr.BitNumber >= 0 && data.Length == 1)
        {
            // 位写入：更新对应位，保留其余位
            var bitMask = (Byte)(1 << addr.BitNumber);
            if (data[0] != 0)
                mem[byteOffset] |= bitMask;
            else
                mem[byteOffset] &= (Byte)~bitMask;
        }
        else
        {
            var copyLen = Math.Min(data.Length, mem.Length - byteOffset);
            if (copyLen > 0)
                Array.Copy(data, 0, mem, byteOffset, copyLen);
        }
    }

    /// <summary>通过地址字符串写入 Int16 值（大端序）</summary>
    public void SetValue(String address, Int16 value) => SetValue(address, value.GetBytes(false));

    /// <summary>通过地址字符串写入 Int32 值（大端序）</summary>
    public void SetValue(String address, Int32 value) => SetValue(address, value.GetBytes(false));

    /// <summary>通过地址字符串写入 Single 值（大端序）</summary>
    public void SetValue(String address, Single value)
    {
        var buf = BitConverter.GetBytes(value);
        Array.Reverse(buf);
        SetValue(address, buf);
    }

    /// <summary>通过地址字符串写入 Double 值（大端序，对应 S7 LReal）</summary>
    public void SetValue(String address, Double value)
    {
        var buf = BitConverter.GetBytes(value);
        Array.Reverse(buf);
        SetValue(address, buf);
    }
    #endregion

    #region 时钟与SZL模拟
    /// <summary>模拟的PLC时钟。null时使用系统时间</summary>
    public DateTime? SimulatedClock { get; set; }

    /// <summary>模拟的 CPU 运行状态（初始为 Run）</summary>
    public S7CpuStatus CpuStatus { get; internal set; } = S7CpuStatus.Run;

    /// <summary>模拟的 CPU 保护级别（默认无保护）</summary>
    public ProtectionLevel ProtectionLevel { get; set; } = ProtectionLevel.NoProtection;

    /// <summary>模拟的 CPU 密码（8字节ASCII，空=无密码）。仅当 ProtectionLevel != NoProtection 时生效</summary>
    public String? SimulatedPassword { get; set; }

    /// <summary>模拟SZL数据字典（Key=szlId左移16位或szlIndex，Value=原始SZL记录字节）</summary>
    private readonly Dictionary<UInt32, Byte[]> _szlData = [];

    /// <summary>程序块存储（Key=blockType<<16|blockNumber，Value=MC7字节流）</summary>
    private readonly Dictionary<UInt32, Byte[]> _blocks = [];

    /// <summary>设置SZL模拟数据</summary>
    /// <param name="szlId">SZL标识符</param>
    /// <param name="szlIndex">SZL索引</param>
    /// <param name="data">SZL原始数据字节</param>
    public void SetSzlData(UInt16 szlId, UInt16 szlIndex, Byte[] data)
    {
        var key = (UInt32)((szlId << 16) | szlIndex);
        _szlData[key] = data;
    }

    /// <summary>向服务器预置程序块（用于上传测试）</summary>
    /// <param name="blockType">块类型</param>
    /// <param name="blockNumber">块编号</param>
    /// <param name="data">MC7 字节流</param>
    public void SetBlock(S7BlockType blockType, Int32 blockNumber, Byte[] data)
    {
        var key = (UInt32)(((Byte)blockType << 16) | (UInt16)blockNumber);
        lock (_blocks) _blocks[key] = data;
    }

    /// <summary>读取服务器中存储的程序块（下载后验证用）</summary>
    /// <param name="blockType">块类型</param>
    /// <param name="blockNumber">块编号</param>
    public Byte[]? GetBlock(S7BlockType blockType, Int32 blockNumber)
    {
        var key = (UInt32)(((Byte)blockType << 16) | (UInt16)blockNumber);
        lock (_blocks) return _blocks.TryGetValue(key, out var d) ? d : null;
    }

    internal Byte[] GetSzlData(UInt16 szlId, UInt16 szlIndex)
    {
        // SZL 0x0174：CPU 运行状态，动态生成，反映 CpuStatus 当前值
        if (szlId == 0x0174)
        {
            var raw = new Byte[10];
            raw[0] = 0x01; raw[1] = 0x74; // szlId
            raw[2] = (Byte)(szlIndex >> 8); raw[3] = (Byte)(szlIndex & 0xFF);
            raw[8] = CpuStatus switch
            {
                S7CpuStatus.Stop => 0x01,
                S7CpuStatus.Halt => 0x02,
                S7CpuStatus.Run  => 0x03,
                _                => 0x00,
            };
            return raw;
        }

        // SZL 0x0022：程序块列表，动态生成
        if (szlId == 0x0022)
            return BuildSzl0022(szlIndex);

        var key = (UInt32)((szlId << 16) | szlIndex);
        if (_szlData.TryGetValue(key, out var data)) return data;

        // 未预置时返回空SZL头（8字节：szlId+szlIndex+recLen+recCount）
        var empty = new Byte[8];
        empty[0] = (Byte)(szlId >> 8);
        empty[1] = (Byte)(szlId & 0xFF);
        empty[2] = (Byte)(szlIndex >> 8);
        empty[3] = (Byte)(szlIndex & 0xFF);
        return empty;
    }

    /// <summary>构建 SZL 0x0022 块列表响应（每条记录4字节：blockNum_hi + blockNum_lo + flags + lang）</summary>
    private Byte[] BuildSzl0022(UInt16 szlIndex)
    {
        // 块类型通过 szlIndex 高字节推断：0x0A00=DB, 0x0800=OB, 0x0C00=FB, 0x0E00=FC, 0x0B00=SFB, 0x0D00=SFC
        var blockType = (szlIndex >> 8) switch
        {
            0x08 => S7BlockType.OB,
            0x0A => S7BlockType.DB,
            0x0B => S7BlockType.SDB,
            0x0C => S7BlockType.FB,
            0x0D => S7BlockType.SFC,
            0x0E => S7BlockType.FC,
            _    => S7BlockType.DB,
        };

        // 从 _blocks 查指定类型的块编号，DB 类型还包含 _db 内存区的已分配块
        var blockTypeKey = (Byte)blockType;
        SortedSet<Int32> numSet;
        lock (_blocks)
        {
            numSet = new SortedSet<Int32>(
                _blocks.Keys
                    .Where(k => (Byte)(k >> 16) == blockTypeKey)
                    .Select(k => (Int32)(k & 0xFFFF)));
        }
        if (blockType == S7BlockType.DB)
        {
            lock (_memLock)
            {
                foreach (var k in _db.Keys) numSet.Add(k);
            }
        }
        var blockNums = numSet.ToList();

        var recLen = 4; // 每条记录 4 字节
        var recCount = blockNums.Count;
        var buf = new Byte[8 + recLen * recCount];

        // SZL 头（8字节）
        buf[0] = 0x00; buf[1] = 0x22;
        buf[2] = (Byte)(szlIndex >> 8); buf[3] = (Byte)(szlIndex & 0xFF);
        buf[4] = (Byte)(recLen >> 8); buf[5] = (Byte)(recLen & 0xFF);
        buf[6] = (Byte)(recCount >> 8); buf[7] = (Byte)(recCount & 0xFF);

        for (var i = 0; i < recCount; i++)
        {
            var off = 8 + i * recLen;
            var blockNum = blockNums[i];
            buf[off]     = (Byte)(blockNum >> 8);
            buf[off + 1] = (Byte)(blockNum & 0xFF);
            buf[off + 2] = 0x01; // flags: block exists
            buf[off + 3] = 0x01; // language: STL
        }

        return buf;
    }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public S7Server()
    {
        Port = 102;
        ProtocolType = NetType.Tcp;

        Add(new TPKTCodec());
    }
    #endregion
}

/// <summary>S7连接会话</summary>
public class S7Session : NetSession<S7Server>
{
    private Boolean _logined;

    // 上传会话状态
    private UInt32 _uploadJobId;
    private Byte[]? _uploadData;
    private Int32 _uploadOffset;

    // 下载会话状态
    private S7BlockType _downloadType;
    private Int32 _downloadNumber;
    private readonly List<Byte> _downloadBuffer = [];

    /// <summary>客户端连接时</summary>
    protected override void OnConnected()
    {
        WriteLog("S7连接：{0}", Remote);

        base.OnConnected();
    }

    /// <summary>客户端断开连接时</summary>
    /// <param name="reason"></param>
    protected override void OnDisconnected(String reason)
    {
        WriteLog("S7断开：{0} {1}", Remote, reason);

        base.OnDisconnected(reason);
    }

    /// <summary>报错时</summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected override void OnError(Object? sender, ExceptionEventArgs e)
    {
        WriteLog("S7错误：{0}", e.Exception.Message);

        base.OnError(sender, e);
    }

    /// <summary>收到数据时</summary>
    /// <param name="e"></param>
    protected override void OnReceive(ReceivedEventArgs e)
    {
        if (e.Message is not TPKT tpkt || tpkt.Data == null) return;

        var cotp = new COTP();
        if (cotp.Read(tpkt.Data))
        {
            WriteLog("<={0}", cotp.ToString());
            switch (cotp.Type)
            {
                case PduType.Data:
                    if (!_logined)
                        OnConnectionRequest(cotp);
                    else
                        OnData(cotp);
                    break;
                case PduType.ConnectionRequest:
                    OnConnectionRequest(cotp);
                    break;
                default:
                    break;
            }
        }

        base.OnReceive(e);
    }

    void OnConnectionRequest(COTP cotp)
    {
        var rs = new COTP
        {
            Type = PduType.ConnectionConfirmed,
            Destination = cotp.Source,
            Source = cotp.Destination,
            Number = cotp.Number,
        };

        Send(rs.ToPacket(true));

        _logined = true;
    }

    void OnData(COTP cotp)
    {
        if (cotp.Data == null) return;

        var msg = new S7Message();
        if (!msg.Read(cotp.Data)) return;

        switch (msg.Kind)
        {
            case S7Kinds.Job:
                {
                    var rs = new S7Message
                    {
                        Kind = S7Kinds.AckData,
                        Sequence = msg.Sequence,
                    };

                    var pm = msg.Parameters.FirstOrDefault();
                    if (pm == null)
                    {
                        Send(rs.ToCOTP().ToPacket(true));
                        break;
                    }
                    switch (pm.Code)
                    {
                        case S7Functions.ReadVar:
                            var pm2 = OnRead(pm as ReadRequest);
                            if (pm2 != null)
                                rs.Parameters.Add(pm2);
                            break;
                        case S7Functions.WriteVar:
                            var pm3 = OnWrite(pm as WriteRequest);
                            if (pm3 != null)
                                rs.Parameters.Add(pm3);
                            break;
                        case S7Functions.PlcStop:
                            WriteLog("PlcStop 收到，状态 -> Stop");
                            Host.CpuStatus = S7CpuStatus.Stop;
                            // 返回空 AckData（ErrorClass=0 ErrorCode=0）
                            break;
                        case S7Functions.PlcStart:
                            var pcp = pm as PlcControlParameter;
                            WriteLog("PlcStart 收到 Mode={0}，状态 -> Run", pcp?.Mode);
                            Host.CpuStatus = S7CpuStatus.Run;
                            break;
                        case S7Functions.StartUpload:
                            var suParam = OnStartUpload(pm as UploadRawParameter);
                            if (suParam != null) rs.Parameters.Add(suParam);
                            break;
                        case S7Functions.Upload:
                            var upParam = OnUpload(pm as UploadRawParameter);
                            if (upParam != null) rs.Parameters.Add(upParam);
                            break;
                        case S7Functions.EndUpload:
                            // 只需返回空 AckData 确认
                            _uploadData = null;
                            _uploadOffset = 0;
                            _uploadJobId = 0;
                            break;
                        case S7Functions.StartDownload:
                            OnStartDownload(pm as UploadRawParameter);
                            break;
                        case S7Functions.Download:
                            OnDownloadChunk(pm as UploadRawParameter);
                            break;
                        case S7Functions.EndDownload:
                            OnEndDownload();
                            break;
                        case S7Functions.Setup:
                        default:
                            foreach (var item in msg.Parameters)
                            {
                                rs.Parameters.Add(item);
                            }
                            break;
                    }

                    Send(rs.ToCOTP().ToPacket(true));
                }
                break;
            case S7Kinds.Ack:
                break;
            case S7Kinds.AckData:
                break;
            case S7Kinds.UserData:
                {
                    var rs = new S7Message
                    {
                        Kind = S7Kinds.UserData,
                        Sequence = msg.Sequence,
                    };

                    var udp = msg.Parameters.OfType<UserDataParameter>().FirstOrDefault();
                    if (udp != null)
                    {
                        var rsp = OnUserData(udp);
                        if (rsp != null)
                            rs.Parameters.Add(rsp);
                    }

                    Send(rs.ToCOTP().ToPacket(true));
                }
                break;
            default:
                break;
        }
    }

    ReadResponse? OnRead(ReadRequest? request)
    {
        if (request == null) return null;

        WriteLog("读取：{0}", request.ToJson());

        var rs = new ReadResponse();
        foreach (var item in request.Items)
        {
            try
            {
                var mem = Host.GetMemory(item.Area, item.DbNumber);
                var byteOffset = (Int32)(item.Address >> 3);

                Byte[] data;
                if (item.TransportSize == 1) // BIT
                {
                    // 返回单字节，位值位于 bit0
                    var bitOffset = (Int32)(item.Address & 7);
                    var byteVal = (byteOffset < mem.Length) ? mem[byteOffset] : (Byte)0;
                    data = [(Byte)((byteVal >> bitOffset) & 1)];
                }
                else // BYTE/WORD/DWORD
                {
                    var byteCount = Math.Min((Int32)item.Count, mem.Length - byteOffset);
                    byteCount = Math.Max(byteCount, 0);
                    data = new Byte[byteCount];
                    if (byteOffset >= 0 && byteCount > 0)
                        Array.Copy(mem, byteOffset, data, 0, byteCount);
                }

                rs.Items.Add(new DataItem
                {
                    Code = ReadWriteErrorCode.Success,
                    TransportSize = 0x04,
                    Data = data,
                });
            }
            catch
            {
                rs.Items.Add(new DataItem { Code = ReadWriteErrorCode.AddressOutOfRange });
            }
        }

        return rs;
    }

    #region 块传输处理
    private static UInt32 _nextJobId = 1;

    UploadRawParameter? OnStartUpload(UploadRawParameter? req)
    {
        if (req == null) return null;

        // 解析请求：paramBytes[3]=fileId, [4]=blockType, [5..9]=blockNumAscii(5位), [10]=destFS
        var raw = req.RawBytes;
        if (raw.Length < 10) return null;

        var blockTypeByte = raw[4];
        var blockNumStr = System.Text.Encoding.ASCII.GetString(raw, 5, 5);
        if (!Int32.TryParse(blockNumStr, out var blockNum)) blockNum = 0;
        var blockType = (S7BlockType)blockTypeByte;

        var data = Host.GetBlock(blockType, blockNum);
        data ??= [];
        _uploadData = data;
        _uploadOffset = 0;
        _uploadJobId = _nextJobId++;

        WriteLog("StartUpload: {0}{1} size={2} jobId=0x{3:X8}", blockType, blockNum, data.Length, _uploadJobId);

        // 响应 RawBytes: [0]=0x00, [1..4]=jobId, [5..6]=dataLen
        var rsp = new Byte[7];
        rsp[0] = 0x00;
        rsp[1] = (Byte)(_uploadJobId >> 24); rsp[2] = (Byte)(_uploadJobId >> 16);
        rsp[3] = (Byte)(_uploadJobId >> 8);  rsp[4] = (Byte)_uploadJobId;
        rsp[5] = (Byte)(data.Length >> 8);   rsp[6] = (Byte)(data.Length & 0xFF);
        return new UploadRawParameter(S7Functions.StartUpload, rsp);
    }

    UploadRawParameter? OnUpload(UploadRawParameter? req)
    {
        if (req == null || _uploadData == null) return null;

        // 每帧最大 PDU - 开销（使用固定 220 字节避免 PDU 协商依赖）
        const Int32 ChunkSize = 220;
        var remaining = _uploadData.Length - _uploadOffset;
        var toSend = Math.Min(ChunkSize, remaining);
        var moreData = (_uploadOffset + toSend) < _uploadData.Length;

        var chunk = new Byte[toSend];
        if (toSend > 0)
            Array.Copy(_uploadData, _uploadOffset, chunk, 0, toSend);
        _uploadOffset += toSend;

        // 响应 RawBytes: [0]=0x00, [1]=moreData(0x01/0x00), [2..3]=dataLen, [4+]=data
        var rsp = new Byte[4 + toSend];
        rsp[0] = 0x00;
        rsp[1] = moreData ? (Byte)0x01 : (Byte)0x00;
        rsp[2] = (Byte)(toSend >> 8); rsp[3] = (Byte)(toSend & 0xFF);
        if (toSend > 0)
            Array.Copy(chunk, 0, rsp, 4, toSend);

        WriteLog("Upload 数据块 size={0} moreData={1}", toSend, moreData);
        return new UploadRawParameter(S7Functions.Upload, rsp);
    }

    void OnStartDownload(UploadRawParameter? req)
    {
        if (req == null) return;

        var raw = req.RawBytes;
        if (raw.Length < 10) return;

        var blockTypeByte = raw[4];
        var blockNumStr = System.Text.Encoding.ASCII.GetString(raw, 5, 5);
        if (!Int32.TryParse(blockNumStr, out var blockNum)) blockNum = 0;

        _downloadType = (S7BlockType)blockTypeByte;
        _downloadNumber = blockNum;
        _downloadBuffer.Clear();
        WriteLog("StartDownload: {0}{1}", _downloadType, _downloadNumber);
    }

    void OnDownloadChunk(UploadRawParameter? req)
    {
        if (req == null) return;
        // 客户端 Download 帧 RawBytes: [0]=moreData, [1]=reserved, [2]=len_hi, [3+]=data
        var raw = req.RawBytes;
        if (raw.Length <= 3) return;
        var dataStart = 3;
        var dataLen = raw.Length - dataStart;
        for (var i = 0; i < dataLen; i++)
            _downloadBuffer.Add(raw[dataStart + i]);
        WriteLog("Download 数据块 size={0} bufTotal={1}", dataLen, _downloadBuffer.Count);
    }

    void OnEndDownload()
    {
        var data = _downloadBuffer.ToArray();
        Host.SetBlock(_downloadType, _downloadNumber, data);
        WriteLog("EndDownload: {0}{1} 已保存 {2} 字节", _downloadType, _downloadNumber, data.Length);
        _downloadBuffer.Clear();
    }
    #endregion

    WriteResponse? OnWrite(WriteRequest? request)
    {
        if (request == null) return null;

        WriteLog("写入：{0}", request.ToJson());

        var rs = new WriteResponse();
        for (var i = 0; i < request.Items.Count; i++)
        {
            try
            {
                var item = request.Items[i];
                var dataItem = i < request.DataItems.Count ? request.DataItems[i] : null;

                if (dataItem?.Data != null && dataItem.Data.Length > 0)
                {
                    var mem = Host.GetMemory(item.Area, item.DbNumber);
                    var byteOffset = (Int32)(item.Address >> 3);

                    if (item.TransportSize == 1) // BIT
                    {
                        var bitOffset = (Int32)(item.Address & 7);
                        var bitMask = (Byte)(1 << bitOffset);
                        if (dataItem.Data[0] != 0)
                            mem[byteOffset] |= bitMask;
                        else
                            mem[byteOffset] &= (Byte)~bitMask;
                    }
                    else
                    {
                        var maxWrite = Math.Min(dataItem.Data.Length, mem.Length - byteOffset);
                        if (byteOffset >= 0 && maxWrite > 0)
                            Array.Copy(dataItem.Data, 0, mem, byteOffset, maxWrite);

                        WriteLog("写入区域={0} DB={1} 偏移={2} 字节={3}", item.Area, item.DbNumber, byteOffset, dataItem.Data.ToHex());
                    }
                }

                rs.Items.Add(new DataItem { Code = ReadWriteErrorCode.Success });
            }
            catch
            {
                rs.Items.Add(new DataItem { Code = ReadWriteErrorCode.AddressOutOfRange });
            }
        }

        return rs;
    }

    UserDataParameter? OnUserData(UserDataParameter request)
    {
        // Function=0x12 → 时钟功能组
        if (request.Function == 0x12)
        {
            if (request.SubFunction == 0x04) // 读时钟
            {
                WriteLog("读取PLC时钟");
                var now = Host.SimulatedClock ?? DateTime.Now;
                return new UserDataParameter
                {
                    ExtraByte = 0x01,
                    Function = 0x82,    // 时钟响应
                    SubFunction = 0x04,
                    ReturnCode = 0xFF,
                    TransportSize = 0x09,
                    Data = S7DateTimeHelper.Encode(now),
                };
            }
            if (request.SubFunction == 0x02) // 写时钟
            {
                if (request.Data?.Length >= 8)
                {
                    var dt = S7DateTimeHelper.Decode(request.Data);
                    Host.SimulatedClock = dt;
                    WriteLog("写入PLC时钟：{0}", dt);
                }
                return new UserDataParameter
                {
                    ExtraByte = 0x01,
                    Function = 0x82,
                    SubFunction = 0x02,
                    ReturnCode = 0xFF,
                    TransportSize = 0x09,
                    Data = [],
                };
            }
        }
        // Function=0x11 → SZL功能组
        else if (request.Function == 0x11)
        {
            if (request.SubFunction == 0x0E && request.Data?.Length >= 4) // 读SZL记录
            {
                var szlId = (UInt16)((request.Data[0] << 8) | request.Data[1]);
                var szlIdx = (UInt16)((request.Data[2] << 8) | request.Data[3]);
                WriteLog("读取SZL：ID=0x{0:X4} Index=0x{1:X4}", szlId, szlIdx);

                var szlData = Host.GetSzlData(szlId, szlIdx);
                return new UserDataParameter
                {
                    ExtraByte = 0x01,
                    Function = 0x81,    // SZL响应
                    SubFunction = 0x0E,
                    ReturnCode = 0xFF,
                    TransportSize = 0x09,
                    Data = szlData,
                };
            }
        }

        // Function=0x00 → CPU 功能组（密码认证）
        else if (request.Function == 0x00)
        {
            if (request.SubFunction == 0x01) // SetPassword
            {
                var pwd = request.Data is { Length: >= 8 }
                    ? System.Text.Encoding.ASCII.GetString(request.Data, 0, 8).TrimEnd()
                    : null;
                WriteLog("SetPassword: 收到密码认证（{0}）", pwd ?? "(空)");

                // 验证密码
                var expected = Host.SimulatedPassword?.TrimEnd();
                var success = Host.ProtectionLevel == ProtectionLevel.NoProtection
                    || String.Equals(pwd, expected, StringComparison.Ordinal);

                var rspData = new Byte[4];
                rspData[0] = success ? (Byte)ProtectionLevel.NoProtection : (Byte)Host.ProtectionLevel;
                rspData[1] = 0x00;
                rspData[2] = 0x00;
                rspData[3] = 0x00;

                WriteLog("SetPassword: 认证{0}，返回级别={1}", success ? "成功" : "失败", (ProtectionLevel)rspData[0]);

                return new UserDataParameter
                {
                    ExtraByte = 0x01,
                    Function = 0x80,    // CPU 功能组响应
                    SubFunction = 0x01,
                    ReturnCode = 0xFF,
                    TransportSize = 0x09,
                    Data = rspData,
                };
            }
            if (request.SubFunction == 0x02) // ClearPassword
            {
                WriteLog("ClearPassword: 清除密码认证");
                return new UserDataParameter
                {
                    ExtraByte = 0x01,
                    Function = 0x80,
                    SubFunction = 0x02,
                    ReturnCode = 0xFF,
                    TransportSize = 0x09,
                    Data = [],
                };
            }
        }

        return null;
    }
}