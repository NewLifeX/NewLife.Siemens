﻿using System.Net.Sockets;
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
            await queue.Enqueue(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await EstablishConnection(stream, cancellationToken).ConfigureAwait(false);
                _stream = stream;

                return default(Object);
            }).ConfigureAwait(false);
        }
        catch (Exception)
        {
            stream.Dispose();
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
            throw new InvalidDataException("Connection request was denied", response.TPkt.Data, 1, 0x0d);
        }
    }

    private static Byte[] GetCOTPConnectionRequest(TsapAddress tsap)
    {
        Byte[] buf = {
                    3, 0, 0, 22, //TPKT
                    17,     //COTP Header Length
                    224,    //Connect Request
                    0, 0,   //Destination Reference
                    0, 46,  //Source Reference
                    0,      //Flags
                    193,    //Parameter Code (src-tasp)
                    2,      //Parameter Length
                    (Byte)(tsap.Local>>8), (Byte)(tsap.Local|0xFF),   //Source TASP
                    194,    //Parameter Code (dst-tasp)
                    2,      //Parameter Length
                    (Byte)(tsap.Remote>>8), (Byte)(tsap.Remote|0xFF),   //Destination TASP
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

    private void AssertPduSizeForRead(ICollection<DataItem> dataItems)
    {
        // send request limit: 19 bytes of header data, 12 bytes of parameter data for each dataItem
        var requiredRequestSize = 19 + dataItems.Count * 12;
        if (requiredRequestSize > MaxPDUSize) throw new Exception($"Too many vars requested for read. Request size ({requiredRequestSize}) is larger than protocol limit ({MaxPDUSize}).");

        // response limit: 14 bytes of header data, 4 bytes of result data for each dataItem and the actual data
        var requiredResponseSize = GetDataLength(dataItems) + dataItems.Count * 4 + 14;
        if (requiredResponseSize > MaxPDUSize) throw new Exception($"Too much data requested for read. Response size ({requiredResponseSize}) is larger than protocol limit ({MaxPDUSize}).");
    }

    private void AssertPduSizeForWrite(ICollection<DataItem> dataItems)
    {
        // 12 bytes of header data, 18 bytes of parameter data for each dataItem
        if (dataItems.Count * 18 + 12 > MaxPDUSize) throw new Exception("Too many vars supplied for write");

        // 12 bytes of header data, 16 bytes of data for each dataItem and the actual data
        if (GetDataLength(dataItems) + dataItems.Count * 16 + 12 > MaxPDUSize)
            throw new Exception("Too much data supplied for write");
    }

    private Int32 GetDataLength(IEnumerable<DataItem> dataItems)
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

    #region IDisposable Support
    private Boolean disposedValue = false; // To detect redundant calls

    /// <summary>
    /// Dispose Plc Object
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(Boolean disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Close();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            disposedValue = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~Plc() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    void IDisposable.Dispose() =>
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);// TODO: uncomment the following line if the finalizer is overridden above.// GC.SuppressFinalize(this);
    #endregion

}
