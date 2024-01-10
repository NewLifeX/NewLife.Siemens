using System.Net.Sockets;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Siemens.Messages;
using NewLife.Siemens.Models;

namespace NewLife.Siemens.Protocols;

/// <summary>S7驱动</summary>
public partial class S7Client : DisposeBase, ILogFeature
{
    #region 属性
    /// <summary>IP地址</summary>
    public String IP { get; set; }

    /// <summary>端口</summary>
    public Int32 Port { get; set; } = 102;

    /// <summary>超时时间。默认5000毫秒</summary>
    public Int32 Timeout { get; set; } = 5_000;

    /// <summary>类型</summary>
    public CpuType CPU { get; set; }

    /// <summary>机架号。通常为0</summary>
    public Int16 Rack { get; set; }

    /// <summary>插槽，对于S7300-S7400通常为2，对于S7-1200和S7-1500为0</summary>
    public Int16 Slot { get; set; }

    /// <summary>最大PDU大小</summary>
    public Int32 MaxPDUSize { get; private set; } = 1024;

    private TcpClient? _client;
    private NetworkStream? _stream;
    private Int32 _sequence;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="cpu"></param>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    /// <param name="rack"></param>
    /// <param name="slot"></param>
    public S7Client(CpuType cpu, String ip, Int32 port, Int16 rack = 0, Int16 slot = 0)
    {
        IP = ip;
        if (port > 0) Port = port;

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

        // 开启KeepAlive，避免长时间空闲时，路由器或防火墙关闭连接
        client.Client.SetTcpKeepAlive(true, 15_000, 15_000);

        await client.ConnectAsync(IP, Port).ConfigureAwait(false);

        var stream = client.GetStream();
        _stream = stream;
        _client = client;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RequestConnection(stream, cancellationToken).ConfigureAwait(false);
            await SetupConnection(stream, cancellationToken).ConfigureAwait(false);
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
        var request = new COTP
        {
            Type = PduType.ConnectionRequest,
            Destination = 0x00,
            Source = 0x01,
            Option = 0x00,
        };
        request.SetParameter(COTPParameterKinds.SrcTsap, tsap.Local);
        request.SetParameter(COTPParameterKinds.DstTsap, tsap.Remote);
        request.SetParameter(COTPParameterKinds.TpduSize, (Byte)0x0A);

        var response = await RequestAsync(stream, request, cancellationToken).ConfigureAwait(false);

        if (response.Type != PduType.ConnectionConfirmed)
            throw new InvalidDataException($"Connection request was denied (PDUType={response.Type})");
    }

    private async Task SetupConnection(Stream stream, CancellationToken cancellationToken)
    {
        var setup = new SetupMessage
        {
            MaxAmqCaller = 0x0001,
            MaxAmqCallee = 0x0001,
            PduLength = 960,
        };

        var rs = await InvokeAsync(setup, cancellationToken).ConfigureAwait(false);
        if (rs == null) return;

        if (rs is SetupMessage pm) MaxPDUSize = pm.PduLength;
    }

    /// <summary>获取网络流，检查并具备断线重连能力</summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<Stream> GetStream(CancellationToken cancellationToken)
    {
        if (_stream != null && _client != null && _client.Connected) return _stream;

        await OpenAsync(cancellationToken).ConfigureAwait(false);

        return _stream!;
    }

    /// <summary>关闭连接</summary>
    public void Close()
    {
        _client?.Close();
        _client = null;
        _stream = null;
    }
    #endregion

    #region 核心方法
    /// <summary>发起S7请求</summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<S7Message?> RequestAsync(S7Message request, CancellationToken cancellationToken = default)
    {
        var stream = await GetStream(cancellationToken);

        // 设置递增的序列号
        if (request.Sequence == 0) request.Sequence = (UInt16)Interlocked.Increment(ref _sequence);

        WriteLog("=> {0}", request);

        var cotp = await RequestAsync(stream, request.ToCOTP(), cancellationToken).ConfigureAwait(false);
        if (cotp == null || cotp.Data == null) return null;

        var msg = new S7Message();
        if (!msg.Read(cotp.Data)) return null;

        WriteLog("<= {0}", msg);

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
            if (request.Type != PduType.Data) WriteLog("=> {0}", request);

            var pk = request.ToPacket(true);
            var buf = pk.ReadBytes();

            using var closeOnCancellation = cancellationToken.Register(Close);
            //await pk.CopyToAsync(stream, cancellationToken);
            await stream.WriteAsync(buf, 0, buf.Length, cancellationToken);
            var rs = await COTP.ReadAsync(stream, cancellationToken).ConfigureAwait(false);

            if (request.Type != PduType.Data) WriteLog("<= {0}", rs);

            return rs;
        }
        catch (Exception exc)
        {
            if (exc is InvalidDataException)
            {
                Close();
            }

            throw;
        }
    }
    #endregion

    #region 读取
    /// <summary>读取多个字节</summary>
    /// <param name="address"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public Byte[] ReadBytes(PLCAddress address, Int32 count)
    {
        var ms = new MemoryStream();
        var index = 0;
        while (count > 0)
        {
            // 最大PDU大小
            var maxToRead = Math.Min(count, MaxPDUSize - 18);

            var addr = (address.StartByte + index) * 8;
            if (address.BitNumber > 0) addr += address.BitNumber;
            var request = BuildRead(address.DataType, address.DbNumber, address.VarType, addr, maxToRead);

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

    private static ReadRequest BuildRead(DataType dataType, Int32 db, VarType varType, Int32 address, Int32 count)
    {
        var ri = new RequestItem
        {
            // S7ANY
            SyntaxId = 0x10,
            TransportSize = (Byte)(varType == VarType.Bit ? 1 : 2),
            Count = (UInt16)count,
            DbNumber = (UInt16)db,
            Area = dataType,

            Address = (UInt32)address,
        };

        var request = new ReadRequest();
        request.Items.Add(ri);

        return request;
    }
    #endregion

    #region 写入
    /// <summary>从指定DB开始，写入多个字节</summary>
    /// <param name="address"></param>
    /// <param name="value"></param>
    public void WriteBytes(PLCAddress address, Byte[] value)
    {
        var index = 0;
        var count = value.Length;
        while (count > 0)
        {
            var pdu = Math.Min(count, MaxPDUSize - 28);

            var addr = (address.StartByte + index) * 8;
            if (address.BitNumber > 0) addr += address.BitNumber;
            var request = BuildWrite(address.DataType, address.DbNumber, address.VarType, addr, value, index, pdu);

            // 发起请求
            var rs = InvokeAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
            if (rs is not WriteResponse res) break;

            if (res.Items != null && res.Items.Count > 0)
            {
                var code = res.Items[0].Code;
                if (code != ReadWriteErrorCode.Success) throw new ApiException((Int32)code, code + "");
            }

            count -= pdu;
            index += pdu;
        }
    }

    private WriteRequest BuildWrite(DataType dataType, Int32 db, VarType varType, Int32 address, Byte[] value, Int32 offset, Int32 count)
    {
        var request = new WriteRequest();
        request.Items.Add(new RequestItem
        {
            SpecType = 0x12,
            SyntaxId = 0x10,
            TransportSize = (Byte)(varType == VarType.Bit ? 1 : 2),
            Count = (UInt16)count,
            DbNumber = (UInt16)db,
            Area = dataType,
            Address = (UInt32)address
        });
        request.DataItems.Add(new DataItem
        {
            TransportSize = (Byte)(varType == VarType.Bit ? 0x03 : 0x04),
            Data = value.ReadBytes(offset, count),
        });

        return request;
    }
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object[] args) => Log.Info(format, args);
    #endregion
}
