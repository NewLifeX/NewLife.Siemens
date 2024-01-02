using NewLife.Siemens.Models;

namespace NewLife.Siemens.Common;

/// <summary>PLC异常</summary>
public class PlcException : Exception
{
    /// <summary>错误码</summary>
    public ErrorCode ErrorCode { get; }

    /// <summary>实例化</summary>
    /// <param name="errorCode"></param>
    public PlcException(ErrorCode errorCode) : this(errorCode, $"PLC communication failed with error '{errorCode}'.")
    {
    }

    /// <summary>实例化</summary>
    /// <param name="errorCode"></param>
    /// <param name="innerException"></param>
    public PlcException(ErrorCode errorCode, Exception innerException) : this(errorCode, innerException.Message,
        innerException)
    {
    }

    /// <summary>实例化</summary>
    /// <param name="errorCode"></param>
    /// <param name="message"></param>
    public PlcException(ErrorCode errorCode, String message) : base(message) => ErrorCode = errorCode;

    /// <summary>实例化</summary>
    /// <param name="errorCode"></param>
    /// <param name="message"></param>
    /// <param name="inner"></param>
    public PlcException(ErrorCode errorCode, String message, Exception inner) : base(message, inner) => ErrorCode = errorCode;
}