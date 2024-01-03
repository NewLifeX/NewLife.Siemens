using System.Runtime.Serialization;

namespace NewLife.Siemens.Common;

internal class WrongNumberOfBytesException : Exception
{
    public WrongNumberOfBytesException() : base()
    {
    }

    public WrongNumberOfBytesException(String message) : base(message)
    {
    }

    public WrongNumberOfBytesException(String message, Exception innerException) : base(message, innerException)
    {
    }

    protected WrongNumberOfBytesException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}

internal class InvalidAddressException : Exception
{
    public InvalidAddressException() : base()
    {
    }

    public InvalidAddressException(String message) : base(message)
    {
    }

    public InvalidAddressException(String message, Exception innerException) : base(message, innerException)
    {
    }

    protected InvalidAddressException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}

internal class InvalidVariableTypeException : Exception
{
    public InvalidVariableTypeException() : base()
    {
    }

    public InvalidVariableTypeException(String message) : base(message)
    {
    }

    public InvalidVariableTypeException(String message, Exception innerException) : base(message, innerException)
    {
    }

    protected InvalidVariableTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}

internal class TPKTInvalidException : Exception
{
    public TPKTInvalidException() : base()
    {
    }

    public TPKTInvalidException(String message) : base(message)
    {
    }

    public TPKTInvalidException(String message, Exception innerException) : base(message, innerException)
    {
    }

    protected TPKTInvalidException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}

internal class TPDUInvalidException : Exception
{
    public TPDUInvalidException() : base()
    {
    }

    public TPDUInvalidException(String message) : base(message)
    {
    }

    public TPDUInvalidException(String message, Exception innerException) : base(message, innerException)
    {
    }
}
