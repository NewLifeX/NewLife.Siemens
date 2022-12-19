namespace NewLife.Siemens.Models
{
    /// <summary>
    /// Types
    /// </summary>
    public enum VarType
    {
        /// <summary>
        /// S7 Bit variable type (bool)
        /// </summary>
        Bit,

        /// <summary>
        /// S7 Byte variable type (8 bits)
        /// </summary>
        Byte,

        /// <summary>
        /// S7 Word variable type (16 bits, 2 bytes)
        /// </summary>
        Word,

        /// <summary>
        /// S7 DWord variable type (32 bits, 4 bytes)
        /// </summary>
        DWord,

        /// <summary>
        /// S7 Int variable type (16 bits, 2 bytes)
        /// </summary>
        Int,

        /// <summary>
        /// DInt variable type (32 bits, 4 bytes)
        /// </summary>
        DInt,

        /// <summary>
        /// Real variable type (32 bits, 4 bytes)
        /// </summary>
        Real,

        /// <summary>
        /// LReal variable type (64 bits, 8 bytes)
        /// </summary>
        LReal,

        /// <summary>
        /// Char Array / C-String variable type (variable)
        /// </summary>
        String,

        /// <summary>
        /// S7 String variable type (variable)
        /// </summary>
        S7String,

        /// <summary>
        /// S7 WString variable type (variable)
        /// </summary>
        S7WString,

        /// <summary>
        /// Timer variable type
        /// </summary>
        Timer,

        /// <summary>
        /// Counter variable type
        /// </summary>
        Counter,

        /// <summary>
        /// DateTIme variable type
        /// </summary>
        DateTime,

        /// <summary>
        /// DateTimeLong variable type
        /// </summary>
        DateTimeLong
    }
}