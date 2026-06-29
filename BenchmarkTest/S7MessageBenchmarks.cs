using BenchmarkDotNet.Attributes;
using NewLife.Siemens.Messages;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;

namespace BenchmarkTest;

/// <summary>S7 消息序列化/反序列化基准测试</summary>
/// <remarks>
/// 测试 S7 协议报文的读（解析）写（构造）性能。
/// S7Message 是每次读写请求的载体，序列化性能影响高频采集吞吐。
/// </remarks>
[MemoryDiagnoser]
[ShortRunJob]
public class S7MessageBenchmarks
{
    private Byte[] _readRequestBytes = [];
    private Byte[] _writeRequestBytes = [];
    private Byte[] _setupResponseBytes = [];
    private Byte[] _gtmSetupResponseBytes = [];

    [GlobalSetup]
    public void Setup()
    {
        // 构造 ReadRequest 消息字节（单变量读 DB1.DBW0）
        var readReq = new S7Message { Kind = S7Kinds.Job };
        var readParam = new ReadRequest();
        readParam.Items.Add(new RequestItem
        {
            SpecType = 0x12,
            SyntaxId = 0x10,
            TransportSize = 0x02,
            Count = 1,
            DbNumber = 1,
            Area = DataType.DataBlock,
            Address = 0,
        });
        readReq.SetParameter(readParam);
        _readRequestBytes = readReq.ToCOTP().ToPacket(true).ReadBytes();

        // 构造 WriteRequest 消息字节
        var writeReq = new S7Message { Kind = S7Kinds.Job };
        var writeParam = new WriteRequest();
        writeParam.Items.Add(new RequestItem
        {
            SpecType = 0x12,
            SyntaxId = 0x10,
            TransportSize = 0x02,
            Count = 1,
            DbNumber = 1,
            Area = DataType.DataBlock,
            Address = 0,
        });
        writeParam.DataItems.Add(new DataItem { Code = ReadWriteErrorCode.Success, TransportSize = 0x04, Data = [0x01, 0x02] });
        writeReq.SetParameter(writeParam);
        _writeRequestBytes = writeReq.ToCOTP().ToPacket(true).ReadBytes();

        // 构造标准 Setup 响应（8字节参数）
        var setupRsp = new S7Message { Kind = S7Kinds.AckData };
        setupRsp.SetParameter(new SetupMessage { MaxAmqCaller = 0x0001, MaxAmqCallee = 0x0001, PduLength = 480 });
        _setupResponseBytes = setupRsp.ToCOTP().ToPacket(true).ReadBytes();

        // 构造 GTM Setup 响应（13字节参数）
        var gtmSetupRsp = new S7Message { Kind = S7Kinds.AckData };
        gtmSetupRsp.SetParameter(new SetupMessage
        {
            MaxAmqCaller = 0x0001,
            MaxAmqCallee = 0x0001,
            PduLength = 480,
            HasGtm = true,
            GtmData = [0x00, 0x00, 0x00, 0x01, 0x00],
        });
        _gtmSetupResponseBytes = gtmSetupRsp.ToCOTP().ToPacket(true).ReadBytes();
    }

    /// <summary>构造 ReadRequest 消息（含 TPKT+COTP+S7+ReadRequestItem）</summary>
    [Benchmark]
    public Byte[] Build_ReadRequest()
    {
        var msg = new S7Message { Kind = S7Kinds.Job };
        var readParam = new ReadRequest();
        readParam.Items.Add(new RequestItem
        {
            SpecType = 0x12,
            SyntaxId = 0x10,
            TransportSize = 0x02,
            Count = 1,
            DbNumber = 1,
            Area = DataType.DataBlock,
            Address = 0,
        });
        msg.SetParameter(readParam);
        return msg.ToCOTP().ToPacket(true).ReadBytes();
    }

    /// <summary>构造 GTM Setup 请求（13字节参数）</summary>
    [Benchmark]
    public Byte[] Build_GTM_SetupRequest()
    {
        var msg = new S7Message { Kind = S7Kinds.Job };
        msg.SetParameter(new SetupMessage
        {
            MaxAmqCaller = 0x0001,
            MaxAmqCallee = 0x0001,
            PduLength = 960,
            HasGtm = true,
            GtmData = [0x00, 0x00, 0x00, 0x00, 0x00],
        });
        return msg.ToCOTP().ToPacket(true).ReadBytes();
    }

    /// <summary>解析 ReadRequest 响应（从 TPKT 帧到 S7Message）</summary>
    [Benchmark]
    public S7Message Parse_ReadResponse()
    {
        var tpkt = new TPKT();
        tpkt.Read(_readRequestBytes); // 复用同样结构的字节
        var cotp = new COTP();
        cotp.Read(tpkt.Data!);
        var msg = new S7Message();
        msg.Read(cotp.Data!);
        return msg;
    }

    /// <summary>解析 Setup 响应（标准8字节参数）</summary>
    [Benchmark]
    public S7Message Parse_SetupResponse()
    {
        var tpkt = new TPKT();
        tpkt.Read(_setupResponseBytes);
        var cotp = new COTP();
        cotp.Read(tpkt.Data!);
        var msg = new S7Message();
        msg.Read(cotp.Data!);
        return msg;
    }

    /// <summary>解析 GTM Setup 响应（13字节参数含 GTM 扩展）</summary>
    [Benchmark]
    public S7Message Parse_GTM_SetupResponse()
    {
        var tpkt = new TPKT();
        tpkt.Read(_gtmSetupResponseBytes);
        var cotp = new COTP();
        cotp.Read(tpkt.Data!);
        var msg = new S7Message();
        msg.Read(cotp.Data!);
        return msg;
    }
}
