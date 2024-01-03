using System.IO;
using NewLife.Data;
using NewLife.Serialization;
using NewLife.Siemens.Common;

namespace NewLife.Siemens.Protocols;

/// <summary>面向连接的传输协议(Connection-Oriented Transport Protocol)</summary>
/// <remarks>
/// 文档：https://tools.ietf.org/html/rfc905
/// </remarks>
public class COTP : IAccessor
{
    #region 属性
    /// <summary>包类型</summary>
    public PduType Type { get; set; }

    /// <summary>目标的引用，可以认为是用来唯一标识目标</summary>
    public UInt16 Destination { get; set; }

    /// <summary>源的引用</summary>
    public UInt16 Source { get; set; }

    /// <summary>选项</summary>
    public Byte Option { get; set; }

    /// <summary>参数集合</summary>
    public IList<COTPParameter> Parameters { get; set; } = [];

    /// <summary>编码</summary>
    public Int32 Number { get; set; }

    /// <summary>是否最后数据单元</summary>
    public Boolean LastDataUnit { get; set; }

    /// <summary>数据</summary>
    public Packet Data { get; set; }
    #endregion

    #region 读写
    /// <summary>解析数据</summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public Boolean Read(Packet data) => (this as IAccessor).Read(null, data);

    /// <summary>序列化</summary>
    /// <returns></returns>
    public Byte[] ToArray()
    {
        var ms = new MemoryStream();
        (this as IAccessor).Write(ms, null);

        return ms.ToArray();
    }

    /// <summary>读取解析数据</summary>
    /// <param name="stream"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public Boolean Read(Stream stream, Object context)
    {
        var pk = context as Packet;
        stream ??= pk?.GetStream();
        var reader = new Binary { Stream = stream, IsLittleEndian = false };

        // 头部长度。当前字节之后就是头部，然后是数据，一般为17 bytes
        var len = reader.ReadByte();
        Type = (PduType)reader.ReadByte();

        // 解析不同数据帧
        switch (Type)
        {
            case PduType.Data:
                {
                    var flags = reader.ReadByte();
                    Number = flags & 0x7F;
                    LastDataUnit = (flags & 0x80) > 0;

                    if (pk != null)
                        Data = pk.Slice(4, pk.Total - 1 - len);
                    else
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
        var stream = reader.Stream;
        var list = new List<COTPParameter>();
        while (stream.Position + 3 <= stream.Length)
        {
            var tlv = new COTPParameter
            {
                Kind = (COTPParameterKinds)reader.ReadByte(),
                Length = reader.ReadByte()
            };
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
    /// <param name="context"></param>
    /// <returns></returns>
    public Boolean Write(Stream stream, Object context)
    {
        var pk = context as Packet;
        stream ??= pk?.GetStream();
        var writer = new Binary { Stream = stream, IsLittleEndian = false };

        switch (Type)
        {
            case PduType.Data:
                {
                    var len = 2;
                    stream.WriteByte((Byte)len);
                    stream.WriteByte((Byte)Type);

                    var flags = (Byte)(Number & 0x7F);
                    if (LastDataUnit) flags |= 0x80;
                    stream.WriteByte(flags);

                    Data?.CopyTo(stream);
                }
                break;
            case PduType.ConnectionRequest:
            case PduType.ConnectionConfirmed:
            default:
                {
                    var len = 2;
                    writer.WriteByte((Byte)len);
                    writer.WriteByte((Byte)Type);

                    writer.Write(Destination);
                    writer.Write(Source);
                    writer.WriteByte(Option);

                    var ps = Parameters;
                    if (ps != null) WriteParameters(writer, ps);
                }
                break;
        }

        return true;
    }

    void WriteParameters(Binary writer, IList<COTPParameter> parameters)
    {
        foreach (var item in parameters)
        {
            writer.WriteByte((Byte)item.Kind);

            if (item.Value is Byte b)
            {
                writer.WriteByte(1);
                writer.WriteByte(b);
            }
            else if (item.Value is UInt16 u16)
            {
                writer.WriteByte(2);
                writer.Write(u16);
            }
            else if (item.Value is UInt32 u32)
            {
                writer.WriteByte(4);
                writer.Write(u32);
            }
            else if (item.Value is Byte[] buf)
            {
                writer.WriteByte((Byte)buf.Length);
                writer.Write(buf);
            }
            else
                throw new NotSupportedException();
        }
    }

    /// <summary>带TPKT头的写入</summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public Boolean WriteWithTPKT(Stream stream)
    {
        var tpkt = new TPKT { Version = 3, };

        // 先写COTP，得到长度后再回过头写TPKT
        var ms = new MemoryStream();
        if (!Write(ms, null)) return false;

        tpkt.Length = (UInt16)ms.Length;
        tpkt.Write(stream);

        ms.Position = 0;
        ms.CopyTo(stream);

        return true;
    }

    /// <summary>获取内容</summary>
    /// <param name="withTPKT"></param>
    /// <returns></returns>
    public Packet GetBytes(Boolean withTPKT = true)
    {
        var ms = new MemoryStream();
        if (!Write(ms, null)) return null;

        ms.Position = 0;
        var pk = new Packet(ms);
        if (!withTPKT) return pk;

        var tpkt = new TPKT { Version = 3, };
        tpkt.Length = (UInt16)ms.Length;

        var rs = new Packet(tpkt.ToArray());
        rs.Append(pk);

        return rs;
    }
    #endregion

    #region 方法
    /// <summary>从网络流读取一个COTP帧</summary>
    /// <param name="stream">网络流</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<COTP> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var tpkt = await TPKT.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
        if (tpkt.Length == 0) throw new TPDUInvalidException("No protocol data received");

        var cotp = new COTP();
        if (!cotp.Read(tpkt.Data)) throw new TPDUInvalidException("Invalid protocol data received");

        return cotp;
    }

    /// <summary>从网络流读取多个帧，直到最后一个数据单元</summary>
    /// <param name="stream">网络流</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<Packet> ReadAllAsync(Stream stream, CancellationToken cancellationToken)
    {
        var cotp = await ReadAsync(stream, cancellationToken).ConfigureAwait(false);
        if (cotp.LastDataUnit) return cotp.Data;

        while (!cotp.LastDataUnit)
        {
            var seg = await ReadAsync(stream, cancellationToken).ConfigureAwait(false);
            if (seg != null && seg.Data != null) cotp.Data.Append(seg.Data);

            cotp = seg;
        }

        return cotp.Data;
    }

    /// <summary>已重载</summary>
    /// <returns></returns>
    public override String ToString() => $"[{Type}] TPDUNumber: {Number} Last: {LastDataUnit} Data[{Data?.Total}]";
    #endregion
}