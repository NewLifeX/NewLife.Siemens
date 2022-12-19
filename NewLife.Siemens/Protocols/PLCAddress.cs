using NewLife.Siemens.Common;
using NewLife.Siemens.Models;

namespace NewLife.Siemens.Protocols;

internal class PLCAddress
{
    public DataType DataType { get; set; }

    public Int32 DbNumber { get; set; }

    public Int32 StartByte { get; set; }

    public Int32 BitNumber { get; set; }

    public VarType VarType { get; set; }

    public PLCAddress(String address)
    {
        Parse(address, out var dataType, out var dbNumber, out var varType, out var startByte, out var bitNumber);

        DataType = dataType;
        DbNumber = dbNumber;
        StartByte = startByte;
        BitNumber = bitNumber;
        VarType = varType;
    }

    public static void Parse(String input, out DataType dataType, out Int32 dbNumber, out VarType varType, out Int32 address, out Int32 bitNumber)
    {
        bitNumber = -1;
        dbNumber = 0;

        switch (input[..2])
        {
            case "DB":
                var strings = input.Split(new Char[] { '.' });
                if (strings.Length < 2)
                    throw new InvalidAddressException("To few periods for DB address");

                dataType = DataType.DataBlock;
                dbNumber = Int32.Parse(strings[0][2..]);
                address = Int32.Parse(strings[1][3..]);

                var dbType = strings[1][..3];
                switch (dbType)
                {
                    case "DBB":
                        varType = VarType.Byte;
                        return;
                    case "DBW":
                        varType = VarType.Word;
                        return;
                    case "DBD":
                        varType = VarType.DWord;
                        return;
                    case "DBX":
                        bitNumber = Int32.Parse(strings[2]);
                        if (bitNumber > 7)
                            throw new InvalidAddressException("Bit can only be 0-7");
                        varType = VarType.Bit;
                        return;
                    default:
                        throw new InvalidAddressException();
                }
            case "IB":
            case "EB":
                // Input byte
                dataType = DataType.Input;
                dbNumber = 0;
                address = Int32.Parse(input[2..]);
                varType = VarType.Byte;
                return;
            case "IW":
            case "EW":
                // Input word
                dataType = DataType.Input;
                dbNumber = 0;
                address = Int32.Parse(input[2..]);
                varType = VarType.Word;
                return;
            case "ID":
            case "ED":
                // Input double-word
                dataType = DataType.Input;
                dbNumber = 0;
                address = Int32.Parse(input[2..]);
                varType = VarType.DWord;
                return;
            case "QB":
            case "AB":
            case "OB":
                // Output byte
                dataType = DataType.Output;
                dbNumber = 0;
                address = Int32.Parse(input[2..]);
                varType = VarType.Byte;
                return;
            case "QW":
            case "AW":
            case "OW":
                // Output word
                dataType = DataType.Output;
                dbNumber = 0;
                address = Int32.Parse(input[2..]);
                varType = VarType.Word;
                return;
            case "QD":
            case "AD":
            case "OD":
                // Output double-word
                dataType = DataType.Output;
                dbNumber = 0;
                address = Int32.Parse(input[2..]);
                varType = VarType.DWord;
                return;
            case "MB":
                // Memory byte
                dataType = DataType.Memory;
                dbNumber = 0;
                address = Int32.Parse(input[2..]);
                varType = VarType.Byte;
                return;
            case "MW":
                // Memory word
                dataType = DataType.Memory;
                dbNumber = 0;
                address = Int32.Parse(input[2..]);
                varType = VarType.Word;
                return;
            case "MD":
                // Memory double-word
                dataType = DataType.Memory;
                dbNumber = 0;
                address = Int32.Parse(input[2..]);
                varType = VarType.DWord;
                return;
            default:
                switch (input[..1])
                {
                    case "E":
                    case "I":
                        // Input
                        dataType = DataType.Input;
                        varType = VarType.Bit;
                        break;
                    case "Q":
                    case "A":
                    case "O":
                        // Output
                        dataType = DataType.Output;
                        varType = VarType.Bit;
                        break;
                    case "M":
                        // Memory
                        dataType = DataType.Memory;
                        varType = VarType.Bit;
                        break;
                    case "T":
                        // Timer
                        dataType = DataType.Timer;
                        dbNumber = 0;
                        address = Int32.Parse(input[1..]);
                        varType = VarType.Timer;
                        return;
                    case "Z":
                    case "C":
                        // Counter
                        dataType = DataType.Counter;
                        dbNumber = 0;
                        address = Int32.Parse(input[1..]);
                        varType = VarType.Counter;
                        return;
                    default:
                        throw new InvalidAddressException(String.Format("{0} is not a valid address", input[..1]));
                }

                var txt2 = input[1..];
                if (txt2.IndexOf(".") == -1)
                    throw new InvalidAddressException("To few periods for DB address");

                address = Int32.Parse(txt2[..txt2.IndexOf(".")]);
                bitNumber = Int32.Parse(txt2[(txt2.IndexOf(".") + 1)..]);
                if (bitNumber > 7)
                    throw new InvalidAddressException("Bit can only be 0-7");
                return;
        }
    }
}
