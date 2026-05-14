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
                //case PduType.ConnectionConfirmed:
                //    break;
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
}