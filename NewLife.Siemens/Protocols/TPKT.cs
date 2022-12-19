using NewLife.Siemens.Common;

namespace NewLife.Siemens.Protocols;

internal class TPKT
{
    public Byte Version;
    public Byte Reserved1;
    public Int32 Length;
    public Byte[] Data;
    private TPKT(Byte version, Byte reserved1, Int32 length, Byte[] data)
    {
        Version = version;
        Reserved1 = reserved1;
        Length = length;
        Data = data;
    }

    /// <summary>
    /// Reads a TPKT from the socket Async
    /// </summary>
    /// <param name="stream">The stream to read from</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Task TPKT Instace</returns>
    public static async Task<TPKT> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buf = new Byte[4];
        var len = await stream.ReadExactAsync(buf, 0, 4, cancellationToken).ConfigureAwait(false);
        if (len < 4) throw new TPKTInvalidException("TPKT is incomplete / invalid");

        var version = buf[0];
        var reserved1 = buf[1];
        var length = buf[2] * 256 + buf[3]; //BigEndian

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