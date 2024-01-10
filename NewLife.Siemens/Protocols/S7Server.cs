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

    protected override void OnConnected()
    {
        WriteLog("S7连接：{0}", Remote);

        base.OnConnected();
    }

    protected override void OnDisconnected()
    {
        WriteLog("S7断开：{0}", Remote);

        base.OnDisconnected();
    }

    protected override void OnError(Object sender, ExceptionEventArgs e)
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

        XTrace.WriteLine("读取：{0}", request.ToJson());

        var di = new DataItem
        {
            Code = ReadWriteErrorCode.Success,
            Type = VarType.DWord,
            Data = Rand.Next().GetBytes(false)
        };

        var rs = new ReadResponse();
        rs.Items.Add(di);

        return rs;
    }

    WriteResponse? OnWrite(WriteRequest? request)
    {
        if (request == null) return null;

        XTrace.WriteLine("写入：{0}", request.ToJson());

        var di = new DataItem
        {
            Code = ReadWriteErrorCode.Success,
        };

        var rs = new WriteResponse();
        rs.Items.Add(di);

        return rs;
    }
}