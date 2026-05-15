namespace NewLife.Siemens.Protocols;

/// <summary>PLC 程序块类型</summary>
/// <remarks>
/// 值对应 Siemens 文件上传/下载协议中的块类型字节（ASCII 字符含义）：
///   OB='8'(0x38)  DB='A'(0x41)  SDB='B'(0x42)  FC='C'(0x43)  SFC='D'(0x44)  FB='E'(0x45)  SFB='F'(0x46)
/// </remarks>
public enum S7BlockType : Byte
{
    /// <summary>组织块（Organization Block）'8'</summary>
    OB = 0x38,

    /// <summary>数据块（Data Block）'A'</summary>
    DB = 0x41,

    /// <summary>系统数据块（System Data Block）'B'</summary>
    SDB = 0x42,

    /// <summary>功能（Function）'C'</summary>
    FC = 0x43,

    /// <summary>系统功能（System Function）'D'</summary>
    SFC = 0x44,

    /// <summary>功能块（Function Block）'E'</summary>
    FB = 0x45,

    /// <summary>系统功能块（System Function Block）'F'</summary>
    SFB = 0x46,
}

/// <summary>PLC 程序块信息</summary>
/// <remarks>通过 SZL 0x0022 获取的块元数据</remarks>
public class S7BlockInfo
{
    #region 属性
    /// <summary>块类型</summary>
    public S7BlockType BlockType { get; set; }

    /// <summary>块编号</summary>
    public Int32 BlockNumber { get; set; }

    /// <summary>块标志（SZL 记录的原始标志字节）</summary>
    public Byte Flags { get; set; }

    /// <summary>块语言代码（SZL 原始值）</summary>
    public Byte Language { get; set; }
    #endregion

    /// <summary>已重载</summary>
    public override String ToString() => $"{BlockType}{BlockNumber} Flags=0x{Flags:X2}";

    /// <summary>从 SZL 0x0022 响应数据解析块列表</summary>
    /// <param name="data">ReadSzlAsync 返回的原始字节</param>
    /// <returns>块信息列表</returns>
    /// <remarks>
    /// SZL 0x0022 每条记录 4 字节：
    ///   [0..1] = 块编号（大端 UInt16）
    ///   [2]    = 标志字节
    ///   [3]    = 语言代码
    /// 记录前有 8 字节 SZL 头（szlId + szlIndex + recLen + recCount）。
    /// </remarks>
    public static S7BlockInfo[] ParseFromSzl0022(Byte[] data, S7BlockType blockType)
    {
        if (data == null || data.Length < 8) return [];

        // SZL 头：[0-1]=szlId [2-3]=szlIndex [4-5]=recLen [6-7]=recCount
        var recLen = (Int32)((data[4] << 8) | data[5]);
        var recCount = (Int32)((data[6] << 8) | data[7]);

        if (recLen < 4 || recCount <= 0) return [];

        var result = new List<S7BlockInfo>(recCount);
        var pos = 8;
        for (var i = 0; i < recCount && pos + recLen <= data.Length; i++, pos += recLen)
        {
            var blockNum = (Int32)((data[pos] << 8) | data[pos + 1]);
            result.Add(new S7BlockInfo
            {
                BlockType = blockType,
                BlockNumber = blockNum,
                Flags = data[pos + 2],
                Language = data[pos + 3],
            });
        }

        return [.. result];
    }
}
