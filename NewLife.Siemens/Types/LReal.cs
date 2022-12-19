﻿namespace NewLife.Siemens.Types
{
    /// <summary>
    /// Contains the conversion methods to convert Real from S7 plc to C# double.
    /// </summary>
    public static class LReal
    {
        /// <summary>
        /// Converts a S7 LReal (8 bytes) to double
        /// </summary>
        public static System.Double FromByteArray(System.Byte[] bytes)
        {
            if (bytes.Length != 8)
                throw new ArgumentException("Wrong number of bytes. Bytes array must contain 8 bytes.");
            var buffer = bytes;

            // sps uses bigending so we have to reverse if platform needs
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            return BitConverter.ToDouble(buffer, 0);
        }

        /// <summary>
        /// Converts a double to S7 LReal (8 bytes)
        /// </summary>
        public static System.Byte[] ToByteArray(System.Double value)
        {
            var bytes = BitConverter.GetBytes(value);

            // sps uses bigending so we have to check if platform is same
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        /// <summary>
        /// Converts an array of double to an array of bytes 
        /// </summary>
        public static System.Byte[] ToByteArray(System.Double[] value) => TypeHelper.ToByteArray(value, ToByteArray);

        /// <summary>
        /// Converts an array of S7 LReal to an array of double
        /// </summary>
        public static System.Double[] ToArray(System.Byte[] bytes) => TypeHelper.ToArray(bytes, FromByteArray);

    }
}
