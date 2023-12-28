using NewLife.Siemens.Common;

namespace NewLife.Siemens.Protocols;

/// <summary>面向连接的传输协议(Connection-Oriented Transport Protocol)</summary>
public class COTP
{
    /// <summary>PDU类型</summary>
    public enum PduType : Byte
    {
        /// <summary>数据帧</summary>
        Data = 0xf0,

        /// <summary>CR连接请求帧</summary>
        ConnectionRequest = 0xe0,

        /// <summary>CC连接确认帧</summary>
        ConnectionConfirmed = 0xd0
    }

    /// <summary>
    /// Describes a COTP TPDU (Transport protocol data unit)
    /// </summary>
    public class TPDU
    {
        ///// <summary>TPKT头</summary>
        //public TPKT TPkt { get; }

        ///// <summary>头部长度</summary>
        //public Byte HeaderLength { get; set; }

        /// <summary>包类型</summary>
        public PduType PDUType { get; set; }

        /// <summary>编码</summary>
        public Int32 Number { get; set; }

        /// <summary>数据</summary>
        public Byte[] Data { get; set; }

        /// <summary>是否最后数据单元</summary>
        public Boolean LastDataUnit { get; set; }

        /// <summary>实例化</summary>
        /// <param name="tPKT"></param>
        public TPDU(TPKT tPKT)
        {
            //TPkt = tPKT;

            var len = tPKT.Data[0]; // Header length excluding this length byte
            if (len >= 2)
            {
                PDUType = (PduType)tPKT.Data[1];

                // 解析不同数据帧
                if (PDUType == PduType.Data) //DT Data
                {
                    var flags = tPKT.Data[2];
                    Number = flags & 0x7F;
                    LastDataUnit = (flags & 0x80) > 0;
                    Data = new Byte[tPKT.Data.Length - len - 1]; // substract header length byte + header length.
                    Array.Copy(tPKT.Data, len + 1, Data, 0, Data.Length);

                    return;
                }
                //TODO: Handle other PDUTypes
            }
            Data = new Byte[0];
        }

        /// <summary>
        /// Reads COTP TPDU (Transport protocol data unit) from the network stream
        /// See: https://tools.ietf.org/html/rfc905
        /// </summary>
        /// <param name="stream">The socket to read from</param>
        /// <param name="cancellationToken"></param>
        /// <returns>COTP DPDU instance</returns>
        public static async Task<TPDU> ReadAsync(Stream stream, CancellationToken cancellationToken)
        {
            var tpkt = await TPKT.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
            if (tpkt.Length == 0) throw new TPDUInvalidException("No protocol data received");

            return new TPDU(tpkt);
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => $"[{PDUType}] TPDUNumber: {Number} Last: {LastDataUnit} Segment Data: {BitConverter.ToString(Data)}";
    }

    /// <summary>
    /// Describes a COTP TSDU (Transport service data unit). One TSDU consist of 1 ore more TPDUs
    /// </summary>
    public class TSDU
    {
        /// <summary>
        /// Reads the full COTP TSDU (Transport service data unit)
        /// See: https://tools.ietf.org/html/rfc905
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Data in TSDU</returns>
        public static async Task<Byte[]> ReadAsync(Stream stream, CancellationToken cancellationToken)
        {
            var segment = await TPDU.ReadAsync(stream, cancellationToken).ConfigureAwait(false);

            if (segment.LastDataUnit) return segment.Data;

            // More segments are expected, prepare a buffer to store all data
            var buffer = new Byte[segment.Data.Length];
            Array.Copy(segment.Data, buffer, segment.Data.Length);

            while (!segment.LastDataUnit)
            {
                segment = await TPDU.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
                var previousLength = buffer.Length;
                Array.Resize(ref buffer, buffer.Length + segment.Data.Length);
                Array.Copy(segment.Data, 0, buffer, previousLength, segment.Data.Length);
            }

            return buffer;
        }
    }
}