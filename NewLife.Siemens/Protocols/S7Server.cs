using NewLife.Net;

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

    /// <summary>收到数据时</summary>
    /// <param name="e"></param>
    protected override void OnReceive(ReceivedEventArgs e)
    {
        if (e.Message is not TPKT tpkt || tpkt.Data == null) return;

        var cotp = new COTP();
        if (cotp.Read(tpkt.Data))
        {
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
            LastDataUnit = true,
        };

        var ms = new MemoryStream();
        rs.WriteWithTPKT(ms);
        ms.Position = 0;

        var buf = ms.ToArray();
        Send(ms);

        _logined = true;
    }

    void OnData(COTP cotp)
    {
        var ms = new MemoryStream();
        cotp.WriteWithTPKT(ms);
        ms.Position = 0;

        Send(ms);
    }
}