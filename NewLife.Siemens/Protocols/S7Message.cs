using NewLife.Data;
using NewLife.Serialization;
using NewLife.Siemens.Messages;

namespace NewLife.Siemens.Protocols;

/// <summary>S7协议报文</summary>
public class S7Message : IAccessor
{
    #region 属性
    /// <summary>协议常量，始终设置为0x32</summary>
    public Byte ProtocolId { get; set; } = 0x32;

    /// <summary>消息的一般类型（有时称为ROSCTR类型），消息的其余部分在很大程度上取决于Kind和功能代码。</summary>
    public S7Kinds Kind { get; set; }

    /// <summary>保留数据</summary>
    public UInt16 Reserved { get; set; }

    /// <summary>序列号。由主站生成，每次新传输递增，用于链接对其请求的响应。小端字节序</summary>
    public UInt16 Sequence { get; set; }

    /// <summary>错误类型。仅出现在AckData</summary>
    public Byte ErrorClass { get; set; }

    /// <summary>错误码。仅出现在AckData</summary>
    public Byte ErrorCode { get; set; }

    /// <summary>参数集合</summary>
    public IList<S7Parameter> Parameters { get; set; } = [];

    ///// <summary>数据</summary>
    //public Packet Data { get; set; }
    #endregion

    #region 构造
    /// <summary>友好显示</summary>
    /// <returns></returns>
    public override String ToString() => $"[{Kind}]<{Sequence}>[{Parameters.Count}] {Parameters.FirstOrDefault()}";
    #endregion

    #region 读写
    /// <summary>读取</summary>
    /// <param name="pk"></param>
    /// <returns></returns>
    public Boolean Read(Packet pk) => Read(pk.GetStream(), pk);

    /// <summary>读取</summary>
    /// <param name="stream"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public Boolean Read(Stream stream, Object? context)
    {
        //var pk = context as Packet;
        //stream ??= pk?.GetStream();
        var reader = new Binary { Stream = stream, IsLittleEndian = false };

        ProtocolId = reader.ReadByte();
        Kind = (S7Kinds)reader.ReadByte();
        Reserved = reader.ReadUInt16();
        Sequence = reader.ReadUInt16();

        // 参数长度和数据长度
        var plen = reader.ReadUInt16();
        var dlen = reader.ReadUInt16();

        // 错误码
        if (Kind == S7Kinds.AckData)
        {
            ErrorClass = reader.ReadByte();
            ErrorCode = reader.ReadByte();
        }

        // 读取参数
        if (plen > 0)
        {
            var buf = reader.ReadBytes(plen);
            ReadParameters(buf);
        }

        // 读取数据
        if (dlen > 0)
        {
            var buf = reader.ReadBytes(dlen);
            ReadParameterItems(buf);
        }

        return true;
    }

    void ReadParameters(Byte[] buf)
    {
        var ms = new MemoryStream(buf);
        var reader = new Binary { Stream = ms, IsLittleEndian = false };

        while (ms.Position < ms.Length)
        {
            var kind = (S7Functions)reader.ReadByte();
            ms.Seek(-1, SeekOrigin.Current);
            switch (kind)
            {
                case S7Functions.Setup:
                    var pm = new SetupMessage();
                    if (pm.Read(reader))
                        Parameters.Add(pm);
                    break;
                case S7Functions.ReadVar:
                    if (Kind == S7Kinds.AckData)
                    {
                        var rv = new ReadResponse();
                        if (rv.Read(reader))
                            Parameters.Add(rv);
                    }
                    else
                    {
                        var rv = new ReadRequest();
                        if (rv.Read(reader))
                            Parameters.Add(rv);
                    }
                    break;
                case S7Functions.WriteVar:
                    if (Kind == S7Kinds.AckData)
                    {
                        var rv = new WriteResponse();
                        if (rv.Read(reader))
                            Parameters.Add(rv);
                    }
                    else
                    {
                        var rv = new WriteRequest();
                        if (rv.Read(reader))
                            Parameters.Add(rv);
                    }
                    break;
                default:
                    throw new NotSupportedException($"不支持的S7参数类型[{kind}]");
            }
        }
    }

    void ReadParameterItems(Byte[] buf)
    {
        var ms = new MemoryStream(buf);
        var reader = new Binary { Stream = ms, IsLittleEndian = false };

        foreach (var pm in Parameters)
        {
            if (pm is IDataItems rr)
                rr.ReadItems(reader);
        }
    }

    /// <summary>写入</summary>
    /// <param name="stream"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public Boolean Write(Stream stream, Object? context)
    {
        //var pk = context as Packet;
        //stream ??= pk?.GetStream();
        var writer = new Binary { Stream = stream, IsLittleEndian = false };

        writer.WriteByte(ProtocolId);
        writer.WriteByte((Byte)Kind);
        writer.WriteUInt16(Reserved);
        writer.WriteUInt16(Sequence);

        var ps = SaveParameters(Parameters);
        var dt = SaveParameterItems(Parameters);
        var plen = ps?.Length ?? 0;
        var dlen = dt?.Length ?? 0;

        writer.WriteUInt16((UInt16)plen);
        writer.WriteUInt16((UInt16)dlen);

        if (Kind == S7Kinds.AckData)
        {
            writer.WriteByte(ErrorClass);
            writer.WriteByte(ErrorCode);
        }

        if (ps != null && ps.Length > 0) writer.Write(ps, 0, ps.Length);
        if (dt != null && dt.Length > 0) writer.Write(dt, 0, dt.Length);

        return true;
    }

    Byte[]? SaveParameters(IList<S7Parameter> ps)
    {
        if (ps == null || ps.Count == 0) return null;

        var writer = new Binary { IsLittleEndian = false };
        foreach (var pm in ps)
        {
            pm.Write(writer);
        }

        return writer.GetBytes();
    }

    Byte[]? SaveParameterItems(IList<S7Parameter> ps)
    {
        if (ps == null || ps.Count == 0) return null;

        var writer = new Binary { IsLittleEndian = false };
        foreach (var pm in ps)
        {
            if (pm is IDataItems rr)
                rr.WriteItems(writer);
        }

        return writer.GetBytes();
    }

    /// <summary>序列化</summary>
    /// <returns></returns>
    public Byte[] GetBytes()
    {
        var ms = new MemoryStream();
        Write(ms, null);

        return ms.ToArray();
    }
    #endregion

    #region 参数
    /// <summary>获取参数</summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public S7Parameter? GetParameter(S7Functions code) => Parameters?.FirstOrDefault(e => e.Code == code);

    /// <summary>设置参数</summary>
    /// <param name="parameter"></param>
    public void SetParameter(S7Parameter parameter)
    {
        var ps = Parameters;
        for (var i = 0; i < ps.Count; i++)
        {
            var pm2 = ps[i];
            if (pm2.Code == parameter.Code)
            {
                ps[i] = parameter;
                return;
            }
        }

        ps.Add(parameter);
    }

    /// <summary>设置参数</summary>
    /// <param name="amq"></param>
    /// <param name="pdu"></param>
    public void Setup(UInt16 amq, UInt16 pdu)
    {
        SetParameter(new SetupMessage
        {
            MaxAmqCaller = amq,
            MaxAmqCallee = amq,
            PduLength = pdu,
        });
    }
    #endregion

    #region 辅助
    /// <summary>序列化为COTP</summary>
    /// <returns></returns>
    public COTP ToCOTP()
    {
        return new COTP
        {
            Type = PduType.Data,
            LastDataUnit = true,
            Data = GetBytes()
        };
    }
    #endregion
}
