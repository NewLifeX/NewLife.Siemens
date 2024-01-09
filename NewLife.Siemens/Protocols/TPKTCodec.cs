using NewLife.Data;
using NewLife.Messaging;
using NewLife.Model;
using NewLife.Net.Handlers;

namespace NewLife.Siemens.Protocols;

/// <summary>编码器</summary>
public class TPKTCodec : MessageCodec<TPKT>
{
    /// <summary>编码</summary>
    /// <param name="context"></param>
    /// <param name="msg"></param>
    /// <returns></returns>
    protected override Object? Encode(IHandlerContext context, TPKT msg)
    {
        if (msg is TPKT cmd) return cmd.ToPacket();

        return null;
    }

    /// <summary>解码</summary>
    /// <param name="context"></param>
    /// <param name="pk"></param>
    /// <returns></returns>
    protected override IList<TPKT>? Decode(IHandlerContext context, Packet pk)
    {
        if (context.Owner is not IExtend ss) return null;

        if (ss["Codec"] is not PacketCodec pc)
            ss["Codec"] = pc = new PacketCodec { GetLength = p => GetLength(p, 3, 1) - 4, Offset = 3 };

        var pks = pc.Parse(pk);
        var list = pks.Select(e => new TPKT().Read(e)).ToList();

        return list;
    }

    /// <summary>连接关闭时，清空粘包编码器</summary>
    /// <param name="context"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    public override Boolean Close(IHandlerContext context, String reason)
    {
        if (context.Owner is IExtend ss) ss["Codec"] = null;

        return base.Close(context, reason);
    }

    /// <summary>是否匹配响应</summary>
    /// <param name="request"></param>
    /// <param name="response"></param>
    /// <returns></returns>
    protected override Boolean IsMatch(Object? request, Object? response)
    {
        if (request is not TPKT || response is not TPKT) return false;

        // 不支持链路复用，任意响应都是匹配的

        return true;
    }
}