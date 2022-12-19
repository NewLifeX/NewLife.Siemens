namespace NewLife.Siemens.Types
{
    /// <summary>
    /// Contains the conversion methods to convert Real from S7 plc to C# float.
    /// </summary>
    [Obsolete("Class Single is obsolete. Use Real instead.")]
    public static class Single
    {
        /// <summary>
        /// Converts a S7 Real (4 bytes) to float
        /// </summary>
        public static System.Single FromByteArray(System.Byte[] bytes) => Real.FromByteArray(bytes);

        /// <summary>
        /// Converts a S7 DInt to float
        /// </summary>
        public static System.Single FromDWord(Int32 value)
        {
            var b = DInt.ToByteArray(value);
            var d = FromByteArray(b);
            return d;
        }

        /// <summary>
        /// Converts a S7 DWord to float
        /// </summary>
        public static System.Single FromDWord(UInt32 value)
        {
            var b = DWord.ToByteArray(value);
            var d = FromByteArray(b);
            return d;
        }


        /// <summary>
        /// Converts a double to S7 Real (4 bytes)
        /// </summary>
        public static System.Byte[] ToByteArray(System.Single value) => Real.ToByteArray(value);

        /// <summary>
        /// Converts an array of float to an array of bytes 
        /// </summary>
        public static System.Byte[] ToByteArray(System.Single[] value)
        {
            var arr = new ByteArray();
            foreach (var val in value)
                arr.Add(ToByteArray(val));
            return arr.Array;
        }

        /// <summary>
        /// Converts an array of S7 Real to an array of float
        /// </summary>
        public static System.Single[] ToArray(System.Byte[] bytes)
        {
            var values = new System.Single[bytes.Length / 4];

            var counter = 0;
            for (var cnt = 0; cnt < bytes.Length / 4; cnt++)
                values[cnt] = FromByteArray(new System.Byte[] { bytes[counter++], bytes[counter++], bytes[counter++], bytes[counter++] });

            return values;
        }

    }
}
