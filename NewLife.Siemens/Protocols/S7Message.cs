﻿using NewLife.Data;
using NewLife.Serialization;

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

    /// <summary>参数集合</summary>
    public IList<S7Parameter> Parameters { get; set; } = [];

    /// <summary>数据</summary>
    public Packet Data { get; set; }
    #endregion

    #region 读写
    /// <summary>读取</summary>
    /// <param name="pk"></param>
    /// <returns></returns>
    public Boolean Read(Packet pk) => Read(null, pk);

    /// <summary>读取</summary>
    /// <param name="stream"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public Boolean Read(Stream stream, Object context)
    {
        var pk = context as Packet;
        stream ??= pk?.GetStream();
        var reader = new Binary { Stream = stream, IsLittleEndian = false };

        ProtocolId = reader.ReadByte();
        Kind = (S7Kinds)reader.ReadByte();
        Reserved = reader.ReadUInt16();
        Sequence = reader.ReadUInt16();

        // 参数长度和数据长度
        var plen = reader.ReadUInt16();
        var dlen = reader.ReadUInt16();

        // 读取参数
        if (plen > 0)
        {
            var buf = reader.ReadBytes(plen);
            ReadParameter(buf);
        }

        // 读取数据
        if (dlen > 0)
        {
            Data = reader.ReadBytes(dlen);
        }

        return true;
    }

    void ReadParameter(Byte[] buf)
    {
        var ms = new MemoryStream(buf);
        var reader = new Binary { Stream = ms };

        while (ms.Position < ms.Length)
        {
            var kind = (S7Functions)reader.ReadByte();
            ms.Seek(-1, SeekOrigin.Current);
            switch (kind)
            {
                case S7Functions.Setup:
                    var pm = new S7SetupParameter();
                    if (pm.Read(null, reader))
                        Parameters.Add(pm);
                    break;
                default:
                    throw new NotSupportedException($"不支持的S7参数类型[{kind}]");
            }
        }
    }

    /// <summary>写入</summary>
    /// <param name="stream"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public Boolean Write(Stream stream, Object context)
    {
        var pk = context as Packet;
        stream ??= pk?.GetStream();
        var writer = new Binary { Stream = stream, IsLittleEndian = false };

        writer.WriteByte(ProtocolId);
        writer.WriteByte((Byte)Kind);
        writer.WriteUInt16(Reserved);
        writer.WriteUInt16(Sequence);

        var ps = SaveParameters(Parameters);
        var dt = Data;
        var plen = ps?.Length ?? 0;
        var dlen = dt?.Total ?? 0;

        writer.WriteUInt16((UInt16)plen);
        writer.WriteUInt16((UInt16)dlen);

        if (ps != null && ps.Length > 0) writer.Write(ps, 0, ps.Length);

        if (dt != null && dt.Total > 0) writer.Write(dt);

        return true;
    }

    Byte[] SaveParameters(IList<S7Parameter> ps)
    {
        if (ps == null || ps.Count == 0) return null;

        var writer = new Binary { IsLittleEndian = false };
        foreach (var pm in ps)
        {
            pm.Write(null, writer);
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
    public S7Parameter GetParameter(S7Functions code) => Parameters?.FirstOrDefault(e => e.Code == code);

    /// <summary>设置参数</summary>
    /// <param name="parameter"></param>
    public void SetParameter(S7Parameter parameter)
    {
        for (var i = 0; i < Parameters.Count; i++)
        {
            var pm2 = Parameters[i];
            if (pm2.Code == parameter.Code)
            {
                Parameters[i] = parameter;
                return;
            }
        }

        Parameters.Add(parameter);
    }

    /// <summary>设置参数</summary>
    /// <param name="amq"></param>
    /// <param name="pdu"></param>
    public void Setup(UInt16 amq, UInt16 pdu)
    {
        SetParameter(new S7SetupParameter
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
