using System.Reflection;
using NewLife.Siemens.Common;

namespace NewLife.Siemens.Types
{
    /// <summary>
    /// Contains the methods to convert a C# class to S7 data types
    /// </summary>
    public static class Class
    {
        private static IEnumerable<PropertyInfo> GetAccessableProperties(Type classType)
        {
            return classType
#if NETSTANDARD1_3
                .GetTypeInfo().DeclaredProperties.Where(p => p.SetMethod != null);
#else
                .GetProperties(
                    BindingFlags.SetProperty |
                    BindingFlags.Public |
                    BindingFlags.Instance)
                .Where(p => p.GetSetMethod() != null);
#endif

        }

        private static System.Double GetIncreasedNumberOfBytes(System.Double numBytes, Type type)
        {
            switch (type.Name)
            {
                case "Boolean":
                    numBytes += 0.125;
                    break;
                case "Byte":
                    numBytes = Math.Ceiling(numBytes);
                    numBytes++;
                    break;
                case "Int16":
                case "UInt16":
                    numBytes = Math.Ceiling(numBytes);
                    if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                        numBytes++;
                    numBytes += 2;
                    break;
                case "Int32":
                case "UInt32":
                    numBytes = Math.Ceiling(numBytes);
                    if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                        numBytes++;
                    numBytes += 4;
                    break;
                case "Single":
                    numBytes = Math.Ceiling(numBytes);
                    if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                        numBytes++;
                    numBytes += 4;
                    break;
                case "Double":
                    numBytes = Math.Ceiling(numBytes);
                    if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                        numBytes++;
                    numBytes += 8;
                    break;
                default:
                    var propertyClass = Activator.CreateInstance(type);
                    numBytes = GetClassSize(propertyClass, numBytes, true);
                    break;
            }

            return numBytes;
        }

        /// <summary>
        /// Gets the size of the class in bytes.
        /// </summary>
        /// <param name="instance">An instance of the class</param>
        /// <returns>the number of bytes</returns>
        public static System.Double GetClassSize(Object instance, System.Double numBytes = 0.0, System.Boolean isInnerProperty = false)
        {
            var properties = GetAccessableProperties(instance.GetType());
            foreach (var property in properties)
                if (property.PropertyType.IsArray)
                {
                    var elementType = property.PropertyType.GetElementType();
                    var array = (Array)property.GetValue(instance, null);
                    if (array.Length <= 0)
                        throw new Exception("Cannot determine size of class, because an array is defined which has no fixed size greater than zero.");

                    IncrementToEven(ref numBytes);
                    for (var i = 0; i < array.Length; i++)
                        numBytes = GetIncreasedNumberOfBytes(numBytes, elementType);
                }
                else
                    numBytes = GetIncreasedNumberOfBytes(numBytes, property.PropertyType);
            if (false == isInnerProperty)
            {
                // enlarge numBytes to next even number because S7-Structs in a DB always will be resized to an even byte count
                numBytes = Math.Ceiling(numBytes);
                if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                    numBytes++;
            }
            return numBytes;
        }

        private static Object GetPropertyValue(Type propertyType, System.Byte[] bytes, ref System.Double numBytes)
        {
            Object value = null;

            switch (propertyType.Name)
            {
                case "Boolean":
                    // get the value
                    var bytePos = (Int32)Math.Floor(numBytes);
                    var bitPos = (Int32)((numBytes - bytePos) / 0.125);
                    if ((bytes[bytePos] & (Int32)Math.Pow(2, bitPos)) != 0)
                        value = true;
                    else
                        value = false;
                    numBytes += 0.125;
                    break;
                case "Byte":
                    numBytes = Math.Ceiling(numBytes);
                    value = bytes[(Int32)numBytes];
                    numBytes++;
                    break;
                case "Int16":
                    numBytes = Math.Ceiling(numBytes);
                    if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                        numBytes++;
                    // hier auswerten
                    var source = Word.FromBytes(bytes[(Int32)numBytes + 1], bytes[(Int32)numBytes]);
                    value = source.ConvertToShort();
                    numBytes += 2;
                    break;
                case "UInt16":
                    numBytes = Math.Ceiling(numBytes);
                    if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                        numBytes++;
                    // hier auswerten
                    value = Word.FromBytes(bytes[(Int32)numBytes + 1], bytes[(Int32)numBytes]);
                    numBytes += 2;
                    break;
                case "Int32":
                    numBytes = Math.Ceiling(numBytes);
                    if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                        numBytes++;
                    // hier auswerten
                    var sourceUInt = DWord.FromBytes(bytes[(Int32)numBytes + 3],
                                                                       bytes[(Int32)numBytes + 2],
                                                                       bytes[(Int32)numBytes + 1],
                                                                       bytes[(Int32)numBytes + 0]);
                    value = sourceUInt.ConvertToInt();
                    numBytes += 4;
                    break;
                case "UInt32":
                    numBytes = Math.Ceiling(numBytes);
                    if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                        numBytes++;
                    // hier auswerten
                    value = DWord.FromBytes(
                        bytes[(Int32)numBytes],
                        bytes[(Int32)numBytes + 1],
                        bytes[(Int32)numBytes + 2],
                        bytes[(Int32)numBytes + 3]);
                    numBytes += 4;
                    break;
                case "Single":
                    numBytes = Math.Ceiling(numBytes);
                    if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                        numBytes++;
                    // hier auswerten
                    value = Real.FromByteArray(
                        new System.Byte[] {
                            bytes[(Int32)numBytes],
                            bytes[(Int32)numBytes + 1],
                            bytes[(Int32)numBytes + 2],
                            bytes[(Int32)numBytes + 3] });
                    numBytes += 4;
                    break;
                case "Double":
                    numBytes = Math.Ceiling(numBytes);
                    if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                        numBytes++;
                    var buffer = new System.Byte[8];
                    Array.Copy(bytes, (Int32)numBytes, buffer, 0, 8);
                    // hier auswerten
                    value = LReal.FromByteArray(buffer);
                    numBytes += 8;
                    break;
                default:
                    var propClass = Activator.CreateInstance(propertyType);
                    numBytes = FromBytes(propClass, bytes, numBytes);
                    value = propClass;
                    break;
            }

            return value;
        }

        /// <summary>
        /// Sets the object's values with the given array of bytes
        /// </summary>
        /// <param name="sourceClass">The object to fill in the given array of bytes</param>
        /// <param name="bytes">The array of bytes</param>
        public static System.Double FromBytes(Object sourceClass, System.Byte[] bytes, System.Double numBytes = 0, System.Boolean isInnerClass = false)
        {
            if (bytes == null)
                return numBytes;

            var properties = GetAccessableProperties(sourceClass.GetType());
            foreach (var property in properties)
                if (property.PropertyType.IsArray)
                {
                    var array = (Array)property.GetValue(sourceClass, null);
                    IncrementToEven(ref numBytes);
                    var elementType = property.PropertyType.GetElementType();
                    for (var i = 0; i < array.Length && numBytes < bytes.Length; i++)
                        array.SetValue(
                            GetPropertyValue(elementType, bytes, ref numBytes),
                            i);
                }
                else
                    property.SetValue(
                        sourceClass,
                        GetPropertyValue(property.PropertyType, bytes, ref numBytes),
                        null);

            return numBytes;
        }

        private static System.Double SetBytesFromProperty(Object propertyValue, System.Byte[] bytes, System.Double numBytes)
        {
            var bytePos = 0;
            var bitPos = 0;
            System.Byte[] bytes2 = null;

            switch (propertyValue.GetType().Name)
            {
                case "Boolean":
                    // get the value
                    bytePos = (Int32)Math.Floor(numBytes);
                    bitPos = (Int32)((numBytes - bytePos) / 0.125);
                    if ((System.Boolean)propertyValue)
                        bytes[bytePos] |= (System.Byte)Math.Pow(2, bitPos);            // is true
                    else
                        bytes[bytePos] &= (System.Byte)~(System.Byte)Math.Pow(2, bitPos);   // is false
                    numBytes += 0.125;
                    break;
                case "Byte":
                    numBytes = (Int32)Math.Ceiling(numBytes);
                    bytePos = (Int32)numBytes;
                    bytes[bytePos] = (System.Byte)propertyValue;
                    numBytes++;
                    break;
                case "Int16":
                    bytes2 = Int.ToByteArray((Int16)propertyValue);
                    break;
                case "UInt16":
                    bytes2 = Word.ToByteArray((UInt16)propertyValue);
                    break;
                case "Int32":
                    bytes2 = DInt.ToByteArray((Int32)propertyValue);
                    break;
                case "UInt32":
                    bytes2 = DWord.ToByteArray((UInt32)propertyValue);
                    break;
                case "Single":
                    bytes2 = Real.ToByteArray((System.Single)propertyValue);
                    break;
                case "Double":
                    bytes2 = LReal.ToByteArray((System.Double)propertyValue);
                    break;
                default:
                    numBytes = ToBytes(propertyValue, bytes, numBytes);
                    break;
            }

            if (bytes2 != null)
            {
                IncrementToEven(ref numBytes);

                bytePos = (Int32)numBytes;
                for (var bCnt = 0; bCnt < bytes2.Length; bCnt++)
                    bytes[bytePos + bCnt] = bytes2[bCnt];
                numBytes += bytes2.Length;
            }

            return numBytes;
        }

        /// <summary>
        /// Creates a byte array depending on the struct type.
        /// </summary>
        /// <param name="sourceClass">The struct object</param>
        /// <returns>A byte array or null if fails.</returns>
        public static System.Double ToBytes(Object sourceClass, System.Byte[] bytes, System.Double numBytes = 0.0)
        {
            var properties = GetAccessableProperties(sourceClass.GetType());
            foreach (var property in properties)
                if (property.PropertyType.IsArray)
                {
                    var array = (Array)property.GetValue(sourceClass, null);
                    IncrementToEven(ref numBytes);
                    var elementType = property.PropertyType.GetElementType();
                    for (var i = 0; i < array.Length && numBytes < bytes.Length; i++)
                        numBytes = SetBytesFromProperty(array.GetValue(i), bytes, numBytes);
                }
                else
                    numBytes = SetBytesFromProperty(property.GetValue(sourceClass, null), bytes, numBytes);
            return numBytes;
        }

        private static void IncrementToEven(ref System.Double numBytes)
        {
            numBytes = Math.Ceiling(numBytes);
            if (numBytes % 2 > 0) numBytes++;
        }
    }
}
