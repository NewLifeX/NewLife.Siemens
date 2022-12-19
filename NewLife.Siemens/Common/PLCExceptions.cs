namespace NewLife.Siemens.Common
{
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

#if NET_FULL
        protected WrongNumberOfBytesException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
#endif
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

#if NET_FULL
        protected InvalidAddressException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
#endif
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

#if NET_FULL
        protected InvalidVariableTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
#endif
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

#if NET_FULL
        protected TPKTInvalidException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
#endif
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
}
