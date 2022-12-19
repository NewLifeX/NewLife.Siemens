namespace NewLife.Siemens.Types
{
    /// <summary>
    /// Contains the methods to convert from bytes to byte arrays
    /// </summary>
    public static class Byte
    {
        /// <summary>
        /// Converts a byte to byte array
        /// </summary>
        public static System.Byte[] ToByteArray(System.Byte value)
        {
            return new System.Byte[] { value }; ;
        }

        /// <summary>
        /// Converts a byte array to byte
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static System.Byte FromByteArray(System.Byte[] bytes)
        {
            if (bytes.Length != 1)
                throw new ArgumentException("Wrong number of bytes. Bytes array must contain 1 bytes.");
            return bytes[0];
        }

    }
}
