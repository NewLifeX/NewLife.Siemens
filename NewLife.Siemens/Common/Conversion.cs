using System.Globalization;

namespace NewLife.Siemens.Common
{
    /// <summary>
    /// Conversion methods to convert from Siemens numeric format to C# and back
    /// </summary>
    public static class Conversion
    {
        /// <summary>
        /// Converts a binary string to Int32 value
        /// </summary>
        /// <param name="txt"></param>
        /// <returns></returns>
        public static Int32 BinStringToInt32(this String txt)
        {
            var ret = 0;

            for (var i = 0; i < txt.Length; i++)
                ret = ret << 1 | (txt[i] == '1' ? 1 : 0);
            return ret;
        }

        /// <summary>
        /// Converts a binary string to a byte. Can return null.
        /// </summary>
        /// <param name="txt"></param>
        /// <returns></returns>
        public static Byte? BinStringToByte(this String txt)
        {
            if (txt.Length == 8) return (Byte)txt.BinStringToInt32();
            return null;
        }

        /// <summary>
        /// Converts the value to a binary string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static String ValToBinString(this Object value)
        {
            var cnt = 0;
            var cnt2 = 0;
            var x = 0;
            var txt = "";
            Int64 longValue = 0;

            try
            {
                if (value.GetType().Name.IndexOf("[]") < 0)
                {
                    // ist nur ein Wert
                    switch (value.GetType().Name)
                    {
                        case "Byte":
                            x = 7;
                            longValue = (Byte)value;
                            break;
                        case "Int16":
                            x = 15;
                            longValue = (Int16)value;
                            break;
                        case "Int32":
                            x = 31;
                            longValue = (Int32)value;
                            break;
                        case "Int64":
                            x = 63;
                            longValue = (Int64)value;
                            break;
                        default:
                            throw new Exception();
                    }

                    for (cnt = x; cnt >= 0; cnt += -1)
                        if ((longValue & (Int64)Math.Pow(2, cnt)) > 0)
                            txt += "1";
                        else
                            txt += "0";
                }
                else
                    // ist ein Array
                    switch (value.GetType().Name)
                    {
                        case "Byte[]":
                            x = 7;
                            var ByteArr = (Byte[])value;
                            for (cnt2 = 0; cnt2 <= ByteArr.Length - 1; cnt2++)
                                for (cnt = x; cnt >= 0; cnt += -1)
                                    if ((ByteArr[cnt2] & (Byte)Math.Pow(2, cnt)) > 0) txt += "1"; else txt += "0";
                            break;
                        case "Int16[]":
                            x = 15;
                            var Int16Arr = (Int16[])value;
                            for (cnt2 = 0; cnt2 <= Int16Arr.Length - 1; cnt2++)
                                for (cnt = x; cnt >= 0; cnt += -1)
                                    if ((Int16Arr[cnt2] & (Byte)Math.Pow(2, cnt)) > 0) txt += "1"; else txt += "0";
                            break;
                        case "Int32[]":
                            x = 31;
                            var Int32Arr = (Int32[])value;
                            for (cnt2 = 0; cnt2 <= Int32Arr.Length - 1; cnt2++)
                                for (cnt = x; cnt >= 0; cnt += -1)
                                    if ((Int32Arr[cnt2] & (Byte)Math.Pow(2, cnt)) > 0) txt += "1"; else txt += "0";
                            break;
                        case "Int64[]":
                            x = 63;
                            var Int64Arr = (Byte[])value;
                            for (cnt2 = 0; cnt2 <= Int64Arr.Length - 1; cnt2++)
                                for (cnt = x; cnt >= 0; cnt += -1)
                                    if ((Int64Arr[cnt2] & (Byte)Math.Pow(2, cnt)) > 0) txt += "1"; else txt += "0";
                            break;
                        default:
                            throw new Exception();
                    }
                return txt;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Helper to get a bit value given a byte and the bit index.
        /// Example: DB1.DBX0.5 -> var bytes = ReadBytes(DB1.DBW0); bool bit = bytes[0].SelectBit(5); 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="bitPosition"></param>
        /// <returns></returns>
        public static Boolean SelectBit(this Byte data, Int32 bitPosition)
        {
            var mask = 1 << bitPosition;
            var result = data & mask;

            return result != 0;
        }

        /// <summary>
        /// Converts from ushort value to short value; it's used to retrieve negative values from words
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Int16 ConvertToShort(this UInt16 input)
        {
            Int16 output;
            output = Int16.Parse(input.ToString("X"), NumberStyles.HexNumber);
            return output;
        }

        /// <summary>
        /// Converts from short value to ushort value; it's used to pass negative values to DWs
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static UInt16 ConvertToUshort(this Int16 input)
        {
            UInt16 output;
            output = UInt16.Parse(input.ToString("X"), NumberStyles.HexNumber);
            return output;
        }

        /// <summary>
        /// Converts from UInt32 value to Int32 value; it's used to retrieve negative values from DBDs
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Int32 ConvertToInt(this UInt32 input)
        {
            Int32 output;
            output = Int32.Parse(input.ToString("X"), NumberStyles.HexNumber);
            return output;
        }

        /// <summary>
        /// Converts from Int32 value to UInt32 value; it's used to pass negative values to DBDs
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static UInt32 ConvertToUInt(this Int32 input)
        {
            UInt32 output;
            output = UInt32.Parse(input.ToString("X"), NumberStyles.HexNumber);
            return output;
        }

        /// <summary>
        /// Converts from float to DWord (DBD)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static UInt32 ConvertToUInt(this Single input)
        {
            UInt32 output;
            output = Types.DWord.FromByteArray(Types.Real.ToByteArray(input));
            return output;
        }

        /// <summary>
        /// Converts from DWord (DBD) to float
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Single ConvertToFloat(this UInt32 input)
        {
            Single output;
            output = Types.Real.FromByteArray(Types.DWord.ToByteArray(input));
            return output;
        }
    }
}
