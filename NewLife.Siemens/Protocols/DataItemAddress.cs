using NewLife.Siemens.Models;

namespace NewLife.Siemens.Protocols;

/// <summary>
/// Represents an area of memory in the PLC
/// </summary>
internal class DataItemAddress(DataType dataType, Int32 db, Int32 startByteAddress, Int32 byteLength)
{
    /// <summary>
    /// Memory area to read 
    /// </summary>
    public DataType DataType { get; } = dataType;

    /// <summary>
    /// Address of memory area to read (example: for DB1 this value is 1, for T45 this value is 45)
    /// </summary>
    public Int32 DB { get; } = db;

    /// <summary>
    /// Address of the first byte to read
    /// </summary>
    public Int32 StartByteAddress { get; } = startByteAddress;

    /// <summary>
    /// Length of data to read
    /// </summary>
    public Int32 ByteLength { get; } = byteLength;
}
