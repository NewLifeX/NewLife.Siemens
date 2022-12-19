namespace NewLife.Siemens.Types
{
    /// <summary>
    /// Contains the conversion methods to convert DInt from S7 plc to C# int (Int32).
    /// </summary>
    public static class DInt
    {
        /// <summary>
        /// Converts a S7 DInt (4 bytes) to int (Int32)
        /// </summary>
        public static Int32 FromByteArray(System.Byte[] bytes)
        {
            if (bytes.Length != 4)
                throw new ArgumentException("Wrong number of bytes. Bytes array must contain 4 bytes.");
            return bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];
        }


        /// <summary>
        /// Converts a int (Int32) to S7 DInt (4 bytes)
        /// </summary>
        public static System.Byte[] ToByteArray(Int32 value)
        {
            var bytes = new System.Byte[4];

            bytes[0] = (System.Byte)(value >> 24 & 0xFF);
            bytes[1] = (System.Byte)(value >> 16 & 0xFF);
            bytes[2] = (System.Byte)(value >> 8 & 0xFF);
            bytes[3] = (System.Byte)(value & 0xFF);

            return bytes;
        }

        /// <summary>
        /// Converts an array of int (Int32) to an array of bytes
        /// </summary>
        public static System.Byte[] ToByteArray(Int32[] value)
        {
            var arr = new ByteArray();
            foreach (var val in value)
                arr.Add(ToByteArray(val));
            return arr.Array;
        }

        /// <summary>
        /// Converts an array of S7 DInt to an array of int (Int32)
        /// </summary>
        public static Int32[] ToArray(System.Byte[] bytes)
        {
            var values = new Int32[bytes.Length / 4];

            var counter = 0;
            for (var cnt = 0; cnt < bytes.Length / 4; cnt++)
                values[cnt] = FromByteArray(new System.Byte[] { bytes[counter++], bytes[counter++], bytes[counter++], bytes[counter++] });

            return values;
        }


    }
}
