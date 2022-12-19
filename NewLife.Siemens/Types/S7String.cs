using System.Text;
using NewLife.Siemens.Common;
using NewLife.Siemens.Models;

namespace NewLife.Siemens.Types
{
    /// <summary>
    /// Contains the methods to convert from S7 strings to C# strings
    /// An S7 String has a preceeding 2 byte header containing its capacity and length
    /// </summary>
    public static class S7String
    {
        private static Encoding stringEncoding = Encoding.ASCII;

        /// <summary>
        /// The Encoding used when serializing and deserializing S7String (Encoding.ASCII by default)
        /// </summary>
        /// <exception cref="ArgumentNullException">StringEncoding must not be null</exception>
        public static Encoding StringEncoding
        {
            get => stringEncoding;
            set => stringEncoding = value ?? throw new ArgumentNullException(nameof(StringEncoding));
        }

        /// <summary>
        /// Converts S7 bytes to a string
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static System.String FromByteArray(System.Byte[] bytes)
        {
            if (bytes.Length < 2)
                throw new PlcException(ErrorCode.ReadData, "Malformed S7 String / too short");

            Int32 size = bytes[0];
            Int32 length = bytes[1];
            if (length > size)
                throw new PlcException(ErrorCode.ReadData, "Malformed S7 String / length larger than capacity");

            try
            {
                return StringEncoding.GetString(bytes, 2, length);
            }
            catch (Exception e)
            {
                throw new PlcException(ErrorCode.ReadData,
                    $"Failed to parse {VarType.S7String} from data. Following fields were read: size: '{size}', actual length: '{length}', total number of bytes (including header): '{bytes.Length}'.",
                    e);
            }
        }

        /// <summary>
        /// Converts a <see cref="T:string"/> to S7 string with 2-byte header.
        /// </summary>
        /// <param name="value">The string to convert to byte array.</param>
        /// <param name="reservedLength">The length (in characters) allocated in PLC for the string.</param>
        /// <returns>A <see cref="T:byte[]" /> containing the string header and string value with a maximum length of <paramref name="reservedLength"/> + 2.</returns>
        public static System.Byte[] ToByteArray(System.String value, Int32 reservedLength)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            if (reservedLength > 254) throw new ArgumentException($"The maximum string length supported is 254.");

            var bytes = StringEncoding.GetBytes(value);
            if (bytes.Length > reservedLength) throw new ArgumentException($"The provided string length ({bytes.Length} is larger than the specified reserved length ({reservedLength}).");

            var buffer = new System.Byte[2 + reservedLength];
            Array.Copy(bytes, 0, buffer, 2, bytes.Length);
            buffer[0] = (System.Byte)reservedLength;
            buffer[1] = (System.Byte)bytes.Length;
            return buffer;
        }
    }
}
