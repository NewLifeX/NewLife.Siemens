namespace NewLife.Siemens.Common
{
    public class InvalidDataException : Exception
    {
        public Byte[] ReceivedData { get; }
        public Int32 ErrorIndex { get; }
        public Byte ExpectedValue { get; }

        public InvalidDataException(String message, Byte[] receivedData, Int32 errorIndex, Byte expectedValue)
            : base(FormatMessage(message, receivedData, errorIndex, expectedValue))
        {
            ReceivedData = receivedData;
            ErrorIndex = errorIndex;
            ExpectedValue = expectedValue;
        }

        private static String FormatMessage(String message, Byte[] receivedData, Int32 errorIndex, Byte expectedValue)
        {
            if (errorIndex >= receivedData.Length)
                throw new ArgumentOutOfRangeException(nameof(errorIndex),
                    $"{nameof(errorIndex)} {errorIndex} is outside the bounds of {nameof(receivedData)} with length {receivedData.Length}.");

            return $"{message} Invalid data received. Expected '{expectedValue}' at index {errorIndex}, " +
                $"but received {receivedData[errorIndex]}. See the {nameof(ReceivedData)} property " +
                "for the full message received.";
        }
    }
}
