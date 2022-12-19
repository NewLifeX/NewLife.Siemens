namespace NewLife.Siemens.Types
{
    /// <summary>
    /// Converts the Timer data type to C# data type
    /// </summary>
    public static class Timer
    {
        /// <summary>
        /// Converts the timer bytes to a double
        /// </summary>
        public static System.Double FromByteArray(System.Byte[] bytes)
        {
            System.Double wert = 0;

            wert = (bytes[0] & 0x0F) * 100.0;
            wert += (bytes[1] >> 4 & 0x0F) * 10.0;
            wert += (bytes[1] & 0x0F) * 1.0;

            // this value is not used... may for a nother exponation
            //int unknown = (bytes[0] >> 6) & 0x03;

            switch (bytes[0] >> 4 & 0x03)
            {
                case 0:
                    wert *= 0.01;
                    break;
                case 1:
                    wert *= 0.1;
                    break;
                case 2:
                    wert *= 1.0;
                    break;
                case 3:
                    wert *= 10.0;
                    break;
            }

            return wert;
        }

        /// <summary>
        /// Converts a ushort (UInt16) to an array of bytes formatted as time
        /// </summary>
        public static System.Byte[] ToByteArray(UInt16 value)
        {
            var bytes = new System.Byte[2];
            bytes[1] = (System.Byte)(value & 0xFF);
            bytes[0] = (System.Byte)(value >> 8 & 0xFF);

            return bytes;
        }

        /// <summary>
        /// Converts an array of ushorts (Uint16) to an array of bytes formatted as time
        /// </summary>
        public static System.Byte[] ToByteArray(UInt16[] value)
        {
            var arr = new ByteArray();
            foreach (var val in value)
                arr.Add(ToByteArray(val));
            return arr.Array;
        }

        /// <summary>
        /// Converts an array of bytes formatted as time to an array of doubles
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static System.Double[] ToArray(System.Byte[] bytes)
        {
            var values = new System.Double[bytes.Length / 2];

            var counter = 0;
            for (var cnt = 0; cnt < bytes.Length / 2; cnt++)
                values[cnt] = FromByteArray(new System.Byte[] { bytes[counter++], bytes[counter++] });

            return values;
        }
    }
}
