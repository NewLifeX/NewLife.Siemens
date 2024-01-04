using System.Net.Sockets;
using NewLife.Data;
using NewLife.Siemens.Common;
using NewLife.Siemens.Models;

namespace NewLife.Siemens.Protocols;

/// <summary>S7驱动</summary>
public partial class S7PLC : DisposeBase
{
    #region 属性
    /// <summary>IP地址</summary>
    public String IP { get; set; }

    /// <summary>端口</summary>
    public Int32 Port { get; set; } = 102;

    /// <summary>超时时间。默认5000毫秒</summary>
    public Int32 Timeout { get; set; } = 5_000;

    /// <summary>TSAP地址</summary>
    public TsapAddress TSAP { get; set; }

    /// <summary>类型</summary>
    public CpuType CPU { get; set; }

    /// <summary>机架号。通常为0</summary>
    public Int16 Rack { get; set; }

    /// <summary>插槽，对于S7300-S7400通常为2，对于S7-1200和S7-1500为0</summary>
    public Int16 Slot { get; set; }

    /// <summary>最大PDU大小</summary>
    public Int32 MaxPDUSize { get; private set; } = 240;

    private readonly Byte[] plcHead1 = [3, 0, 0, 22, 17, 224, 0, 0, 0, 1, 0, 192, 1, 10, 193, 2, 1, 2, 194, 2, 1, 0];
    private readonly Byte[] plcHead2 = [3, 0, 0, 25, 2, 240, 128, 50, 1, 0, 0, 4, 0, 0, 8, 0, 0, 240, 0, 0, 1, 0, 1, 1, 224];
    private readonly Byte[] plcHead1_200smart = [3, 0, 0, 22, 17, 224, 0, 0, 0, 1, 0, 193, 2, 16, 0, 194, 2, 3, 0, 192, 1, 10];
    private readonly Byte[] plcHead2_200smart = [3, 0, 0, 25, 2, 240, 128, 50, 1, 0, 0, 204, 193, 0, 8, 0, 0, 240, 0, 0, 1, 0, 1, 3, 192];
    private TcpClient _client;
    private NetworkStream _stream;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="cpu"></param>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    /// <param name="rack"></param>
    /// <param name="slot"></param>
    public S7PLC(CpuType cpu, String ip, Int32 port, Int16 rack = 0, Int16 slot = 0)
    {
        IP = ip;
        if (port > 0) Port = port;

        CPU = cpu;
        Rack = rack;
        Slot = slot;

        TSAP = TsapAddress.GetDefaultTsapPair(cpu, rack, slot);
    }

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        Close();
    }
    #endregion

    #region 连接
    /// <summary>打开连接</summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        var client = new TcpClient
        {
            SendTimeout = Timeout,
            ReceiveTimeout = Timeout
        };

        await client.ConnectAsync(IP, Port).ConfigureAwait(false);

        var stream = client.GetStream();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RequestConnection(stream, cancellationToken).ConfigureAwait(false);
            await SetupConnection(stream, cancellationToken).ConfigureAwait(false);

            _stream = stream;
            _client = client;
        }
        catch (Exception)
        {
            stream.Dispose();
            throw;
        }
    }

    private async Task RequestConnection(Stream stream, CancellationToken cancellationToken)
    {
        var request = GetConnectionRequest(TSAP);
        var response = await RequestAsync(stream, request, cancellationToken).ConfigureAwait(false);

        if (response.Type != PduType.ConnectionConfirmed)
            throw new InvalidDataException($"Connection request was denied (PDUType={response.Type})");
    }

    private COTP GetConnectionRequest(TsapAddress tsap)
    {
        COTP cotp;
        if (CPU == CpuType.S7200Smart)
        {
            cotp = new COTP
            {
                Type = PduType.Data,
                LastDataUnit = true,
                Data = new Byte[] { 50, 1, 0, 0, 204, 193, 0, 8, 0, 0, 240, 0, 0, 1, 0, 1, 3, 192 }
            };
        }
        else
        {
            cotp = new COTP
            {
                Type = PduType.ConnectionRequest,
                Destination = 0x00,
                Source = 0x2E,
                Option = 0x00,
            };
            cotp.Parameters.Add(new COTPParameter { Kind = COTPParameterKinds.SrcTsap, Value = tsap.Local });
            cotp.Parameters.Add(new COTPParameter { Kind = COTPParameterKinds.DstTsap, Value = tsap.Remote });
            cotp.Parameters.Add(new COTPParameter { Kind = COTPParameterKinds.TpduSize, Value = (Byte)0x0A });
        }

        //var tpkt = new TPKT { Version = 3, };

        //// 先写COTP，得到长度后再回过头写TPKT
        //var ms = new MemoryStream
        //{
        //    Position = 4
        //};
        //cotp.Write(ms, null);

        //ms.Position = 0;
        //tpkt.Length = (UInt16)(ms.Length - 4);
        //tpkt.Write(ms);

        //return ms.ToArray();

        //var ms = new MemoryStream();
        //cotp.WriteWithTPKT(ms);

        //return ms.ToArray();
        return cotp;

        //if (CPU == CpuType.S7200Smart) return plcHead1_200smart;

        //Byte[] buf = [
        //    3,
        //    0,
        //    0,
        //    22, //TPKT
        //    17,     //COTP Header Length
        //    224,    //Connect Request
        //    0,
        //    0,   //Destination Reference
        //    0,
        //    46,  //Source Reference
        //    0,      //Flags
        //    193,    //Parameter Code (src-tasp)
        //    2,      //Parameter Length
        //    (Byte)(tsap.Local >> 8),
        //    (Byte)(tsap.Local & 0xFF),   //Source TASP
        //    194,    //Parameter Code (dst-tasp)
        //    2,      //Parameter Length
        //    (Byte)(tsap.Remote >> 8),
        //    (Byte)(tsap.Remote & 0xFF),   //Destination TASP
        //    192,    //Parameter Code (tpdu-size)
        //    1,      //Parameter Length
        //    10      //TPDU Size (2^10 = 1024)
        //        ];

        //return buf;
    }

    private async Task SetupConnection(Stream stream, CancellationToken cancellationToken)
    {
        var setupData = GetS7ConnectionSetup();

        var s7data = await RequestAsync(stream, setupData, 0, setupData.Length, cancellationToken)
            .ConfigureAwait(false);

        if (s7data.Count < 2)
            throw new WrongNumberOfBytesException("Not enough data received in response to Communication Setup");

        //Check for S7 Ack Data
        if (s7data[1] != 0x03)
            throw new InvalidDataException("Error reading Communication Setup response");

        if (s7data.Count < 20)
            throw new WrongNumberOfBytesException("Not enough data received in response to Communication Setup");

        // TODO: check if this should not rather be UInt16.
        MaxPDUSize = s7data[18] * 256 + s7data[19];
    }

    private Byte[] GetS7ConnectionSetup()
    {
        if (CPU == CpuType.S7200Smart) return plcHead2_200smart;

        return [3, 0, 0, 25, 2, 240, 128, 50, 1, 0, 0, 255, 255, 0, 8, 0, 0, 240, 0, 0, 3, 0, 3, 3, 192];
    }

    /// <summary>关闭连接</summary>
    public void Close()
    {
        _client?.Close();
        _client = null;
    }
    #endregion

    #region 核心方法
    /// <summary>生成头部</summary>
    /// <param name="stream"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    private static void BuildHeaderPackage(MemoryStream stream, Int32 amount = 1)
    {
        //header size = 19 bytes
        stream.WriteByteArray([0x03, 0x00]);
        //complete package size
        stream.WriteByteArray(((Int16)(19 + (12 * amount))).GetBytes(false));
        stream.WriteByteArray([0x02, 0xf0, 0x80, 0x32, 0x01, 0x00, 0x00, 0x00, 0x00]);
        //data part size
        stream.WriteByteArray(ToByteArray((UInt16)(2 + (amount * 12))));
        stream.WriteByteArray([0x00, 0x00, 0x04]);
        //amount of requests
        stream.WriteByte((Byte)amount);
    }

    /// <summary>异步请求</summary>
    /// <param name="stream"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<COTP> RequestAsync(Stream stream, COTP request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var pk = request.GetBytes(true);
            var buf = pk.ReadBytes();

            using var closeOnCancellation = cancellationToken.Register(Close);
            //await pk.CopyToAsync(stream, cancellationToken);
            await stream.WriteAsync(buf, 0, buf.Length, cancellationToken);
            return await COTP.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exc)
        {
            if (exc is TPDUInvalidException || exc is TPKTInvalidException)
            {
                Close();
            }

            throw;
        }
    }

    /// <summary>异步请求</summary>
    /// <param name="stream"></param>
    /// <param name="request"></param>
    /// <param name="offset"></param>
    /// <param name="length"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<Packet> RequestAsync(Stream stream, Byte[] request, Int32 offset, Int32 length,
    CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var closeOnCancellation = cancellationToken.Register(Close);
            await stream.WriteAsync(request, offset, length, cancellationToken).ConfigureAwait(false);
            return await COTP.ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exc)
        {
            if (exc is TPDUInvalidException || exc is TPKTInvalidException)
            {
                Close();
            }

            throw;
        }
    }

    private Packet Request(Byte[] requestData) => Request(requestData, 0, requestData.Length);

    private Packet Request(Byte[] requestData, Int32 offset, Int32 length, CancellationToken cancellationToken = default)
    {
        var stream = GetStreamIfAvailable();

        return RequestAsync(stream, requestData, offset, length, cancellationToken).GetAwaiter().GetResult();
    }
    #endregion

    #region 读取
    /// <summary>从指定DB开始，读取多个字节</summary>
    /// <param name="dataType">Data type of the memory area, can be DB, Timer, Counter, Merker(Memory), Input, Output.</param>
    /// <param name="db">Address of the memory area (if you want to read DB1, this is set to 1). This must be set also for other memory area types: counters, timers,etc.</param>
    /// <param name="startByteAdr">Start byte address. If you want to read DB1.DBW200, this is 200.</param>
    /// <param name="count">Byte count, if you want to read 120 bytes, set this to 120.</param>
    /// <returns>Returns the bytes in an array</returns>
    public Byte[] ReadBytes(DataType dataType, Int32 db, Int32 startByteAdr, Int32 count)
    {
        var result = new Byte[count];
        var index = 0;
        while (count > 0)
        {
            //This works up to MaxPDUSize-1 on SNAP7. But not MaxPDUSize-0.
            var maxToRead = Math.Min(count, MaxPDUSize - 18);
            ReadBytesWithSingleRequest(dataType, db, startByteAdr + index, result, index, maxToRead);
            count -= maxToRead;
            index += maxToRead;
        }
        return result;
    }

    private void ReadBytesWithSingleRequest(DataType dataType, Int32 db, Int32 startByteAdr, Byte[] buffer, Int32 offset, Int32 count)
    {
        try
        {
            // first create the header
            var packageSize = 19 + 12; // 19 header + 12 for 1 request
            var package = new MemoryStream(packageSize);
            BuildHeaderPackage(package);
            // package.Add(0x02);  // datenart
            BuildReadDataRequestPackage(package, dataType, db, startByteAdr, count);

            var dataToSend = package.ToArray();
            var s7data = Request(dataToSend);
            AssertReadResponse(s7data, count);

            //Array.Copy(s7data, 18, buffer, offset, count);
            s7data.Slice(18).WriteTo(buffer, offset, count);
        }
        catch (Exception exc)
        {
            throw new PlcException(ErrorCode.ReadData, exc);
        }
    }

    /// <summary>创建请求包</summary>
    /// <param name="stream"></param>
    /// <param name="dataType">MemoryType (DB, Timer, Counter, etc.)</param>
    /// <param name="db">Address of the memory to be read</param>
    /// <param name="startByteAdr">Start address of the byte</param>
    /// <param name="count">Number of bytes to be read</param>
    /// <returns></returns>
    private static void BuildReadDataRequestPackage(MemoryStream stream, DataType dataType, Int32 db, Int32 startByteAdr, Int32 count = 1)
    {
        //single data req = 12
        stream.WriteByteArray([0x12, 0x0a, 0x10]);
        switch (dataType)
        {
            case DataType.Timer:
            case DataType.Counter:
                stream.WriteByte((Byte)dataType);
                break;
            default:
                stream.WriteByte(0x02);
                break;
        }

        stream.WriteByteArray(ToByteArray((UInt16)(count)));
        stream.WriteByteArray(ToByteArray((UInt16)(db)));
        stream.WriteByte((Byte)dataType);
        var overflow = (Int32)(startByteAdr * 8 / 0xffffU); // handles words with address bigger than 8191
        stream.WriteByte((Byte)overflow);
        switch (dataType)
        {
            case DataType.Timer:
            case DataType.Counter:
                stream.WriteByteArray(ToByteArray((UInt16)(startByteAdr)));
                break;
            default:
                stream.WriteByteArray(ToByteArray((UInt16)((startByteAdr) * 8)));
                break;
        }
    }
    #endregion

    #region 写入
    /// <summary>从指定DB开始，写入多个字节</summary>
    /// <param name="dataType">Data type of the memory area, can be DB, Timer, Counter, Merker(Memory), Input, Output.</param>
    /// <param name="db">Address of the memory area (if you want to read DB1, this is set to 1). This must be set also for other memory area types: counters, timers,etc.</param>
    /// <param name="startByteAdr">Start byte address. If you want to write DB1.DBW200, this is 200.</param>
    /// <param name="value">Bytes to write. If more than 200, multiple requests will be made.</param>
    public void WriteBytes(DataType dataType, Int32 db, Int32 startByteAdr, Byte[] value)
    {
        var localIndex = 0;
        var count = value.Length;
        while (count > 0)
        {
            //TODO: Figure out how to use MaxPDUSize here
            //Snap7 seems to choke on PDU sizes above 256 even if snap7
            //replies with bigger PDU size in connection setup.
            var maxToWrite = Math.Min(count, MaxPDUSize - 28);//TODO tested only when the MaxPDUSize is 480
            WriteBytesWithASingleRequest(dataType, db, startByteAdr + localIndex, value, localIndex, maxToWrite);
            count -= maxToWrite;
            localIndex += maxToWrite;
        }
    }

    private void WriteBytesWithASingleRequest(DataType dataType, Int32 db, Int32 startByteAdr, Byte[] value, Int32 dataOffset, Int32 count)
    {
        try
        {
            var dataToSend = BuildWriteBytesPackage(dataType, db, startByteAdr, value, dataOffset, count);
            var s7data = Request(dataToSend);

            ValidateResponseCode((ReadWriteErrorCode)s7data[14]);
        }
        catch (Exception exc)
        {
            throw new PlcException(ErrorCode.WriteData, exc);
        }
    }

    private Byte[] BuildWriteBytesPackage(DataType dataType, Int32 db, Int32 startByteAdr, Byte[] value, Int32 dataOffset, Int32 count)
    {
        var varCount = count;
        // first create the header
        var packageSize = 35 + varCount;
        var package = new MemoryStream(new Byte[packageSize]);

        package.WriteByte(3);
        package.WriteByte(0);
        //complete package size
        package.WriteByteArray(((Int16)packageSize).GetBytes(false));
        package.WriteByteArray([2, 0xf0, 0x80, 0x32, 1, 0, 0]);
        package.WriteByteArray(ToByteArray((UInt16)(varCount - 1)));
        package.WriteByteArray([0, 0x0e]);
        package.WriteByteArray(ToByteArray((UInt16)(varCount + 4)));
        package.WriteByteArray([0x05, 0x01, 0x12, 0x0a, 0x10, 0x02]);
        package.WriteByteArray(ToByteArray((UInt16)varCount));
        package.WriteByteArray(ToByteArray((UInt16)(db)));
        package.WriteByte((Byte)dataType);
        var overflow = (Int32)(startByteAdr * 8 / 0xffffU); // handles words with address bigger than 8191
        package.WriteByte((Byte)overflow);
        package.WriteByteArray(ToByteArray((UInt16)(startByteAdr * 8)));
        package.WriteByteArray([0, 4]);
        package.WriteByteArray(ToByteArray((UInt16)(varCount * 8)));

        // now join the header and the data
        package.Write(value, dataOffset, count);

        return package.ToArray();
    }
    #endregion

    #region 辅助
    /// <summary>
    /// Converts a ushort (UInt16) to word (2 bytes)
    /// </summary>
    public static Byte[] ToByteArray(UInt16 value)
    {
        var bytes = new Byte[2];

        bytes[1] = (Byte)(value & 0xFF);
        bytes[0] = (Byte)(value >> 8 & 0xFF);

        return bytes;
    }

    private static void AssertReadResponse(Packet s7Data, Int32 dataLength)
    {
        var expectedLength = dataLength + 18;

        PlcException NotEnoughBytes()
        {
            return new PlcException(ErrorCode.WrongNumberReceivedBytes, $"Received {s7Data.Count} bytes: '{s7Data.ToHex()}', expected {expectedLength} bytes.");
        }

        if (s7Data == null)
            throw new PlcException(ErrorCode.WrongNumberReceivedBytes, "No s7Data received.");

        if (s7Data.Count < 15) throw NotEnoughBytes();

        ValidateResponseCode((ReadWriteErrorCode)s7Data[14]);

        if (s7Data.Count < expectedLength) throw NotEnoughBytes();
    }

    internal static void ValidateResponseCode(ReadWriteErrorCode statusCode)
    {
        switch (statusCode)
        {
            case ReadWriteErrorCode.ObjectDoesNotExist:
                throw new Exception("Received error from PLC: Object does not exist.");
            case ReadWriteErrorCode.DataTypeInconsistent:
                throw new Exception("Received error from PLC: Data type inconsistent.");
            case ReadWriteErrorCode.DataTypeNotSupported:
                throw new Exception("Received error from PLC: Data type not supported.");
            case ReadWriteErrorCode.AccessingObjectNotAllowed:
                throw new Exception("Received error from PLC: Accessing object not allowed.");
            case ReadWriteErrorCode.AddressOutOfRange:
                throw new Exception("Received error from PLC: Address out of range.");
            case ReadWriteErrorCode.HardwareFault:
                throw new Exception("Received error from PLC: Hardware fault.");
            case ReadWriteErrorCode.Success:
                break;
            default:
                throw new Exception($"Invalid response from PLC: statusCode={(Byte)statusCode}.");
        }
    }

    private Stream GetStreamIfAvailable()
    {
        if (_stream == null)
            throw new PlcException(ErrorCode.ConnectionError, "Plc is not connected");

        return _stream;
    }
    #endregion
}
