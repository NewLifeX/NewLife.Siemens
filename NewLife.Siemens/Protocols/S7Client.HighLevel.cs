using System.Text;
using NewLife.Siemens.Models;

// 兼容 net45/netstandard2.0：不能使用 Encoding.Latin1 静态属性

namespace NewLife.Siemens.Protocols;

/// <summary>S7客户端 — 高层泛型读写 API</summary>
public partial class S7Client
{
    #region 泛型读取
    /// <summary>读取指定地址的值，自动完成大端↔小端转换</summary>
    /// <typeparam name="T">目标 .NET 类型（Boolean/Byte/Int16/UInt16/Int32/UInt32/Single/Double）</typeparam>
    /// <param name="address">PLC 地址字符串，如 "DB1.DBW0"、"MW10"</param>
    /// <returns>转换后的值</returns>
    public T Read<T>(String address) where T : struct
    {
        var addr = new PLCAddress(address);
        var byteCount = GetVarTypeByteSize(addr.VarType);
        var data = ReadBytes(addr, byteCount);
        return ConvertToValue<T>(data, addr);
    }

    /// <summary>读取字符串地址的值</summary>
    /// <param name="address">PLC 地址字符串</param>
    /// <param name="targetType">目标 .NET 类型</param>
    /// <returns>转换后的值（装箱）</returns>
    public Object? Read(String address, Type targetType)
    {
        var addr = new PLCAddress(address);

        if (targetType == typeof(String))
        {
            var byteCount = addr.BitNumber > 0 ? addr.BitNumber + 2 : 256;
            var raw = ReadBytes(addr, byteCount);
            return DecodeS7String(raw);
        }

        var size = GetVarTypeByteSize(addr.VarType);
        var data = ReadBytes(addr, size);

        if (targetType == typeof(Boolean)) return ConvertToValue<Boolean>(data, addr);
        if (targetType == typeof(Byte)) return ConvertToValue<Byte>(data, addr);
        if (targetType == typeof(Int16)) return ConvertToValue<Int16>(data, addr);
        if (targetType == typeof(UInt16)) return ConvertToValue<UInt16>(data, addr);
        if (targetType == typeof(Int32)) return ConvertToValue<Int32>(data, addr);
        if (targetType == typeof(UInt32)) return ConvertToValue<UInt32>(data, addr);
        if (targetType == typeof(Single)) return ConvertToValue<Single>(data, addr);
        if (targetType == typeof(Double)) return ConvertToValue<Double>(data, addr);

        throw new NotSupportedException($"不支持的目标类型：{targetType.FullName}");
    }

    /// <summary>读取 S7 STRING 字符串</summary>
    /// <param name="address">PLC STRING 地址，如 "DB1.STRING0.60"</param>
    /// <param name="maxLength">字符串最大长度（若地址中已包含则可省略）</param>
    /// <returns>字符串内容</returns>
    public String ReadString(String address, Int32 maxLength = 0)
    {
        var addr = new PLCAddress(address);
        // BitNumber 存储了 STRING 地址中的最大长度
        var len = maxLength > 0 ? maxLength : (addr.BitNumber > 0 ? addr.BitNumber : 254);
        var raw = ReadBytes(addr, len + 2);
        return DecodeS7String(raw);
    }

    /// <summary>读取数组（按 VarType 步长连续读取）</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="address">起始地址</param>
    /// <param name="count">元素个数</param>
    /// <returns>数组</returns>
    public T[] ReadArray<T>(String address, Int32 count) where T : struct
    {
        if (count <= 0) return [];

        var addr = new PLCAddress(address);
        var elementSize = GetVarTypeByteSize(addr.VarType);
        var totalBytes = elementSize * count;
        var raw = ReadBytes(addr, totalBytes);

        var result = new T[count];
        for (var i = 0; i < count; i++)
        {
            var slice = new Byte[elementSize];
            Array.Copy(raw, i * elementSize, slice, 0, elementSize);
            result[i] = ConvertToValue<T>(slice, addr);
        }
        return result;
    }
    #endregion

    #region 泛型写入
    /// <summary>写入值到指定地址，自动完成类型转换和字节序处理</summary>
    /// <param name="address">PLC 地址字符串</param>
    /// <param name="value">要写入的值</param>
    public void Write(String address, Object value)
    {
        var addr = new PLCAddress(address);
        var data = ConvertToBytes(value, addr.VarType);
        WriteBytes(addr, data);
    }

    /// <summary>写入 S7 STRING 字符串</summary>
    /// <param name="address">PLC STRING 地址</param>
    /// <param name="value">字符串内容</param>
    /// <param name="maxLength">最大长度（默认 254）</param>
    public void WriteString(String address, String value, Int32 maxLength = 0)
    {
        var addr = new PLCAddress(address);
        var maxLen = maxLength > 0 ? maxLength : (addr.BitNumber > 0 ? addr.BitNumber : 254);
        var data = EncodeS7String(value, maxLen);
        WriteBytes(addr, data);
    }
    #endregion

    #region 内部工具
    /// <summary>获取 VarType 对应的字节数</summary>
    public static Int32 GetVarTypeByteSize(VarType varType) => varType switch
    {
        VarType.Bit => 1,
        VarType.Byte => 1,
        VarType.Word => 2,
        VarType.DWord => 4,
        VarType.Int => 2,
        VarType.DInt => 4,
        VarType.Real => 4,
        VarType.LReal => 8,
        VarType.DateTime => 8,
        VarType.Timer => 2,
        VarType.Counter => 2,
        _ => throw new NotSupportedException($"不支持的 VarType：{varType}"),
    };

    private static T ConvertToValue<T>(Byte[] data, PLCAddress addr) where T : struct
    {
        if (typeof(T) == typeof(Boolean))
        {
            if (addr.BitNumber >= 0 && data.Length >= 1)
            {
                // 对位地址（如 M0.0），ReadBytes 返回的是原始字节，需提取对应位
                // 若 BitNumber=-1 表示直接按位传输（BIT TransportSize）
                if (addr.VarType == VarType.Bit && addr.BitNumber < 0)
                    return (T)(Object)(data[0] != 0);
                var bitVal = (data[0] >> addr.BitNumber) & 1;
                return (T)(Object)(bitVal != 0);
            }
            return (T)(Object)(data.Length > 0 && data[0] != 0);
        }

        // 大端 → 小端（S7 协议为大端）
        var buf = (Byte[])data.Clone();
        if (buf.Length > 1) Array.Reverse(buf);

        if (typeof(T) == typeof(Byte)) return (T)(Object)(buf.Length > 0 ? buf[0] : (Byte)0);
        if (typeof(T) == typeof(Int16) && buf.Length >= 2) return (T)(Object)BitConverter.ToInt16(buf, 0);
        if (typeof(T) == typeof(UInt16) && buf.Length >= 2) return (T)(Object)BitConverter.ToUInt16(buf, 0);
        if (typeof(T) == typeof(Int32) && buf.Length >= 4) return (T)(Object)BitConverter.ToInt32(buf, 0);
        if (typeof(T) == typeof(UInt32) && buf.Length >= 4) return (T)(Object)BitConverter.ToUInt32(buf, 0);
        if (typeof(T) == typeof(Single) && buf.Length >= 4) return (T)(Object)BitConverter.ToSingle(buf, 0);
        if (typeof(T) == typeof(Double) && buf.Length >= 8) return (T)(Object)BitConverter.ToDouble(buf, 0);

        throw new NotSupportedException($"不支持的类型：{typeof(T).FullName}");
    }

    private static Byte[] ConvertToBytes(Object value, VarType varType)
    {
        Byte[] buf;
        switch (value)
        {
            case Boolean bv:
                return [bv ? (Byte)1 : (Byte)0];
            case Byte byt:
                return [byt];
            case Int16 i16:
                buf = BitConverter.GetBytes(i16);
                break;
            case UInt16 u16:
                buf = BitConverter.GetBytes(u16);
                break;
            case Int32 i32:
                buf = BitConverter.GetBytes(i32);
                break;
            case UInt32 u32:
                buf = BitConverter.GetBytes(u32);
                break;
            case Single f32:
                buf = BitConverter.GetBytes(f32);
                break;
            case Double f64:
                buf = BitConverter.GetBytes(f64);
                break;
            case String str:
                return EncodeS7String(str, 254);
            default:
                throw new NotSupportedException($"不支持的值类型：{value.GetType().FullName}");
        }

        // 小端 → 大端
        Array.Reverse(buf);
        return buf;
    }

    private static readonly Encoding _latin1 = Encoding.GetEncoding("ISO-8859-1");

    private static String DecodeS7String(Byte[] raw)
    {
        if (raw.Length < 2) return String.Empty;
        var actualLen = Math.Min((Int32)raw[1], raw.Length - 2);
        if (actualLen <= 0) return String.Empty;
        return _latin1.GetString(raw, 2, actualLen);
    }

    private static Byte[] EncodeS7String(String value, Int32 maxLength)
    {
        var bytes = _latin1.GetBytes(value ?? String.Empty);
        var actualLen = Math.Min(bytes.Length, maxLength);
        var buf = new Byte[maxLength + 2];
        buf[0] = (Byte)maxLength;
        buf[1] = (Byte)actualLen;
        Array.Copy(bytes, 0, buf, 2, actualLen);
        return buf;
    }
    #endregion
}
