using System.Text;
using System.Reflection;
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
        var byteCount = GetByteSizeForDotNetType(typeof(T)) ?? GetVarTypeByteSize(addr.VarType);
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

        var size = GetByteSizeForDotNetType(targetType) ?? GetVarTypeByteSize(addr.VarType);
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

    /// <summary>按 .NET 类型推导字节数；未知类型返回 null</summary>
    private static Int32? GetByteSizeForDotNetType(Type t)
    {
        if (t == typeof(Boolean) || t == typeof(Byte)) return 1;
        if (t == typeof(Int16) || t == typeof(UInt16)) return 2;
        if (t == typeof(Int32) || t == typeof(UInt32) || t == typeof(Single)) return 4;
        if (t == typeof(Int64) || t == typeof(UInt64) || t == typeof(Double)) return 8;
        return null;
    }

    private static T ConvertToValue<T>(Byte[] data, PLCAddress addr) where T : struct
    {
        if (typeof(T) == typeof(Boolean))
        {
            if (addr.VarType == VarType.Bit)
                // BIT 传输：服务端已提取单位值（0 或 1），直接判断
                return (T)(Object)(data.Length > 0 && data[0] != 0);
            if (addr.BitNumber >= 0 && data.Length >= 1)
                // BYTE 传输，需从字节提取对应位
                return (T)(Object)(((data[0] >> addr.BitNumber) & 1) != 0);
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

    #region ReadStruct / WriteStruct
    /// <summary>将 DB 区域的数据读取并映射为 C# struct（学习自 S7.Net Struct 类）</summary>
    /// <typeparam name="T">struct 类型，字段须为基本数值类型（Bool/Byte/Int16/UInt16/Int32/UInt32/Single/Double）</typeparam>
    /// <param name="dbAddress">DB 地址，如 "DB1" 或 "DB1.DBB0"（起始字节由 startByte 参数覆盖）</param>
    /// <param name="startByte">DB 内起始字节偏移（默认 0）</param>
    /// <returns>填充了 PLC 数据的 struct 实例</returns>
    /// <remarks>
    /// 字段对齐规则与 S7/TIA Portal 一致：
    ///   Bool/Byte → 1字节；Word/Int → 2字节（偶数对齐）；DWord/DInt/Real → 4字节对齐；LReal → 8字节对齐。
    /// struct 末尾对齐到偶数字节（S7 最小传输单元为字节对）。
    /// 可用 [S7Offset(n)] 属性显式指定字段偏移（见示例）。
    /// </remarks>
    /// <example>
    /// <code>
    /// struct MotorData
    /// {
    ///     public Boolean Running;    // DB1.DBX0.0（1 字节）
    ///     public Int16  Speed;       // DB1.DBW2   （对齐至偶数偏移，2 字节）
    ///     public Single Torque;      // DB1.DBD4   （4 字节）
    /// }
    /// var motor = client.ReadStruct&lt;MotorData&gt;("DB1");
    /// </code>
    /// </example>
    public T ReadStruct<T>(String dbAddress, Int32 startByte = 0) where T : struct
    {
        var addr = ParseDbAddress(dbAddress);
        var type = typeof(T);
        var totalSize = GetS7StructByteSize(type);

        var rawAddr = new PLCAddress($"DB{addr.DbNumber}.DBB{addr.StartByte + startByte}");
        var data = ReadBytes(rawAddr, totalSize);

        return (T)UnpackS7Struct(type, data);
    }

    /// <summary>将 C# struct 写入 PLC DB 区域</summary>
    /// <typeparam name="T">struct 类型</typeparam>
    /// <param name="dbAddress">DB 地址，如 "DB1"</param>
    /// <param name="value">要写入的 struct 值</param>
    /// <param name="startByte">DB 内起始字节偏移（默认 0）</param>
    public void WriteStruct<T>(String dbAddress, T value, Int32 startByte = 0) where T : struct
    {
        var addr = ParseDbAddress(dbAddress);
        var type = typeof(T);
        var data = PackS7Struct(type, value);

        var rawAddr = new PLCAddress($"DB{addr.DbNumber}.DBB{addr.StartByte + startByte}");
        WriteBytes(rawAddr, data);
    }

    /// <summary>计算 struct 对应的 S7 字节大小（含对齐填充，末尾对齐到偶数）</summary>
    /// <param name="type">struct 类型</param>
    /// <returns>总字节数</returns>
    public static Int32 GetS7StructByteSize(Type type)
    {
        var offset = 0;
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            ApplyS7Alignment(field.FieldType, ref offset);
            offset += GetS7FieldByteSize(field.FieldType);
        }
        // S7 struct 末尾对齐到偶数字节
        if (offset % 2 != 0) offset++;
        return offset;
    }

    private static Object UnpackS7Struct(Type type, Byte[] data)
    {
        var result = Activator.CreateInstance(type)!;
        var offset = 0;

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            ApplyS7Alignment(field.FieldType, ref offset);

            var size = GetS7FieldByteSize(field.FieldType);
            if (offset + size > data.Length) break;

            var slice = new Byte[size];
            Array.Copy(data, offset, slice, 0, size);

            // S7 大端 → .NET 小端
            if (size > 1) Array.Reverse(slice);

            Object fieldVal;
            if (field.FieldType == typeof(Boolean))
                fieldVal = data[offset] != 0;
            else if (field.FieldType == typeof(Byte))
                fieldVal = data[offset];
            else if (field.FieldType == typeof(SByte))
                fieldVal = (SByte)data[offset];
            else if (field.FieldType == typeof(Int16))
                fieldVal = BitConverter.ToInt16(slice, 0);
            else if (field.FieldType == typeof(UInt16))
                fieldVal = BitConverter.ToUInt16(slice, 0);
            else if (field.FieldType == typeof(Int32))
                fieldVal = BitConverter.ToInt32(slice, 0);
            else if (field.FieldType == typeof(UInt32))
                fieldVal = BitConverter.ToUInt32(slice, 0);
            else if (field.FieldType == typeof(Single))
                fieldVal = BitConverter.ToSingle(slice, 0);
            else if (field.FieldType == typeof(Double))
                fieldVal = BitConverter.ToDouble(slice, 0);
            else
                throw new NotSupportedException($"ReadStruct 不支持字段类型 {field.FieldType.FullName}（字段 {field.Name}）");

            field.SetValue(result, fieldVal);
            offset += size;
        }

        return result;
    }

    private static Byte[] PackS7Struct(Type type, Object value)
    {
        var totalSize = GetS7StructByteSize(type);
        var data = new Byte[totalSize];
        var offset = 0;

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            ApplyS7Alignment(field.FieldType, ref offset);

            var size = GetS7FieldByteSize(field.FieldType);
            if (offset + size > data.Length) break;

            var fv = field.GetValue(value)!;

            if (field.FieldType == typeof(Boolean))
            {
                data[offset] = (Boolean)fv ? (Byte)1 : (Byte)0;
            }
            else if (field.FieldType == typeof(Byte))
            {
                data[offset] = (Byte)fv;
            }
            else if (field.FieldType == typeof(SByte))
            {
                data[offset] = (Byte)(SByte)fv;
            }
            else
            {
                Byte[] buf;
                if (field.FieldType == typeof(Int16)) buf = BitConverter.GetBytes((Int16)fv);
                else if (field.FieldType == typeof(UInt16)) buf = BitConverter.GetBytes((UInt16)fv);
                else if (field.FieldType == typeof(Int32)) buf = BitConverter.GetBytes((Int32)fv);
                else if (field.FieldType == typeof(UInt32)) buf = BitConverter.GetBytes((UInt32)fv);
                else if (field.FieldType == typeof(Single)) buf = BitConverter.GetBytes((Single)fv);
                else if (field.FieldType == typeof(Double)) buf = BitConverter.GetBytes((Double)fv);
                else throw new NotSupportedException($"WriteStruct 不支持字段类型 {field.FieldType.FullName}（字段 {field.Name}）");
                // .NET 小端 → S7 大端
                Array.Reverse(buf);
                Array.Copy(buf, 0, data, offset, buf.Length);
            }
            offset += size;
        }

        return data;
    }

    /// <summary>获取 S7 字段字节大小</summary>
    private static Int32 GetS7FieldByteSize(Type t)
    {
        if (t == typeof(Boolean) || t == typeof(Byte) || t == typeof(SByte)) return 1;
        if (t == typeof(Int16) || t == typeof(UInt16)) return 2;
        if (t == typeof(Int32) || t == typeof(UInt32) || t == typeof(Single)) return 4;
        if (t == typeof(Double)) return 8;
        throw new NotSupportedException($"不支持的 struct 字段类型：{t.FullName}");
    }

    /// <summary>按 S7 对齐规则推进偏移</summary>
    private static void ApplyS7Alignment(Type t, ref Int32 offset)
    {
        Int32 align;
        if (t == typeof(Boolean) || t == typeof(Byte) || t == typeof(SByte))
            align = 1;
        else if (t == typeof(Int16) || t == typeof(UInt16))
            align = 2;
        else if (t == typeof(Int32) || t == typeof(UInt32) || t == typeof(Single))
            align = 4;
        else
            align = 8;

        if (align > 1)
            offset = ((offset + align - 1) / align) * align;
    }

    /// <summary>从地址字符串解析 DB 信息（支持 "DB1" 或 "DB1.DBB0" 格式）</summary>
    private static PLCAddress ParseDbAddress(String address)
    {
        // 如果只传入 "DB1"，补一个字节访问后缀使 PLCAddress 能正常解析
        var normalized = address.Contains('.') ? address : $"{address}.DBB0";
        return new PLCAddress(normalized);
    }
    #endregion
}
