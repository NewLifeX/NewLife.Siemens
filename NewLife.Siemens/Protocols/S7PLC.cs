using System;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Siemens.Common;
using NewLife.Siemens.Messages;
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

    ///// <summary>TSAP地址</summary>
    //public TsapAddress TSAP { get; set; }

    /// <summary>类型</summary>
    public CpuType CPU { get; set; }

    /// <summary>机架号。通常为0</summary>
    public Int16 Rack { get; set; }

    /// <summary>插槽，对于S7300-S7400通常为2，对于S7-1200和S7-1500为0</summary>
    public Int16 Slot { get; set; }

    /// <summary>最大PDU大小</summary>
    public Int32 MaxPDUSize { get; private set; } = 1024;

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

        //TSAP = TsapAddress.GetDefaultTsapPair(cpu, rack, slot);
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
        var tsap = TsapAddress.GetDefaultTsapPair(CPU, Rack, Slot);
        var request = GetConnectionRequest(tsap);
        var response = await RequestAsync(stream, request, cancellationToken).ConfigureAwait(false);

        if (response.Type != PduType.ConnectionConfirmed)
            throw new InvalidDataException($"Connection request was denied (PDUType={response.Type})");
    }

    private COTP GetConnectionRequest(TsapAddress tsap)
    {
        var cotp = new COTP
        {
            Type = PduType.ConnectionRequest,
            Destination = 0x00,
            Source = 0x01,
            Option = 0x00,
        };
        cotp.SetParameter(COTPParameterKinds.SrcTsap, tsap.Local);
        cotp.SetParameter(COTPParameterKinds.DstTsap, tsap.Remote);
        cotp.SetParameter(COTPParameterKinds.TpduSize, (Byte)0x0A);

        return cotp;
    }

    private async Task SetupConnection(Stream stream, CancellationToken cancellationToken)
    {
        var setup = GetS7ConnectionSetup();

        var s7data = await RequestAsync(stream, setup, cancellationToken)
            .ConfigureAwait(false);

        var rs = new S7Message();
        if (!rs.Read(s7data.Data)) return;

        if (rs.Kind != S7Kinds.AckData)
            throw new InvalidDataException("Error reading Communication Setup response");

        var pm = rs.Parameters?.FirstOrDefault(e => e.Code == S7Functions.Setup) as SetupMessage;
        if (pm != null) MaxPDUSize = pm.PduLength;
    }

    private COTP GetS7ConnectionSetup()
    {
        var msg = new S7Message
        {
            Kind = S7Kinds.Job,
        };
        if (CPU == CpuType.S7200Smart)
        {
            msg.Sequence = 0xCCC1;
            msg.Setup(0x0003, 960);
        }
        else
        {
            msg.Sequence = 0xFFFF;
            msg.Setup(0x0001, 960);
        }

        return msg.ToCOTP();
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

    public async Task<S7Message?> RequestAsync(S7Message request, CancellationToken cancellationToken = default)
    {
        XTrace.WriteLine("=> {0}", request);
        var data = request.ToCOTP().ToPacket(true).ReadBytes();
        XTrace.WriteLine("=> {0}", data.ToHex("-", 4));

        var rs = await RequestAsync(_stream, data, 0, data.Length, cancellationToken).ConfigureAwait(false);
        if (rs == null) return null;

        var msg = new S7Message();
        if (!msg.Read(rs)) return null;

        XTrace.WriteLine("<= {0}", msg);

        return msg;
    }

    /// <summary>发起Job请求</summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<S7Parameter?> InvokeAsync(S7Parameter request, CancellationToken cancellationToken = default)
    {
        var msg = new S7Message
        {
            Kind = S7Kinds.Job,
        };

        msg.SetParameter(request);

        var rs = await RequestAsync(msg, cancellationToken).ConfigureAwait(false);
        if (rs == null) return null;

        return rs.Parameters?.FirstOrDefault();
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
            var pk = request.ToPacket(true);
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
    private async Task<Packet?> RequestAsync(Stream stream, Byte[] request, Int32 offset, Int32 length,
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
    /// <summary>读取多个字节</summary>
    /// <param name="dataType"></param>
    /// <param name="db"></param>
    /// <param name="startByteAdr"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public Byte[] ReadBytes(DataType dataType, Int32 db, Int32 startByteAdr, Int32 count)
    {
        var ms = new MemoryStream();
        var index = 0;
        while (count > 0)
        {
            // 最大PDU大小
            var maxToRead = Math.Min(count, MaxPDUSize - 18);

            var request = BuildRead(dataType, db, startByteAdr + index, maxToRead);

            // 发起请求
            var rs = InvokeAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
            if (rs is not ReadResponse res) break;

            if (res.Items != null)
            {
                foreach (var item in res.Items)
                {
                    var code = res.Items[0].Code;
                    if (code != ReadWriteErrorCode.Success) throw new ApiException((Int32)code, code + "");

                    if (item.Data != null)
                        ms.Write(item.Data, 0, item.Data.Length);
                }
            }

            count -= maxToRead;
            index += maxToRead;
        }
        return ms.ToArray();
    }

    private static ReadRequest BuildRead(DataType dataType, Int32 db, Int32 startByteAdr, Int32 count = 1)
    {
        var ri = new RequestItem
        {
            // S7ANY
            SyntaxId = 0x10,
            // BIT
            Type = VarType.Bit,
            Count = 1,
            DbNumber = (UInt16)db,
            Area = dataType,

            Address = (UInt32)startByteAdr,
        };

        ri.Type = dataType switch
        {
            DataType.Timer => VarType.Timer,
            DataType.Counter => VarType.Counter,
            _ => VarType.Word,
        };

        var request = new ReadRequest();
        request.Items.Add(ri);

        return request;
    }
    #endregion

    #region 写入
    /// <summary>从指定DB开始，写入多个字节</summary>
    /// <param name="dataType"></param>
    /// <param name="db"></param>
    /// <param name="startByteAdr"></param>
    /// <param name="value"></param>
    public void WriteBytes(DataType dataType, Int32 db, Int32 startByteAdr, Byte[] value)
    {
        var localIndex = 0;
        var count = value.Length;
        while (count > 0)
        {
            //TODO: Figure out how to use MaxPDUSize here
            //Snap7 seems to choke on PDU sizes above 256 even if snap7
            //replies with bigger PDU size in connection setup.
            var maxToWrite = Math.Min(count, MaxPDUSize - 28);
            //TODO tested only when the MaxPDUSize is 480
            //WriteBytesWithASingleRequest(dataType, db, startByteAdr + localIndex, value, localIndex, maxToWrite);

            var request = BuildWrite(dataType, db, startByteAdr + localIndex, value, localIndex, maxToWrite);

            // 发起请求
            var rs = InvokeAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
            if (rs is not WriteResponse res) break;

            if (res.Items != null && res.Items.Count > 0)
            {
                var code = res.Items[0].Code;
                if (code != ReadWriteErrorCode.Success) throw new ApiException((Int32)code, code + "");
            }

            count -= maxToWrite;
            localIndex += maxToWrite;
        }
    }

    private WriteRequest BuildWrite(DataType dataType, Int32 db, Int32 startByteAdr, Byte[] value, Int32 offset, Int32 count)
    {
        var request = new WriteRequest();
        request.Items.Add(new RequestItem
        {
            SpecType = 0x12,
            SyntaxId = 0x10,
            Type = VarType.Byte,
            Count = (UInt16)count,
            DbNumber = (UInt16)db,
            Area = dataType,
            Address = (UInt32)startByteAdr
        });
        request.DataItems.Add(new DataItem
        {
            Type = VarType.DWord,
            Data = value.ReadBytes(offset, count),
        });

        return request;
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
