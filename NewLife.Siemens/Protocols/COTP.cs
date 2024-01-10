using NewLife.Data;
using NewLife.Serialization;

namespace NewLife.Siemens.Protocols;

/// <summary>面向连接的传输协议(Connection-Oriented Transport Protocol)</summary>
/// <remarks>
/// 文档：https://tools.ietf.org/html/rfc905
/// </remarks>
public class COTP
{
    #region 属性
    /// <summary>头部长度。不包含长度字段所在字节</summary>
    public Byte Length { get; set; }

    /// <summary>包类型。常用CR连接、CC确认、DT数据</summary>
    public PduType Type { get; set; }
    #endregion

    #region CR/CC包
    /// <summary>目标的引用，可以认为是用来唯一标识目标</summary>
    public UInt16 Destination { get; set; }

    /// <summary>源的引用</summary>
    public UInt16 Source { get; set; }

    /// <summary>选项</summary>
    public Byte Option { get; set; }

    /// <summary>参数集合。一般CR/CC带有参数，交换Tsap和Tpdu大小</summary>
    public IList<COTPParameter> Parameters { get; set; } = [];
    #endregion

    #region DT包
    /// <summary>编码。仅存在于DT包</summary>
    public Int32 Number { get; set; }

    /// <summary>是否最后数据单元。仅存在于DT包</summary>
    public Boolean LastDataUnit { get; set; }

    /// <summary>数据。仅存在于DT包</summary>
    public Packet? Data { get; set; }
    #endregion

    #region 读写
    /// <summary>解析数据</summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public Boolean Read(Packet data) => Read(data.GetStream());

    /// <summary>读取解析数据</summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public Boolean Read(Stream stream)
    {
        var reader = new Binary { Stream = stream, IsLittleEndian = false };

        // 头部长度。当前字节之后就是头部，然后是数据。CR/CC一般为17字节，DT一般为2字节
        Length = reader.ReadByte();
        Type = (PduType)reader.ReadByte();

        // 解析不同数据帧
        switch (Type)
        {
            case PduType.Data:
                {
                    var flags = reader.ReadByte();
                    Number = flags & 0x7F;
                    LastDataUnit = (flags & 0x80) > 0;

                    Data = stream.ReadBytes(-1);
                }
                break;
            case PduType.ConnectionRequest:
            case PduType.ConnectionConfirmed:
            default:
                {
                    Destination = reader.ReadUInt16();
                    Source = reader.ReadUInt16();
                    Option = reader.ReadByte();
                    Parameters = ReadParameters(reader);
                }
                break;
        }

        return true;
    }

    IList<COTPParameter> ReadParameters(Binary reader)
    {
        var list = new List<COTPParameter>();
        while (reader.CheckRemain(1 + 1))
        {
            var tlv = new COTPParameter((COTPParameterKinds)reader.ReadByte(), reader.ReadByte(), null);

            var buf = reader.ReadBytes(tlv.Length);
            tlv.Value = tlv.Length switch
            {
                1 => buf[0],
                2 => buf.ToUInt16(0, false),
                4 => buf.ToUInt32(0, false),
                _ => buf,
            };
            list.Add(tlv);
        }

        return list;
    }

    /// <summary>序列化写入数据</summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public Boolean Write(Stream stream)
    {
        var writer = new Binary { Stream = stream, IsLittleEndian = false };

        switch (Type)
        {
            case PduType.Data:
                stream.WriteByte((Byte)(1 + 1));
                stream.WriteByte((Byte)Type);

                var flags = (Byte)(Number & 0x7F);
                if (LastDataUnit) flags |= 0x80;
                stream.WriteByte(flags);

                Data?.CopyTo(stream);
                break;
            case PduType.ConnectionRequest:
            case PduType.ConnectionConfirmed:
            default:
                // 计算长度。再次之前需要先计算参数长度
                var ps = Parameters;
                var len = 1 + 2 + 2 + 1;
                if (ps != null)
                {
                    FixParameters(ps);
                    foreach (var item in ps)
                    {
                        len += 1 + 1 + item.Length;
                    }
                }
                writer.WriteByte((Byte)len);
                writer.WriteByte((Byte)Type);

                writer.Write(Destination);
                writer.Write(Source);
                writer.WriteByte(Option);

                if (ps != null) WriteParameters(writer, ps);
                break;
        }

        return true;
    }

    void FixParameters(IList<COTPParameter> parameters)
    {
        foreach (var item in parameters)
        {
            item.Length = item.Kind switch
            {
                COTPParameterKinds.TpduSize => 1,
                COTPParameterKinds.SrcTsap => 2,
                COTPParameterKinds.DstTsap => 2,
                _ => item.Value switch
                {
                    Byte => 1,
                    UInt16 or Int16 => 2,
                    UInt32 or Int32 => 4,
                    Byte[] buf => (Byte)buf.Length,
                    _ => throw new NotSupportedException(),
                },
            };
        }
    }

    void WriteParameters(Binary writer, IList<COTPParameter> parameters)
    {
        foreach (var item in parameters)
        {
            writer.WriteByte((Byte)item.Kind);
            writer.WriteByte(item.Length);

            if (item.Value is Byte b)
                writer.WriteByte(b);
            else if (item.Value is UInt16 u16)
                writer.Write(u16);
            else if (item.Value is UInt32 u32)
                writer.Write(u32);
            else if (item.Value is Byte[] buf)
                writer.Write(buf);
            else
                throw new NotSupportedException();
        }
    }

    /// <summary>序列化消息</summary>
    /// <param name="withTPKT">是否带TPKT头</param>
    /// <returns></returns>
    public Packet ToPacket(Boolean withTPKT = true)
    {
        var ms = new MemoryStream();
        Write(ms);

        ms.Position = 0;
        var pk = new Packet(ms);
        if (withTPKT) return new TPKT { Data = pk }.ToPacket();

        return pk;
    }
    #endregion

    #region 参数
    /// <summary>获取参数</summary>
    /// <param name="kind"></param>
    /// <returns></returns>
    public COTPParameter? GetParameter(COTPParameterKinds kind) => Parameters?.FirstOrDefault(e => e.Kind == kind);

    /// <summary>设置参数</summary>
    /// <param name="parameter"></param>
    public void SetParameter(COTPParameter parameter)
    {
        var ps = Parameters;
        for (var i = 0; i < ps.Count; i++)
        {
            var pm2 = ps[i];
            if (pm2.Kind == parameter.Kind)
            {
                ps[i] = parameter;
                return;
            }
        }

        ps.Add(parameter);
    }

    /// <summary>设置参数</summary>
    /// <param name="kind"></param>
    /// <param name="value"></param>
    public void SetParameter(COTPParameterKinds kind, Byte value) => SetParameter(new(kind, 1, value));

    /// <summary>设置参数</summary>
    /// <param name="kind"></param>
    /// <param name="value"></param>
    public void SetParameter(COTPParameterKinds kind, UInt16 value) => SetParameter(new(kind, 2, value));

    /// <summary>设置参数</summary>
    /// <param name="kind"></param>
    /// <param name="value"></param>
    public void SetParameter(COTPParameterKinds kind, UInt32 value) => SetParameter(new(kind, 4, value));
    #endregion

    #region 方法
    /// <summary>从网络流读取一个COTP帧</summary>
    /// <param name="stream">网络流</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<COTP> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var data = await TPKT.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
        if (data.Length == 0) throw new InvalidDataException("No protocol data received");

        var cotp = new COTP();
        if (!cotp.Read(data)) throw new InvalidDataException("Invalid protocol data received");

        return cotp;
    }

    /// <summary>从网络流读取多个帧，直到最后一个数据单元</summary>
    /// <param name="stream">网络流</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<Packet?> ReadAllAsync(Stream stream, CancellationToken cancellationToken)
    {
        Packet? rs = null;
        while (true)
        {
            var cotp = await ReadAsync(stream, cancellationToken).ConfigureAwait(false);
            if (rs == null)
                rs = cotp.Data;
            else if (cotp.Data != null)
                rs.Append(cotp.Data);

            if (cotp.LastDataUnit) break;
        }

        return rs;
    }

    /// <summary>已重载</summary>
    /// <returns></returns>
    public override String ToString() => Type == PduType.Data ? $"[{Type}] Data[{Data?.Total}]" : $"[{Type}]";
    #endregion
}