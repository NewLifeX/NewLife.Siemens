using NewLife.Data;
using NewLife.Siemens.Common;

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
    /// <summary>版本</summary>
    public Byte Version { get; set; }

    /// <summary>保留</summary>
    public Byte Reserved { get; set; }

    /// <summary>长度</summary>
    public UInt16 Length { get; set; }

    /// <summary>数据</summary>
    public Packet Data { get; set; }

    /// <summary>实例化</summary>
    public TPKT() { }

    private TPKT(Byte version, Byte reserved1, Int32 length, Byte[] data)
    {
        Version = version;
        Reserved = reserved1;
        Length = (UInt16)length;
        Data = data;
    }

    /// <summary>解析数据</summary>
    /// <param name="pk"></param>
    public void Read(Packet pk)
    {
        var buf = pk.ReadBytes(0, 4);
        Version = buf[0];
        Reserved = buf[1];
        Length = buf.ToUInt16(2, false);

        Data = pk.Slice(4, Length);
    }

    /// <summary>
    /// Reads a TPKT from the socket Async
    /// </summary>
    /// <param name="stream">The stream to read from</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Task TPKT Instace</returns>
    public static async Task<TPKT> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        // 读取4字节头部
        var buf = new Byte[4];
        var len = await stream.ReadExactAsync(buf, 0, 4, cancellationToken).ConfigureAwait(false);
        if (len < 4) throw new TPKTInvalidException("TPKT is incomplete / invalid");

        var version = buf[0];
        var reserved1 = buf[1];
        var length = buf[2] * 256 + buf[3]; // 大端字节序

        // 根据长度读取数据
        var data = new Byte[length - 4];
        len = await stream.ReadExactAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
        if (len < data.Length)
            throw new TPKTInvalidException("TPKT payload incomplete / invalid");

        return new TPKT
        (
            version: version,
            reserved1: reserved1,
            length: length,
            data: data
        );
    }
}