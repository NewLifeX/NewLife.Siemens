using System.Collections;

namespace NewLife.Siemens.Types
{
    /// <summary>
    /// Contains the conversion methods to convert Bit from S7 plc to C#.
    /// </summary>
    public static class Bit
    {
        /// <summary>
        /// Converts a Bit to bool
        /// </summary>
        public static System.Boolean FromByte(System.Byte v, System.Byte bitAdr) => (v & 1 << bitAdr) != 0;

        /// <summary>
        /// Converts an array of bytes to a BitArray.
        /// </summary>
        /// <param name="bytes">The bytes to convert.</param>
        /// <returns>A BitArray with the same number of bits and equal values as <paramref name="bytes"/>.</returns>
        public static BitArray ToBitArray(System.Byte[] bytes) => ToBitArray(bytes, bytes.Length * 8);

        /// <summary>
        /// Converts an array of bytes to a BitArray.
        /// </summary>
        /// <param name="bytes">The bytes to convert.</param>
        /// <param name="length">The number of bits to return.</param>
        /// <returns>A BitArray with <paramref name="length"/> bits.</returns>
        public static BitArray ToBitArray(System.Byte[] bytes, Int32 length)
        {
            if (length > bytes.Length * 8) throw new ArgumentException($"Not enough data in bytes to return {length} bits.", nameof(bytes));

            var bitArr = new BitArray(bytes);
            var bools = new System.Boolean[length];
            for (var i = 0; i < length; i++) bools[i] = bitArr[i];

            return new BitArray(bools);
        }
    }
}
