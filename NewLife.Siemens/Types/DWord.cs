namespace NewLife.Siemens.Types
{
    /// <summary>
    /// Contains the conversion methods to convert DWord from S7 plc to C#.
    /// </summary>
    public static class DWord
    {
        /// <summary>
        /// Converts a S7 DWord (4 bytes) to uint (UInt32)
        /// </summary>
        public static UInt32 FromByteArray(System.Byte[] bytes) => (UInt32)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);


        /// <summary>
        /// Converts 4 bytes to DWord (UInt32)
        /// </summary>
        public static UInt32 FromBytes(System.Byte b1, System.Byte b2, System.Byte b3, System.Byte b4) => (UInt32)(b4 << 24 | b3 << 16 | b2 << 8 | b1);


        /// <summary>
        /// Converts a uint (UInt32) to S7 DWord (4 bytes) 
        /// </summary>
        public static System.Byte[] ToByteArray(UInt32 value)
        {
            var bytes = new System.Byte[4];

            bytes[0] = (System.Byte)(value >> 24 & 0xFF);
            bytes[1] = (System.Byte)(value >> 16 & 0xFF);
            bytes[2] = (System.Byte)(value >> 8 & 0xFF);
            bytes[3] = (System.Byte)(value & 0xFF);

            return bytes;
        }






        /// <summary>
        /// Converts an array of uint (UInt32) to an array of S7 DWord (4 bytes) 
        /// </summary>
        public static System.Byte[] ToByteArray(UInt32[] value)
        {
            var arr = new ByteArray();
            foreach (var val in value)
                arr.Add(ToByteArray(val));
            return arr.Array;
        }

        /// <summary>
        /// Converts an array of S7 DWord to an array of uint (UInt32)
        /// </summary>
        public static UInt32[] ToArray(System.Byte[] bytes)
        {
            var values = new UInt32[bytes.Length / 4];

            var counter = 0;
            for (var cnt = 0; cnt < bytes.Length / 4; cnt++)
                values[cnt] = FromByteArray(new System.Byte[] { bytes[counter++], bytes[counter++], bytes[counter++], bytes[counter++] });

            return values;
        }
    }
}
