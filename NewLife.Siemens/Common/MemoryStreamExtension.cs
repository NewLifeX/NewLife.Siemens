namespace NewLife.Siemens.Common
{
    internal static class MemoryStreamExtension
    {
        /// <summary>
        /// Helper function to write to whole content of the given byte array to a memory stream.
        /// 
        /// Writes all bytes in value from 0 to value.Length to the memory stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="value"></param>
        public static void WriteByteArray(this MemoryStream stream, Byte[] value) => stream.Write(value, 0, value.Length);
    }
}
