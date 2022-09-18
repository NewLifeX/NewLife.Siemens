using System.Net.Sockets;
using NewLife.Siemens.Common;
using NewLife.Siemens.Models;
using NewLife.Siemens;
using InvalidDataException = NewLife.Siemens.Common.InvalidDataException;

namespace NewLife.Siemens.Protocols;

/// <summary>S7驱动</summary>
public partial class S7PLC : DisposeBase
{
    #region 属性
    /// <summary>IP地址</summary>
    public String IP { get; set; }

    /// <summary>端口</summary>
    public Int32 Port { get; set; } = 102;

    /// <summary>超时时间。默认5秒</summary>
    public Int32 Timeout { get; set; } = 5_000;

    /// <summary>TSAP地址</summary>
    public TsapAddress TSAP { get; set; }

    /// <summary>类型</summary>
    public CpuType CPU { get; set; }

    /// <summary>Rack</summary>
    public Int16 Rack { get; set; }

    /// <summary>Slot</summary>
    public Int16 Slot { get; set; }

    /// <summary>最大PDU大小</summary>
    public Int32 MaxPDUSize { get; private set; } = 240;

    private byte[] plcHead1 = new byte[22]
{
      (byte) 3,
      (byte) 0,
      (byte) 0,
      (byte) 22,
      (byte) 17,
      (byte) 224,
      (byte) 0,
      (byte) 0,
      (byte) 0,
      (byte) 1,
      (byte) 0,
      (byte) 192,
      (byte) 1,
      (byte) 10,
      (byte) 193,
      (byte) 2,
      (byte) 1,
      (byte) 2,
      (byte) 194,
      (byte) 2,
      (byte) 1,
      (byte) 0
};
    private byte[] plcHead2 = new byte[25]
    {
      (byte) 3,
      (byte) 0,
      (byte) 0,
      (byte) 25,
      (byte) 2,
      (byte) 240,
      (byte) 128,
      (byte) 50,
      (byte) 1,
      (byte) 0,
      (byte) 0,
      (byte) 4,
      (byte) 0,
      (byte) 0,
      (byte) 8,
      (byte) 0,
      (byte) 0,
      (byte) 240,
      (byte) 0,
      (byte) 0,
      (byte) 1,
      (byte) 0,
      (byte) 1,
      (byte) 1,
      (byte) 224
    };
    private byte[] plcHead1_200smart = new byte[22]
    {
      (byte) 3,
      (byte) 0,
      (byte) 0,
      (byte) 22,
      (byte) 17,
      (byte) 224,
      (byte) 0,
      (byte) 0,
      (byte) 0,
      (byte) 1,
      (byte) 0,
      (byte) 193,
      (byte) 2,
      (byte) 16,
      (byte) 0,
      (byte) 194,
      (byte) 2,
      (byte) 3,
      (byte) 0,
      (byte) 192,
      (byte) 1,
      (byte) 10
    };
    private byte[] plcHead2_200smart = new byte[25]
{
      (byte) 3,
      (byte) 0,
      (byte) 0,
      (byte) 25,
      (byte) 2,
      (byte) 240,
      (byte) 128,
      (byte) 50,
      (byte) 1,
      (byte) 0,
      (byte) 0,
      (byte) 204,
      (byte) 193,
      (byte) 0,
      (byte) 8,
      (byte) 0,
      (byte) 0,
      (byte) 240,
      (byte) 0,
      (byte) 0,
      (byte) 1,
      (byte) 0,
      (byte) 1,
      (byte) 3,
      (byte) 192
};
    private TcpClient _client;
    private NetworkStream _stream;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="cpu"></param>
    /// <param name="ip"></param>
    /// <param name="rack"></param>
    /// <param name="slot"></param>
    public S7PLC(CpuType cpu, String ip, Int16 rack, Int16 slot)
    {
        IP = ip;
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
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        var client = new TcpClient
        {
            SendTimeout = Timeout,
            ReceiveTimeout = Timeout
        };

        await client.ConnectAsync(IP, Port).ConfigureAwait(false);

        _stream = client.GetStream();
        _client = client;

        try
        {
            //await queue.Enqueue(async () =>
            //{
                cancellationToken.ThrowIfCancellationRequested();
                await EstablishConnection(_stream, cancellationToken).ConfigureAwait(false);
            //}).ConfigureAwait(false);
        }
        catch (Exception)
        {
            _stream.Dispose();
            throw;
        }
    }

    private async Task EstablishConnection(Stream stream, CancellationToken cancellationToken)
    {
        await RequestConnection(stream, cancellationToken).ConfigureAwait(false);
        await SetupConnection(stream, cancellationToken).ConfigureAwait(false);
    }

    private async Task RequestConnection(Stream stream, CancellationToken cancellationToken)
    {
        var requestData = GetCOTPConnectionRequest(TSAP);
        var response = await NoLockRequestTpduAsync(stream, requestData, cancellationToken).ConfigureAwait(false);

        if (response.PDUType != COTP.PduType.ConnectionConfirmed)
        {
            throw new Common.InvalidDataException("Connection request was denied", response.TPkt.Data, 1, 0x0d);
        }
    }

    private Byte[] GetCOTPConnectionRequest(TsapAddress tsap)
    {

        if (CPU == CpuType.S7200Smart)
        {
            return plcHead1_200smart;
        }

        Byte[] buf = {
                    3, 0, 0, 22, //TPKT
                    17,     //COTP Header Length
                    224,    //Connect Request
                    0, 0,   //Destination Reference
                    0, 46,  //Source Reference
                    0,      //Flags
                    193,    //Parameter Code (src-tasp)
                    2,      //Parameter Length
                    (Byte)(tsap.Local>>8), (Byte)(tsap.Local&0xFF),   //Source TASP
                    194,    //Parameter Code (dst-tasp)
                    2,      //Parameter Length
                    (Byte)(tsap.Remote>>8), (Byte)(tsap.Remote&0xFF),   //Destination TASP
                    192,    //Parameter Code (tpdu-size)
                    1,      //Parameter Length
                    10      //TPDU Size (2^10 = 1024)
                };

        return buf;
    }

    private async Task<COTP.TPDU> NoLockRequestTpduAsync(Stream stream, Byte[] requestData,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var closeOnCancellation = cancellationToken.Register(Close);
            await stream.WriteAsync(requestData, 0, requestData.Length, cancellationToken).ConfigureAwait(false);
            return await COTP.TPDU.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
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

    private async Task SetupConnection(Stream stream, CancellationToken cancellationToken)
    {
        var setupData = GetS7ConnectionSetup();

        var s7data = await NoLockRequestTsduAsync(stream, setupData, 0, setupData.Length, cancellationToken)
            .ConfigureAwait(false);

        if (s7data.Length < 2)
            throw new WrongNumberOfBytesException("Not enough data received in response to Communication Setup");

        //Check for S7 Ack Data
        if (s7data[1] != 0x03)
            throw new InvalidDataException("Error reading Communication Setup response", s7data, 1, 0x03);

        if (s7data.Length < 20)
            throw new WrongNumberOfBytesException("Not enough data received in response to Communication Setup");

        // TODO: check if this should not rather be UInt16.
        MaxPDUSize = s7data[18] * 256 + s7data[19];
    }

    /// <summary>
    /// Close connection to PLC
    /// </summary>
    public void Close()
    {
        _client?.Close();
        _client = null;
    }

    #endregion

    #region 核心方法
    /// <summary>
    /// Creates the header to read bytes from the PLC
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    private static void BuildHeaderPackage(System.IO.MemoryStream stream, int amount = 1)
    {
        //header size = 19 bytes
        stream.WriteByteArray(new byte[] { 0x03, 0x00 });
        //complete package size
        stream.WriteByteArray(Types.Int.ToByteArray((short)(19 + (12 * amount))));
        stream.WriteByteArray(new byte[] { 0x02, 0xf0, 0x80, 0x32, 0x01, 0x00, 0x00, 0x00, 0x00 });
        //data part size
        stream.WriteByteArray(Types.Word.ToByteArray((ushort)(2 + (amount * 12))));
        stream.WriteByteArray(new byte[] { 0x00, 0x00, 0x04 });
        //amount of requests
        stream.WriteByte((byte)amount);
    }

    private byte[] RequestTsdu(byte[] requestData) => RequestTsdu(requestData, 0, requestData.Length);

    private byte[] RequestTsdu(byte[] requestData, int offset, int length, CancellationToken cancellationToken = default)
    {
        var stream = GetStreamIfAvailable();

        return 
            //queue.Enqueue(() =>
            NoLockRequestTsduAsync(stream, requestData, offset, length, cancellationToken).GetAwaiter().GetResult();
        //)
        ;
    }
    #endregion

    #region 读取
    /// <summary>
    /// Reads a number of bytes from a DB starting from a specified index. This handles more than 200 bytes with multiple requests.
    /// If the read was not successful, check LastErrorCode or LastErrorString.
    /// </summary>
    /// <param name="dataType">Data type of the memory area, can be DB, Timer, Counter, Merker(Memory), Input, Output.</param>
    /// <param name="db">Address of the memory area (if you want to read DB1, this is set to 1). This must be set also for other memory area types: counters, timers,etc.</param>
    /// <param name="startByteAdr">Start byte address. If you want to read DB1.DBW200, this is 200.</param>
    /// <param name="count">Byte count, if you want to read 120 bytes, set this to 120.</param>
    /// <returns>Returns the bytes in an array</returns>
    public byte[] ReadBytes(DataType dataType, int db, int startByteAdr, int count)
    {
        var result = new byte[count];
        int index = 0;
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

    private void ReadBytesWithSingleRequest(DataType dataType, int db, int startByteAdr, byte[] buffer, int offset, int count)
    {
        try
        {
            // first create the header
            int packageSize = 19 + 12; // 19 header + 12 for 1 request
            var package = new System.IO.MemoryStream(packageSize);
            BuildHeaderPackage(package);
            // package.Add(0x02);  // datenart
            BuildReadDataRequestPackage(package, dataType, db, startByteAdr, count);

            var dataToSend = package.ToArray();
            var s7data = RequestTsdu(dataToSend);
            AssertReadResponse(s7data, count);

            Array.Copy(s7data, 18, buffer, offset, count);
        }
        catch (Exception exc)
        {
            throw new PlcException(ErrorCode.ReadData, exc);
        }
    }

    /// <summary>
    /// Create the bytes-package to request data from the PLC. You have to specify the memory type (dataType),
    /// the address of the memory, the address of the byte and the bytes count.
    /// </summary>
    /// <param name="dataType">MemoryType (DB, Timer, Counter, etc.)</param>
    /// <param name="db">Address of the memory to be read</param>
    /// <param name="startByteAdr">Start address of the byte</param>
    /// <param name="count">Number of bytes to be read</param>
    /// <returns></returns>
    private static void BuildReadDataRequestPackage(System.IO.MemoryStream stream, DataType dataType, int db, int startByteAdr, int count = 1)
    {
        //single data req = 12
        stream.WriteByteArray(new byte[] { 0x12, 0x0a, 0x10 });
        switch (dataType)
        {
            case DataType.Timer:
            case DataType.Counter:
                stream.WriteByte((byte)dataType);
                break;
            default:
                stream.WriteByte(0x02);
                break;
        }

        stream.WriteByteArray(Types.Word.ToByteArray((ushort)(count)));
        stream.WriteByteArray(Types.Word.ToByteArray((ushort)(db)));
        stream.WriteByte((byte)dataType);
        var overflow = (int)(startByteAdr * 8 / 0xffffU); // handles words with address bigger than 8191
        stream.WriteByte((byte)overflow);
        switch (dataType)
        {
            case DataType.Timer:
            case DataType.Counter:
                stream.WriteByteArray(Types.Word.ToByteArray((ushort)(startByteAdr)));
                break;
            default:
                stream.WriteByteArray(Types.Word.ToByteArray((ushort)((startByteAdr) * 8)));
                break;
        }
    }
    #endregion

    #region 写入
    /// <summary>
    /// Write a number of bytes from a DB starting from a specified index. This handles more than 200 bytes with multiple requests.
    /// If the write was not successful, check LastErrorCode or LastErrorString.
    /// </summary>
    /// <param name="dataType">Data type of the memory area, can be DB, Timer, Counter, Merker(Memory), Input, Output.</param>
    /// <param name="db">Address of the memory area (if you want to read DB1, this is set to 1). This must be set also for other memory area types: counters, timers,etc.</param>
    /// <param name="startByteAdr">Start byte address. If you want to write DB1.DBW200, this is 200.</param>
    /// <param name="value">Bytes to write. If more than 200, multiple requests will be made.</param>
    public void WriteBytes(DataType dataType, int db, int startByteAdr, byte[] value)
    {
        int localIndex = 0;
        int count = value.Length;
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

    private void WriteBytesWithASingleRequest(DataType dataType, int db, int startByteAdr, byte[] value, int dataOffset, int count)
    {
        try
        {
            var dataToSend = BuildWriteBytesPackage(dataType, db, startByteAdr, value, dataOffset, count);
            var s7data = RequestTsdu(dataToSend);

            ValidateResponseCode((ReadWriteErrorCode)s7data[14]);
        }
        catch (Exception exc)
        {
            throw new PlcException(ErrorCode.WriteData, exc);
        }
    }

    private byte[] BuildWriteBytesPackage(DataType dataType, int db, int startByteAdr, byte[] value, int dataOffset, int count)
    {
        int varCount = count;
        // first create the header
        int packageSize = 35 + varCount;
        var package = new MemoryStream(new byte[packageSize]);

        package.WriteByte(3);
        package.WriteByte(0);
        //complete package size
        package.WriteByteArray(Types.Int.ToByteArray((short)packageSize));
        package.WriteByteArray(new byte[] { 2, 0xf0, 0x80, 0x32, 1, 0, 0 });
        package.WriteByteArray(Types.Word.ToByteArray((ushort)(varCount - 1)));
        package.WriteByteArray(new byte[] { 0, 0x0e });
        package.WriteByteArray(Types.Word.ToByteArray((ushort)(varCount + 4)));
        package.WriteByteArray(new byte[] { 0x05, 0x01, 0x12, 0x0a, 0x10, 0x02 });
        package.WriteByteArray(Types.Word.ToByteArray((ushort)varCount));
        package.WriteByteArray(Types.Word.ToByteArray((ushort)(db)));
        package.WriteByte((byte)dataType);
        var overflow = (int)(startByteAdr * 8 / 0xffffU); // handles words with address bigger than 8191
        package.WriteByte((byte)overflow);
        package.WriteByteArray(Types.Word.ToByteArray((ushort)(startByteAdr * 8)));
        package.WriteByteArray(new byte[] { 0, 4 });
        package.WriteByteArray(Types.Word.ToByteArray((ushort)(varCount * 8)));

        // now join the header and the data
        package.Write(value, dataOffset, count);

        return package.ToArray();
    }
    #endregion
    private void AssertPduSizeForRead(ICollection<Types.DataItem> dataItems)
    {
        // send request limit: 19 bytes of header data, 12 bytes of parameter data for each dataItem
        var requiredRequestSize = 19 + dataItems.Count * 12;
        if (requiredRequestSize > MaxPDUSize) throw new Exception($"Too many vars requested for read. Request size ({requiredRequestSize}) is larger than protocol limit ({MaxPDUSize}).");

        // response limit: 14 bytes of header data, 4 bytes of result data for each dataItem and the actual data
        var requiredResponseSize = GetDataLength(dataItems) + dataItems.Count * 4 + 14;
        if (requiredResponseSize > MaxPDUSize) throw new Exception($"Too much data requested for read. Response size ({requiredResponseSize}) is larger than protocol limit ({MaxPDUSize}).");
    }

    private void AssertPduSizeForWrite(ICollection<Types.DataItem> dataItems)
    {
        // 12 bytes of header data, 18 bytes of parameter data for each dataItem
        if (dataItems.Count * 18 + 12 > MaxPDUSize) throw new Exception("Too many vars supplied for write");

        // 12 bytes of header data, 16 bytes of data for each dataItem and the actual data
        if (GetDataLength(dataItems) + dataItems.Count * 16 + 12 > MaxPDUSize)
            throw new Exception("Too much data supplied for write");
    }

    private Int32 GetDataLength(IEnumerable<Types.DataItem> dataItems)
    {
        // Odd length variables are 0-padded
        return dataItems.Select(di => VarTypeToByteLength(di.VarType, di.Count))
            .Sum(len => (len & 1) == 1 ? len + 1 : len);
    }

    private static void AssertReadResponse(Byte[] s7Data, Int32 dataLength)
    {
        var expectedLength = dataLength + 18;

        PlcException NotEnoughBytes()
        {
            return new PlcException(ErrorCode.WrongNumberReceivedBytes,
$"Received {s7Data.Length} bytes: '{BitConverter.ToString(s7Data)}', expected {expectedLength} bytes.")
;
        }

        if (s7Data == null)
            throw new PlcException(ErrorCode.WrongNumberReceivedBytes, "No s7Data received.");

        if (s7Data.Length < 15) throw NotEnoughBytes();

        ValidateResponseCode((ReadWriteErrorCode)s7Data[14]);

        if (s7Data.Length < expectedLength) throw NotEnoughBytes();
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
        {
            throw new PlcException(ErrorCode.ConnectionError, "Plc is not connected");
        }

        return _stream;
    }

    private byte[] GetS7ConnectionSetup()
    {
        if (CPU == CpuType.S7200Smart)
        {
            return plcHead2_200smart;
        }

        return new byte[] {  3, 0, 0, 25, 2, 240, 128, 50, 1, 0, 0, 255, 255, 0, 8, 0, 0, 240, 0, 0, 3, 0, 3,
                    3, 192 // Use 960 PDU size
            };
    }

    private async Task<byte[]> NoLockRequestTsduAsync(Stream stream, byte[] requestData, int offset, int length,
    CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var closeOnCancellation = cancellationToken.Register(Close);
            await stream.WriteAsync(requestData, offset, length, cancellationToken).ConfigureAwait(false);
            return await COTP.TSDU.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
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


    /// <summary>
    /// Given a S7 <see cref="VarType"/> (Bool, Word, DWord, etc.), it returns how many bytes to read.
    /// </summary>
    /// <param name="varType"></param>
    /// <param name="varCount"></param>
    /// <returns>Byte lenght of variable</returns>
    internal static int VarTypeToByteLength(VarType varType, int varCount = 1)
    {
        switch (varType)
        {
            case VarType.Bit:
                return (varCount + 7) / 8;
            case VarType.Byte:
                return (varCount < 1) ? 1 : varCount;
            case VarType.String:
                return varCount;
            case VarType.S7String:
                return ((varCount + 2) & 1) == 1 ? (varCount + 3) : (varCount + 2);
            case VarType.S7WString:
                return (varCount * 2) + 4;
            case VarType.Word:
            case VarType.Timer:
            case VarType.Int:
            case VarType.Counter:
                return varCount * 2;
            case VarType.DWord:
            case VarType.DInt:
            case VarType.Real:
                return varCount * 4;
            case VarType.LReal:
            case VarType.DateTime:
                return varCount * 8;
            case VarType.DateTimeLong:
                return varCount * 12;
            default:
                return 0;
        }
    }

}
