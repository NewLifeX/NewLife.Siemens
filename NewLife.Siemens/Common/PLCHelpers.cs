using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using NewLife.Siemens.Types;
using DateTime = NewLife.Siemens.Types.DateTime;

namespace NewLife.Siemens.Common;

public partial class Plc
{
    /// <summary>
    /// Creates the header to read bytes from the PLC
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    private static void BuildHeaderPackage(MemoryStream stream, Int32 amount = 1)
    {
        //header size = 19 bytes
        stream.WriteByteArray(new System.Byte[] { 0x03, 0x00 });
        //complete package size
        stream.WriteByteArray(Int.ToByteArray((Int16)(19 + 12 * amount)));
        stream.WriteByteArray(new System.Byte[] { 0x02, 0xf0, 0x80, 0x32, 0x01, 0x00, 0x00, 0x00, 0x00 });
        //data part size
        stream.WriteByteArray(Word.ToByteArray((UInt16)(2 + amount * 12)));
        stream.WriteByteArray(new System.Byte[] { 0x00, 0x00, 0x04 });
        //amount of requests
        stream.WriteByte((System.Byte)amount);
    }

    /// <summary>
    /// Create the bytes-package to request data from the PLC. You have to specify the memory type (dataType),
    /// the address of the memory, the address of the byte and the bytes count.
    /// </summary>
    /// <param name="dataType">MemoryType (DB, Timer, Counter, etc.)</param>
    /// <param name="db">Address of the memory to be read</param>
    /// <param name="startByteAdr">Start address of the byte</param>
    /// <param name="count">Number of bytes to be read</param>
    /// <returns></returns>
    private static void BuildReadDataRequestPackage(MemoryStream stream, DataType dataType, Int32 db, Int32 startByteAdr, Int32 count = 1)
    {
        //single data req = 12
        stream.WriteByteArray(new System.Byte[] { 0x12, 0x0a, 0x10 });
        switch (dataType)
        {
            case DataType.Timer:
            case DataType.Counter:
                stream.WriteByte((System.Byte)dataType);
                break;
            default:
                stream.WriteByte(0x02);
                break;
        }

        stream.WriteByteArray(Word.ToByteArray((UInt16)count));
        stream.WriteByteArray(Word.ToByteArray((UInt16)db));
        stream.WriteByte((System.Byte)dataType);
        var overflow = (Int32)(startByteAdr * 8 / 0xffffU); // handles words with address bigger than 8191
        stream.WriteByte((System.Byte)overflow);
        switch (dataType)
        {
            case DataType.Timer:
            case DataType.Counter:
                stream.WriteByteArray(Word.ToByteArray((UInt16)startByteAdr));
                break;
            default:
                stream.WriteByteArray(Word.ToByteArray((UInt16)(startByteAdr * 8)));
                break;
        }
    }

    /// <summary>
    /// Given a S7 variable type (Bool, Word, DWord, etc.), it converts the bytes in the appropriate C# format.
    /// </summary>
    /// <param name="varType"></param>
    /// <param name="bytes"></param>
    /// <param name="varCount"></param>
    /// <param name="bitAdr"></param>
    /// <returns></returns>
    private Object ParseBytes(VarType varType, System.Byte[] bytes, Int32 varCount, System.Byte bitAdr = 0)
    {
        if (bytes == null || bytes.Length == 0)
            return null;

        switch (varType)
        {
            case VarType.Byte:
                if (varCount == 1)
                    return bytes[0];
                else
                    return bytes;
            case VarType.Word:
                if (varCount == 1)
                    return Word.FromByteArray(bytes);
                else
                    return Word.ToArray(bytes);
            case VarType.Int:
                if (varCount == 1)
                    return Int.FromByteArray(bytes);
                else
                    return Int.ToArray(bytes);
            case VarType.DWord:
                if (varCount == 1)
                    return DWord.FromByteArray(bytes);
                else
                    return DWord.ToArray(bytes);
            case VarType.DInt:
                if (varCount == 1)
                    return DInt.FromByteArray(bytes);
                else
                    return DInt.ToArray(bytes);
            case VarType.Real:
                if (varCount == 1)
                    return Real.FromByteArray(bytes);
                else
                    return Real.ToArray(bytes);
            case VarType.LReal:
                if (varCount == 1)
                    return LReal.FromByteArray(bytes);
                else
                    return LReal.ToArray(bytes);

            case VarType.String:
                return Types.String.FromByteArray(bytes);
            case VarType.S7String:
                return S7String.FromByteArray(bytes);
            case VarType.S7WString:
                return S7WString.FromByteArray(bytes);

            case VarType.Timer:
                if (varCount == 1)
                    return Types.Timer.FromByteArray(bytes);
                else
                    return Types.Timer.ToArray(bytes);
            case VarType.Counter:
                if (varCount == 1)
                    return Counter.FromByteArray(bytes);
                else
                    return Counter.ToArray(bytes);
            case VarType.Bit:
                if (varCount == 1)
                    if (bitAdr > 7)
                        return null;
                    else
                        return Bit.FromByte(bytes[0], bitAdr);
                else
                    return Bit.ToBitArray(bytes, varCount);
            case VarType.DateTime:
                if (varCount == 1)
                    return DateTime.FromByteArray(bytes);
                else
                    return DateTime.ToArray(bytes);
            case VarType.DateTimeLong:
                if (varCount == 1)
                    return DateTimeLong.FromByteArray(bytes);
                else
                    return DateTimeLong.ToArray(bytes);
            default:
                return null;
        }
    }

    /// <summary>
    /// Given a S7 <see cref="VarType"/> (Bool, Word, DWord, etc.), it returns how many bytes to read.
    /// </summary>
    /// <param name="varType"></param>
    /// <param name="varCount"></param>
    /// <returns>Byte lenght of variable</returns>
    internal static Int32 VarTypeToByteLength(VarType varType, Int32 varCount = 1)
    {
        return varType switch
        {
            VarType.Bit => (varCount + 7) / 8,
            VarType.Byte => varCount < 1 ? 1 : varCount,
            VarType.String => varCount,
            VarType.S7String => (varCount + 2 & 1) == 1 ? varCount + 3 : varCount + 2,
            VarType.S7WString => varCount * 2 + 4,
            VarType.Word or VarType.Timer or VarType.Int or VarType.Counter => varCount * 2,
            VarType.DWord or VarType.DInt or VarType.Real => varCount * 4,
            VarType.LReal or VarType.DateTime => varCount * 8,
            VarType.DateTimeLong => varCount * 12,
            _ => 0,
        };
    }

    private System.Byte[] GetS7ConnectionSetup()
    {
        return new System.Byte[] {  3, 0, 0, 25, 2, 240, 128, 50, 1, 0, 0, 255, 255, 0, 8, 0, 0, 240, 0, 0, 3, 0, 3,
                3, 192 // Use 960 PDU size
        };
    }

    private void ParseDataIntoDataItems(System.Byte[] s7data, List<DataItem> dataItems)
    {
        var offset = 14;
        foreach (var dataItem in dataItems)
        {
            // check for Return Code = Success
            if (s7data[offset] != 0xff)
                throw new PlcException(ErrorCode.WrongNumberReceivedBytes);

            // to Data bytes
            offset += 4;

            var byteCnt = VarTypeToByteLength(dataItem.VarType, dataItem.Count);
            dataItem.Value = ParseBytes(
                dataItem.VarType,
                s7data.Skip(offset).Take(byteCnt).ToArray(),
                dataItem.Count,
                dataItem.BitAdr
            );

            // next Item
            offset += byteCnt;

            // Always align to even offset
            if (offset % 2 != 0)
                offset++;
        }
    }

    private static System.Byte[] BuildReadRequestPackage(IList<DataItemAddress> dataItems)
    {
        var packageSize = 19 + dataItems.Count * 12;
        var package = new MemoryStream(packageSize);

        BuildHeaderPackage(package, dataItems.Count);

        foreach (var dataItem in dataItems)
            BuildReadDataRequestPackage(package, dataItem.DataType, dataItem.DB, dataItem.StartByteAddress, dataItem.ByteLength);

        return package.ToArray();
    }
}
