using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Siemens.Common
{
    public class InvalidDataException : Exception
    {
        public byte[] ReceivedData { get; }
        public int ErrorIndex { get; }
        public byte ExpectedValue { get; }

        public InvalidDataException(string message, byte[] receivedData, int errorIndex, byte expectedValue)
            : base(FormatMessage(message, receivedData, errorIndex, expectedValue))
        {
            ReceivedData = receivedData;
            ErrorIndex = errorIndex;
            ExpectedValue = expectedValue;
        }

        private static string FormatMessage(string message, byte[] receivedData, int errorIndex, byte expectedValue)
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
