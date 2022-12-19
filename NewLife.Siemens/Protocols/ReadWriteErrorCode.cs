namespace NewLife.Siemens.Protocols;

internal enum ReadWriteErrorCode : Byte
{
    Reserved = 0x00,
    HardwareFault = 0x01,
    AccessingObjectNotAllowed = 0x03,
    AddressOutOfRange = 0x05,
    DataTypeNotSupported = 0x06,
    DataTypeInconsistent = 0x07,
    ObjectDoesNotExist = 0x0a,
    Success = 0xff
}
