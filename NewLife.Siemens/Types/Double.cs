namespace NewLife.Siemens.Types
{
    /// <summary>
    /// Contains the conversion methods to convert Real from S7 plc to C# double.
    /// </summary>
    [Obsolete("Class Double is obsolete. Use Real instead for 32bit floating point, or LReal for 64bit floating point.")]
    public static class Double
    {
        /// <summary>
        /// Converts a S7 Real (4 bytes) to double
        /// </summary>
        public static System.Double FromByteArray(System.Byte[] bytes) => Real.FromByteArray(bytes);

        /// <summary>
        /// Converts a S7 DInt to double
        /// </summary>
        public static System.Double FromDWord(Int32 value)
        {
            var b = DInt.ToByteArray(value);
            var d = FromByteArray(b);
            return d;
        }

        /// <summary>
        /// Converts a S7 DWord to double
        /// </summary>
        public static System.Double FromDWord(UInt32 value)
        {
            var b = DWord.ToByteArray(value);
            var d = FromByteArray(b);
            return d;
        }


        /// <summary>
        /// Converts a double to S7 Real (4 bytes)
        /// </summary>
        public static System.Byte[] ToByteArray(System.Double value) => Real.ToByteArray((System.Single)value);

        /// <summary>
        /// Converts an array of double to an array of bytes 
        /// </summary>
        public static System.Byte[] ToByteArray(System.Double[] value)
        {
            var arr = new ByteArray();
            foreach (var val in value)
                arr.Add(ToByteArray(val));
            return arr.Array;
        }

        /// <summary>
        /// Converts an array of S7 Real to an array of double
        /// </summary>
        public static System.Double[] ToArray(System.Byte[] bytes)
        {
            var values = new System.Double[bytes.Length / 4];

            var counter = 0;
            for (var cnt = 0; cnt < bytes.Length / 4; cnt++)
                values[cnt] = FromByteArray(new System.Byte[] { bytes[counter++], bytes[counter++], bytes[counter++], bytes[counter++] });

            return values;
        }

    }
}
