namespace NewLife.Siemens.Types
{
    /// <summary>
    /// Contains the conversion methods to convert Counter from S7 plc to C# ushort (UInt16).
    /// </summary>
    public static class Counter
    {
        /// <summary>
        /// Converts a Counter (2 bytes) to ushort (UInt16)
        /// </summary>
        public static UInt16 FromByteArray(System.Byte[] bytes)
        {
            if (bytes.Length != 2)
                throw new ArgumentException("Wrong number of bytes. Bytes array must contain 2 bytes.");
            // bytes[0] -> HighByte
            // bytes[1] -> LowByte
            return (UInt16)(bytes[0] << 8 | bytes[1]);
        }


        /// <summary>
        /// Converts a ushort (UInt16) to word (2 bytes)
        /// </summary>
        public static System.Byte[] ToByteArray(UInt16 value)
        {
            var bytes = new System.Byte[2];

            bytes[0] = (System.Byte)(value << 8 & 0xFF);
            bytes[1] = (System.Byte)(value & 0xFF);

            return bytes;
        }

        /// <summary>
        /// Converts an array of ushort (UInt16) to an array of bytes
        /// </summary>
        public static System.Byte[] ToByteArray(UInt16[] value)
        {
            var arr = new ByteArray();
            foreach (var val in value)
                arr.Add(ToByteArray(val));
            return arr.Array;
        }

        /// <summary>
        /// Converts an array of bytes to an array of ushort
        /// </summary>
        public static UInt16[] ToArray(System.Byte[] bytes)
        {
            var values = new UInt16[bytes.Length / 2];

            var counter = 0;
            for (var cnt = 0; cnt < bytes.Length / 2; cnt++)
                values[cnt] = FromByteArray(new System.Byte[] { bytes[counter++], bytes[counter++] });

            return values;
        }
    }
}
