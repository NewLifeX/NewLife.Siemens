using System;
using NewLife.Log;
using NewLife.Net;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Siemens.Messages;
using NewLife.Siemens.Models;

namespace NewLife.Siemens.Protocols;

/// <summary>S7服务端。用于仿真</summary>
public class S7Server : NetServer<S7Session>
{
    /// <summary>实例化</summary>
    public S7Server()
    {
        Port = 102;
        ProtocolType = NetType.Tcp;

        Add(new TPKTCodec());
    }
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

        var num = Rand.Next(0, 10000);
        WriteLog("数值：{0}", num);

        var di = new DataItem
        {
            Code = ReadWriteErrorCode.Success,
            Type = VarType.DWord,
            Data = num.GetBytes(false)
        };

        var rs = new ReadResponse();
        rs.Items.Add(di);

        return rs;
    }

    WriteResponse? OnWrite(WriteRequest? request)
    {
        if (request == null) return null;

        WriteLog("写入：{0}", request.ToJson());

        var ri = request.DataItems.FirstOrDefault();
        if (ri != null && ri.Data != null)
        {
            Object? num;
            if (ri.Data.Length == 2 && ri.Type is VarType.Word or VarType.Int or VarType.Bit or VarType.Byte or VarType.DWord or VarType.DInt)
                num = ri.Data.ToUInt16(0, false);
            else if (ri.Data.Length == 1 || ri.Type is VarType.Bit or VarType.Byte)
                num = ri.Data[0];
            else if (ri.Data.Length == 2 && ri.Type is VarType.Word or VarType.Int)
                num = ri.Data.ToUInt16(0, false);
            else if (ri.Data.Length == 4 && ri.Type is VarType.DWord or VarType.DInt)
                num = ri.Data.ToUInt32(0, false);
            else if (ri.Data.Length == 4 && ri.Type is VarType.Real)
                num = Convert.ToSingle(ri.Data);
            else if (ri.Data.Length == 4 && ri.Type is VarType.LReal)
                num = Convert.ToDouble(ri.Data);
            else if (ri.Type is VarType.String or VarType.S7String or VarType.S7WString)
                num = ri.Data.ToStr();
            else
                num = ri.Data.ToHex();

            WriteLog("数值：{0}", num);
        }

        var di = new DataItem
        {
            Code = ReadWriteErrorCode.Success,
        };

        var rs = new WriteResponse();
        rs.Items.Add(di);

        return rs;
    }
}