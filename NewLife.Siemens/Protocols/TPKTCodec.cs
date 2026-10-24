using System.Buffers;
using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Siemens.Protocols;

/// <summary>TPKT 编解码器。帧格式：4 字节头（版本 + 保留 + 2 字节大端长度，长度含头部）+ 负载</summary>
/// <remarks>
/// <para>协议实例无状态，可跨连接共享。TPKT 头部长度字段确定帧边界，因此帧未到齐时声明 <see cref="ParseResult.NeedFullFrame"/>，
/// 由帧泵等整帧到齐后一次性绑定内存体，避免大帧走流式体。</para>
/// <para>S7 链路不做序列号复用应答配对，<see cref="Match"/> 恒为 true。</para>
/// </remarks>
public class TPKTCodec : IMessageCodec, IMessageMatcher
{
    #region 编解码
    /// <summary>定界并构造消息。解析 4 字节头部，构造头部字段就位的 <see cref="TPKT"/></summary>
    /// <param name="buffer">帧首窗口</param>
    /// <returns>解析结果；头部不足返回 null，长度非法返回损坏帧</returns>
    public ParseResult? TryParse(ReadOnlySequence<Byte> buffer)
    {
        if (buffer.Length < 4) return null;

        Span<Byte> head = stackalloc Byte[4];
        buffer.Slice(0, 4).CopyTo(head);

        var length = (head[2] << 8) | head[3];

        // 长度字段含头部，小于 4 说明帧头已损坏
        if (length < 4) return new ParseResult { Invalid = true, HeaderSize = 4 };

        var bodyLength = length - 4;

        // 整帧未到齐：已定界但需要等整帧，避免上层拿到流式体
        if (buffer.Length < length) return new ParseResult { HeaderSize = 4, BodyLength = bodyLength, NeedFullFrame = true };

        var msg = new TPKT
        {
            Version = head[0],
            Reserved = head[1],
            Length = (UInt16)length,
        };

        return new ParseResult { Message = msg, HeaderSize = 4, BodyLength = bodyLength };
    }

    /// <summary>整帧构建（头 + 内存体）。构建不消费消息负载</summary>
    /// <param name="message">TPKT 消息</param>
    /// <returns>整帧拥有句柄，调用方负责 Dispose</returns>
    /// <exception cref="ArgumentException">消息类型不是 <see cref="TPKT"/></exception>
    /// <exception cref="InvalidOperationException">消息体为流式模式，或负载超过 TPKT 长度上限</exception>
    public IOwnerPacket? Build(IMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (message is not TPKT tpkt) throw new ArgumentException($"消息类型[{message.GetType().FullName}]不是TPKT帧", nameof(message));

        if (tpkt.Body is { IsStreaming: true }) throw new InvalidOperationException("流式消息体无法整帧构建，请改用 BuildHeader 配合流式发送");

        var body = tpkt.Payload;
        var len = body?.Total ?? 0;
        if (len > UInt16.MaxValue - 4) throw new InvalidOperationException($"负载长度[{len}]超过TPKT帧上限");

        // 已预留的负载零拷贝借位共享，其余以新头节点挂接负载链
        var pk = body.PrepareHeader(4);
        WriteHeader(tpkt, pk.GetSpan(), len);

        return pk;
    }

    /// <summary>仅构建头部数据包，声明消息体长度</summary>
    /// <param name="message">TPKT 消息</param>
    /// <param name="bodyLength">消息体字节数</param>
    /// <returns>头部拥有句柄，调用方负责 Dispose</returns>
    /// <exception cref="ArgumentException">消息类型不是 <see cref="TPKT"/></exception>
    /// <exception cref="ArgumentOutOfRangeException">长度超出 TPKT 帧上限</exception>
    public IOwnerPacket BuildHeader(IMessage message, Int64 bodyLength)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (message is not TPKT tpkt) throw new ArgumentException($"消息类型[{message.GetType().FullName}]不是TPKT帧", nameof(message));

        if (bodyLength < 0 || bodyLength > UInt16.MaxValue - 4) throw new ArgumentOutOfRangeException(nameof(bodyLength), $"消息体长度[{bodyLength}]超过TPKT帧上限");

        var pk = new OwnerPacket(4);
        WriteHeader(tpkt, pk.GetSpan(), (Int32)bodyLength);

        return pk;
    }

    /// <summary>判断响应是否匹配请求。S7 链路不支持链路复用，任意响应都是匹配的</summary>
    /// <param name="request">挂起的请求消息</param>
    /// <param name="response">收到的响应消息</param>
    /// <returns>恒为 true</returns>
    public Boolean Match(IMessage request, IMessage response) => true;
    #endregion

    #region 辅助
    /// <summary>写入 TPKT 头部</summary>
    /// <param name="tpkt">消息</param>
    /// <param name="span">头部空间</param>
    /// <param name="bodyLength">负载字节数</param>
    private static void WriteHeader(TPKT tpkt, Span<Byte> span, Int32 bodyLength)
    {
        var length = (UInt16)(4 + bodyLength);
        tpkt.Length = length;

        span[0] = tpkt.Version;
        span[1] = tpkt.Reserved;
        span[2] = (Byte)(length >> 8);
        span[3] = (Byte)(length & 0xFF);
    }
    #endregion
}