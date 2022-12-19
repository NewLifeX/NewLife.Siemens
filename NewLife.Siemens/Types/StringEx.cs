namespace NewLife.Siemens.Types
{
    /// <inheritdoc cref="S7String"/>
    [Obsolete("Please use S7String class")]
    public static class StringEx
    {
        /// <inheritdoc cref="S7String.FromByteArray(global::System.Byte[])"/>
        public static System.String FromByteArray(System.Byte[] bytes) => S7String.FromByteArray(bytes);

        /// <inheritdoc cref="S7String.ToByteArray(System.String, Int32)"/>
        public static System.Byte[] ToByteArray(System.String value, Int32 reservedLength) => S7String.ToByteArray(value, reservedLength);
    }
}
