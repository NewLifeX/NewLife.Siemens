using NewLife.Data;

namespace NewLife.Siemens.Protocols;

/// <summary>ISO Transport Service ontop of the TCP</summary>
/// <remarks>
/// 通过TCP的传输服务。介于TCP和COTP之间。属于传输服务类的协议，它为上层的COTP和下层TCP进行了过渡。
/// 功能为在COTP和TCP之间建立桥梁，其内容包含了上层协议数据包的长度。
/// 一般与COTP一起发送，当作Header段。
/// 我们常用的RDP协议（remote desktop protocol，windows的远程桌面协议）也是基于TPKT的，TPKT的默认TCP端口为102（RDP为3389）
/// </remarks>
public class TPKT
{
    #region 属性
    /// <summary>版本</summary>
    public Byte Version { get; set; } = 3;

    /// <summary>保留</summary>
    public Byte Reserved { get; set; }

    /// <summary>长度。包括当前TPKT头和后续数据</summary>
    public UInt16 Length { get; set; }

    /// <summary>数据</summary>
    public Packet? Data { get; set; }
    #endregion

    #region 读写
    /// <summary>解析头部数据</summary>
    /// <param name="buf"></param>
    public TPKT ReadHeader(Byte[] buf)
    {
        Version = buf[0];
        Reserved = buf[1];
        Length = buf.ToUInt16(2, false);

        return this;
    }

    /// <summary>序列化头部数据</summary>
    /// <param name="buf"></param>
    public void WriteHeader(Byte[] buf)
    {
        buf[0] = Version;
        buf[1] = Reserved;
        buf[2] = (Byte)(Length >> 8);
        buf[3] = (Byte)(Length & 0xFF);
    }

    /// <summary>解析头部以及负载数据</summary>
    /// <param name="pk"></param>
    public TPKT Read(Packet pk)
    {
        var buf = pk.ReadBytes(0, 4);
        ReadHeader(buf);

        if (pk.Total > 4) Data = pk.Slice(4, Length - 4);

        return this;
    }

    /// <summary>序列化消息，包括头部和负载数据</summary>
    /// <returns></returns>
    public Packet ToPacket()
    {
        // 根据数据长度计算总长度
        Length = (UInt16)(4 + Data?.Total ?? 0);

        var buf = new Byte[4];
        WriteHeader(buf);

        var pk = new Packet(buf);
        if (Data != null) pk.Append(Data);

        return pk;
    }
    #endregion

    /// <summary>读取TPKT数据</summary>
    /// <param name="stream"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<Byte[]> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        // 读取4字节头部
        var buf = new Byte[4];
        var len = await stream.ReadAsync(buf, 0, 4, cancellationToken).ConfigureAwait(false);
        if (len < 4) throw new InvalidDataException("TPKT is incomplete / invalid");

        var tpkt = new TPKT();
        tpkt.ReadHeader(buf);

        // 根据长度读取数据
        var data = new Byte[tpkt.Length - 4];
        len = await stream.ReadAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
        if (len < data.Length)
            throw new InvalidDataException("TPKT payload incomplete / invalid");

        return data;
    }
}