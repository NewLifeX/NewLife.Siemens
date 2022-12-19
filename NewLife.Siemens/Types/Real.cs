namespace NewLife.Siemens.Types
{
    /// <summary>
    /// Contains the conversion methods to convert Real from S7 plc to C# double.
    /// </summary>
    public static class Real
    {
        /// <summary>
        /// Converts a S7 Real (4 bytes) to float
        /// </summary>
        public static System.Single FromByteArray(System.Byte[] bytes)
        {
            if (bytes.Length != 4)
                throw new ArgumentException("Wrong number of bytes. Bytes array must contain 4 bytes.");

            // sps uses bigending so we have to reverse if platform needs
            if (BitConverter.IsLittleEndian)
                // create deep copy of the array and reverse
                bytes = new System.Byte[] { bytes[3], bytes[2], bytes[1], bytes[0] };

            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// Converts a float to S7 Real (4 bytes)
        /// </summary>
        public static System.Byte[] ToByteArray(System.Single value)
        {
            var bytes = BitConverter.GetBytes(value);

            // sps uses bigending so we have to check if platform is same
            if (!BitConverter.IsLittleEndian) return bytes;

            // create deep copy of the array and reverse
            return new System.Byte[] { bytes[3], bytes[2], bytes[1], bytes[0] };
        }

        /// <summary>
        /// Converts an array of float to an array of bytes 
        /// </summary>
        public static System.Byte[] ToByteArray(System.Single[] value)
        {
            var buffer = new System.Byte[4 * value.Length];
            var stream = new MemoryStream(buffer);
            foreach (var val in value)
                stream.Write(ToByteArray(val), 0, 4);

            return buffer;
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
