namespace NewLife.Siemens.Types
{
    /// <summary>
    /// Contains the conversion methods to convert Int from S7 plc to C#.
    /// </summary>
    public static class Int
    {
        /// <summary>
        /// Converts a S7 Int (2 bytes) to short (Int16)
        /// </summary>
        public static Int16 FromByteArray(System.Byte[] bytes)
        {
            if (bytes.Length != 2)
                throw new ArgumentException("Wrong number of bytes. Bytes array must contain 2 bytes.");
            // bytes[0] -> HighByte
            // bytes[1] -> LowByte
            return (Int16)(bytes[1] | bytes[0] << 8);
        }


        /// <summary>
        /// Converts a short (Int16) to a S7 Int byte array (2 bytes)
        /// </summary>
        public static System.Byte[] ToByteArray(Int16 value)
        {
            var bytes = new System.Byte[2];

            bytes[0] = (System.Byte)(value >> 8 & 0xFF);
            bytes[1] = (System.Byte)(value & 0xFF);

            return bytes;
        }

        /// <summary>
        /// Converts an array of short (Int16) to a S7 Int byte array (2 bytes)
        /// </summary>
        public static System.Byte[] ToByteArray(Int16[] value)
        {
            var bytes = new System.Byte[value.Length * 2];
            var bytesPos = 0;

            for (var i = 0; i < value.Length; i++)
            {
                bytes[bytesPos++] = (System.Byte)(value[i] >> 8 & 0xFF);
                bytes[bytesPos++] = (System.Byte)(value[i] & 0xFF);
            }
            return bytes;
        }

        /// <summary>
        /// Converts an array of S7 Int to an array of short (Int16)
        /// </summary>
        public static Int16[] ToArray(System.Byte[] bytes)
        {
            var shortsCount = bytes.Length / 2;

            var values = new Int16[shortsCount];

            var counter = 0;
            for (var cnt = 0; cnt < shortsCount; cnt++)
                values[cnt] = FromByteArray(new System.Byte[] { bytes[counter++], bytes[counter++] });

            return values;
        }

        /// <summary>
        /// Converts a C# int value to a C# short value, to be used as word.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Int16 CWord(Int32 value)
        {
            if (value > 32767)
            {
                value -= 32768;
                value = 32768 - value;
                value *= -1;
            }
            return (Int16)value;
        }

    }
}
