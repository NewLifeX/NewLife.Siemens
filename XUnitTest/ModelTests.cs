using System;
using NewLife.Siemens.Messages;
using NewLife.Siemens.Models;
using NewLife.Siemens.Protocols;
using Xunit;

namespace XUnitTest;

public class ModelTests
{
    #region DataType
    [Fact]
    public void DataType_Values()
    {
        Assert.Equal(129, (Int32)DataType.Input);
        Assert.Equal(130, (Int32)DataType.Output);
        Assert.Equal(131, (Int32)DataType.Memory);
        Assert.Equal(132, (Int32)DataType.DataBlock);
        Assert.Equal(29, (Int32)DataType.Timer);
        Assert.Equal(28, (Int32)DataType.Counter);
    }
    #endregion

    #region VarType
    [Fact]
    public void VarType_Values()
    {
        Assert.Equal(1, (Int32)VarType.Bit);
        Assert.Equal(2, (Int32)VarType.Byte);
        Assert.Equal(3, (Int32)VarType.Word);
        Assert.Equal(4, (Int32)VarType.DWord);
        Assert.Equal(5, (Int32)VarType.Int);
        Assert.Equal(6, (Int32)VarType.DInt);
        Assert.Equal(7, (Int32)VarType.Real);
        Assert.Equal(8, (Int32)VarType.LReal);
        Assert.Equal(9, (Int32)VarType.String);
        Assert.Equal(10, (Int32)VarType.S7String);
        Assert.Equal(11, (Int32)VarType.S7WString);
        Assert.Equal(12, (Int32)VarType.Timer);
        Assert.Equal(13, (Int32)VarType.Counter);
        Assert.Equal(14, (Int32)VarType.DateTime);
        Assert.Equal(15, (Int32)VarType.DateTimeLong);
    }
    #endregion

    #region CpuType
    [Fact]
    public void CpuType_Values()
    {
        Assert.Equal(0, (Int32)CpuType.S7200);
        Assert.Equal(1, (Int32)CpuType.Logo0BA8);
        Assert.Equal(2, (Int32)CpuType.S7200Smart);
        Assert.Equal(3, (Int32)CpuType.S7300);
        Assert.Equal(4, (Int32)CpuType.S7400);
        Assert.Equal(5, (Int32)CpuType.S71200);
        Assert.Equal(6, (Int32)CpuType.S71500);
    }
    #endregion

    #region ErrorCode
    [Fact]
    public void ErrorCode_Values()
    {
        Assert.Equal(0, (Int32)ErrorCode.NoError);
        Assert.Equal(1, (Int32)ErrorCode.WrongCPU_Type);
        Assert.Equal(2, (Int32)ErrorCode.ConnectionError);
        Assert.Equal(3, (Int32)ErrorCode.IPAddressNotAvailable);
        Assert.Equal(10, (Int32)ErrorCode.WrongVarFormat);
        Assert.Equal(11, (Int32)ErrorCode.WrongNumberReceivedBytes);
        Assert.Equal(20, (Int32)ErrorCode.SendData);
        Assert.Equal(30, (Int32)ErrorCode.ReadData);
        Assert.Equal(50, (Int32)ErrorCode.WriteData);
    }
    #endregion

    #region S7Functions
    [Fact]
    public void S7Functions_Values()
    {
        Assert.Equal(0xF0, (Int32)S7Functions.Setup);
        Assert.Equal(0x04, (Int32)S7Functions.ReadVar);
        Assert.Equal(0x05, (Int32)S7Functions.WriteVar);
    }
    #endregion

    #region ReadWriteErrorCode
    [Fact]
    public void ReadWriteErrorCode_Values()
    {
        Assert.Equal(0x00, (Int32)ReadWriteErrorCode.Reserved);
        Assert.Equal(0x01, (Int32)ReadWriteErrorCode.HardwareFault);
        Assert.Equal(0x03, (Int32)ReadWriteErrorCode.AccessingObjectNotAllowed);
        Assert.Equal(0x05, (Int32)ReadWriteErrorCode.AddressOutOfRange);
        Assert.Equal(0x06, (Int32)ReadWriteErrorCode.DataTypeNotSupported);
        Assert.Equal(0x07, (Int32)ReadWriteErrorCode.DataTypeInconsistent);
        Assert.Equal(0x0A, (Int32)ReadWriteErrorCode.ObjectDoesNotExist);
        Assert.Equal(0xFF, (Int32)ReadWriteErrorCode.Success);
    }
    #endregion

    #region S7Kinds
    [Fact]
    public void S7Kinds_Values()
    {
        Assert.Equal(0x01, (Int32)S7Kinds.Job);
        Assert.Equal(0x02, (Int32)S7Kinds.Ack);
        Assert.Equal(0x03, (Int32)S7Kinds.AckData);
        Assert.Equal(0x07, (Int32)S7Kinds.UserData);
    }
    #endregion

    #region PduType
    [Fact]
    public void PduType_Values()
    {
        Assert.Equal(0xF0, (Int32)PduType.Data);
        Assert.Equal(0xE0, (Int32)PduType.ConnectionRequest);
        Assert.Equal(0xD0, (Int32)PduType.ConnectionConfirmed);
    }
    #endregion

    #region COTPParameterKinds
    [Fact]
    public void COTPParameterKinds_Values()
    {
        Assert.Equal(0xC0, (Int32)COTPParameterKinds.TpduSize);
        Assert.Equal(0xC1, (Int32)COTPParameterKinds.SrcTsap);
        Assert.Equal(0xC2, (Int32)COTPParameterKinds.DstTsap);
    }
    #endregion
}
