using NewLife.Serialization;

namespace NewLife.Siemens.Messages;

/// <summary>S7 UserData参数，用于时钟读写、SZL诊断等扩展功能（Kind=0x07）</summary>
/// <remarks>
/// UserData参数块（4字节固定格式）：
///   [0]=0x00（固定，由基类读取为Code）
///   [1]=0x01（固定）
///   [2]=功能字节（0x12=时钟请求，0x82=时钟响应，0x11=SZL请求，0x81=SZL响应）
///   [3]=子功能字节（时钟：0x04=读,0x02=写；SZL：0x0E=读记录）
/// 数据块格式：[ReturnCode=0xFF] [TransportSize=0x09] [len_hi] [len_lo] [data...]
/// </remarks>
public class UserDataParameter : S7Parameter, IDataItems
{
    #region 属性
    /// <summary>固定字节，始终为0x01</summary>
    public Byte ExtraByte { get; set; } = 0x01;

    /// <summary>功能字节（0x12=时钟请求，0x82=时钟响应，0x11=SZL请求，0x81=SZL响应）</summary>
    public Byte Function { get; set; }

    /// <summary>子功能字节（时钟：0x04=读,0x02=写；SZL：0x0E=读记录）</summary>
    public Byte SubFunction { get; set; }

    /// <summary>数据块返回码（0xFF=成功）</summary>
    public Byte ReturnCode { get; set; } = 0xFF;

    /// <summary>传输大小（0x09=OCTET_STRING）</summary>
    public Byte TransportSize { get; set; } = 0x09;

    /// <summary>功能数据（读时钟响应：8字节BCD；SZL响应：SZL记录数据）</summary>
    public Byte[] Data { get; set; } = [];
    #endregion

    #region 构造
    /// <summary>实例化。Code=0x00为UserData参数首字节</summary>
    public UserDataParameter() => Code = (S7Functions)0x00;

    /// <summary>已重载。</summary>
    public override String ToString() => $"[UserData] Func=0x{Function:X2} SubFunc=0x{SubFunction:X2} DataLen={Data?.Length}";
    #endregion

    #region 参数块读写（基类已读取Code=0x00字节）
    /// <summary>读取参数内容</summary>
    /// <param name="reader"></param>
    protected override void OnRead(Binary reader)
    {
        ExtraByte = reader.ReadByte();     // [1]=0x01
        Function = reader.ReadByte();      // [2]=功能字节
        SubFunction = reader.ReadByte();   // [3]=子功能字节
    }

    /// <summary>写入参数内容</summary>
    /// <param name="writer"></param>
    protected override void OnWrite(Binary writer)
    {
        writer.WriteByte(ExtraByte);
        writer.WriteByte(Function);
        writer.WriteByte(SubFunction);
    }
    #endregion

    #region 数据块读写（IDataItems）
    /// <summary>读取数据块（FF 09 [len_hi] [len_lo] [data...]）</summary>
    /// <param name="reader"></param>
    public void ReadItems(Binary reader)
    {
        if (reader.Stream.Position >= reader.Stream.Length) return;

        ReturnCode = reader.ReadByte();
        TransportSize = reader.ReadByte();
        var len = (Int32)reader.ReadUInt16();
        Data = len > 0 ? reader.ReadBytes(len) : [];
    }

    /// <summary>写入数据块</summary>
    /// <param name="writer"></param>
    public void WriteItems(Binary writer)
    {
        if (Data == null || Data.Length == 0) return;

        writer.WriteByte(ReturnCode);
        writer.WriteByte(TransportSize);
        writer.WriteUInt16((UInt16)Data.Length);
        writer.Write(Data, 0, Data.Length);
    }
    #endregion
}
