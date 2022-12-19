using System.Reflection;
using NewLife.Siemens.Common;

namespace NewLife.Siemens.Types
{
    /// <summary>
    /// Contains the method to convert a C# struct to S7 data types
    /// </summary>
    public static class Struct
    {
        /// <summary>
        /// Gets the size of the struct in bytes.
        /// </summary>
        /// <param name="structType">the type of the struct</param>
        /// <returns>the number of bytes</returns>
        public static Int32 GetStructSize(Type structType)
        {
            var numBytes = 0.0;

            var infos = structType
#if NETSTANDARD1_3
                .GetTypeInfo().DeclaredFields;
#else
                .GetFields();
#endif

            foreach (var info in infos)
                switch (info.FieldType.Name)
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
                    case "String":
                        var attribute = info.GetCustomAttributes<S7StringAttribute>().SingleOrDefault();
                        if (attribute == default(S7StringAttribute))
                            throw new ArgumentException("Please add S7StringAttribute to the string field");

                        numBytes = Math.Ceiling(numBytes);
                        if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                            numBytes++;
                        numBytes += attribute.ReservedLengthInBytes;
                        break;
                    default:
                        numBytes += GetStructSize(info.FieldType);
                        break;
                }
            return (Int32)numBytes;
        }

        /// <summary>
        /// Creates a struct of a specified type by an array of bytes.
        /// </summary>
        /// <param name="structType">The struct type</param>
        /// <param name="bytes">The array of bytes</param>
        /// <returns>The object depending on the struct type or null if fails(array-length != struct-length</returns>
        public static Object FromBytes(Type structType, System.Byte[] bytes)
        {
            if (bytes == null)
                return null;

            if (bytes.Length != GetStructSize(structType))
                return null;

            // and decode it
            var bytePos = 0;
            var bitPos = 0;
            var numBytes = 0.0;
            var structValue = Activator.CreateInstance(structType);


            var infos = structValue.GetType()
#if NETSTANDARD1_3
                .GetTypeInfo().DeclaredFields;
#else
                .GetFields();
#endif

            foreach (var info in infos)
                switch (info.FieldType.Name)
                {
                    case "Boolean":
                        // get the value
                        bytePos = (Int32)Math.Floor(numBytes);
                        bitPos = (Int32)((numBytes - bytePos) / 0.125);
                        if ((bytes[bytePos] & (Int32)Math.Pow(2, bitPos)) != 0)
                            info.SetValue(structValue, true);
                        else
                            info.SetValue(structValue, false);
                        numBytes += 0.125;
                        break;
                    case "Byte":
                        numBytes = Math.Ceiling(numBytes);
                        info.SetValue(structValue, bytes[(Int32)numBytes]);
                        numBytes++;
                        break;
                    case "Int16":
                        numBytes = Math.Ceiling(numBytes);
                        if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                            numBytes++;
                        // get the value
                        var source = Word.FromBytes(bytes[(Int32)numBytes + 1], bytes[(Int32)numBytes]);
                        info.SetValue(structValue, source.ConvertToShort());
                        numBytes += 2;
                        break;
                    case "UInt16":
                        numBytes = Math.Ceiling(numBytes);
                        if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                            numBytes++;
                        // get the value
                        info.SetValue(structValue, Word.FromBytes(bytes[(Int32)numBytes + 1],
                                                                          bytes[(Int32)numBytes]));
                        numBytes += 2;
                        break;
                    case "Int32":
                        numBytes = Math.Ceiling(numBytes);
                        if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                            numBytes++;
                        // get the value
                        var sourceUInt = DWord.FromBytes(bytes[(Int32)numBytes + 3],
                                                                           bytes[(Int32)numBytes + 2],
                                                                           bytes[(Int32)numBytes + 1],
                                                                           bytes[(Int32)numBytes + 0]);
                        info.SetValue(structValue, sourceUInt.ConvertToInt());
                        numBytes += 4;
                        break;
                    case "UInt32":
                        numBytes = Math.Ceiling(numBytes);
                        if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                            numBytes++;
                        // get the value
                        info.SetValue(structValue, DWord.FromBytes(bytes[(Int32)numBytes],
                                                                           bytes[(Int32)numBytes + 1],
                                                                           bytes[(Int32)numBytes + 2],
                                                                           bytes[(Int32)numBytes + 3]));
                        numBytes += 4;
                        break;
                    case "Single":
                        numBytes = Math.Ceiling(numBytes);
                        if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                            numBytes++;
                        // get the value
                        info.SetValue(structValue, Real.FromByteArray(new System.Byte[] { bytes[(Int32)numBytes],
                                                                           bytes[(Int32)numBytes + 1],
                                                                           bytes[(Int32)numBytes + 2],
                                                                           bytes[(Int32)numBytes + 3] }));
                        numBytes += 4;
                        break;
                    case "Double":
                        numBytes = Math.Ceiling(numBytes);
                        if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                            numBytes++;
                        // get the value
                        var data = new System.Byte[8];
                        Array.Copy(bytes, (Int32)numBytes, data, 0, 8);
                        info.SetValue(structValue, LReal.FromByteArray(data));
                        numBytes += 8;
                        break;
                    case "String":
                        var attribute = info.GetCustomAttributes<S7StringAttribute>().SingleOrDefault();
                        if (attribute == default(S7StringAttribute))
                            throw new ArgumentException("Please add S7StringAttribute to the string field");

                        numBytes = Math.Ceiling(numBytes);
                        if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                            numBytes++;

                        // get the value
                        var sData = new System.Byte[attribute.ReservedLengthInBytes];
                        Array.Copy(bytes, (Int32)numBytes, sData, 0, sData.Length);
                        switch (attribute.Type)
                        {
                            case S7StringType.S7String:
                                info.SetValue(structValue, S7String.FromByteArray(sData));
                                break;
                            case S7StringType.S7WString:
                                info.SetValue(structValue, S7WString.FromByteArray(sData));
                                break;
                            default:
                                throw new ArgumentException("Please use a valid string type for the S7StringAttribute");
                        }

                        numBytes += sData.Length;
                        break;
                    default:
                        var buffer = new System.Byte[GetStructSize(info.FieldType)];
                        if (buffer.Length == 0)
                            continue;
                        Buffer.BlockCopy(bytes, (Int32)Math.Ceiling(numBytes), buffer, 0, buffer.Length);
                        info.SetValue(structValue, FromBytes(info.FieldType, buffer));
                        numBytes += buffer.Length;
                        break;
                }
            return structValue;
        }

        /// <summary>
        /// Creates a byte array depending on the struct type.
        /// </summary>
        /// <param name="structValue">The struct object</param>
        /// <returns>A byte array or null if fails.</returns>
        public static System.Byte[] ToBytes(Object structValue)
        {
            var type = structValue.GetType();

            var size = GetStructSize(type);
            var bytes = new System.Byte[size];
            System.Byte[] bytes2 = null;

            var bytePos = 0;
            var bitPos = 0;
            var numBytes = 0.0;

            var infos = type
#if NETSTANDARD1_3
                .GetTypeInfo().DeclaredFields;
#else
                .GetFields();
#endif

            foreach (var info in infos)
            {
                bytes2 = null;
                switch (info.FieldType.Name)
                {
                    case "Boolean":
                        // get the value
                        bytePos = (Int32)Math.Floor(numBytes);
                        bitPos = (Int32)((numBytes - bytePos) / 0.125);
                        if ((System.Boolean)info.GetValue(structValue))
                            bytes[bytePos] |= (System.Byte)Math.Pow(2, bitPos);            // is true
                        else
                            bytes[bytePos] &= (System.Byte)~(System.Byte)Math.Pow(2, bitPos);   // is false
                        numBytes += 0.125;
                        break;
                    case "Byte":
                        numBytes = (Int32)Math.Ceiling(numBytes);
                        bytePos = (Int32)numBytes;
                        bytes[bytePos] = (System.Byte)info.GetValue(structValue);
                        numBytes++;
                        break;
                    case "Int16":
                        bytes2 = Int.ToByteArray((Int16)info.GetValue(structValue));
                        break;
                    case "UInt16":
                        bytes2 = Word.ToByteArray((UInt16)info.GetValue(structValue));
                        break;
                    case "Int32":
                        bytes2 = DInt.ToByteArray((Int32)info.GetValue(structValue));
                        break;
                    case "UInt32":
                        bytes2 = DWord.ToByteArray((UInt32)info.GetValue(structValue));
                        break;
                    case "Single":
                        bytes2 = Real.ToByteArray((System.Single)info.GetValue(structValue));
                        break;
                    case "Double":
                        bytes2 = LReal.ToByteArray((System.Double)info.GetValue(structValue));
                        break;
                    case "String":
                        var attribute = info.GetCustomAttributes<S7StringAttribute>().SingleOrDefault();
                        if (attribute == default(S7StringAttribute))
                            throw new ArgumentException("Please add S7StringAttribute to the string field");

                        bytes2 = attribute.Type switch
                        {
                            S7StringType.S7String => S7String.ToByteArray((System.String)info.GetValue(structValue), attribute.ReservedLength),
                            S7StringType.S7WString => S7WString.ToByteArray((System.String)info.GetValue(structValue), attribute.ReservedLength),
                            _ => throw new ArgumentException("Please use a valid string type for the S7StringAttribute")
                        };
                        break;
                }
                if (bytes2 != null)
                {
                    // add them
                    numBytes = Math.Ceiling(numBytes);
                    if (numBytes / 2 - Math.Floor(numBytes / 2.0) > 0)
                        numBytes++;
                    bytePos = (Int32)numBytes;
                    for (var bCnt = 0; bCnt < bytes2.Length; bCnt++)
                        bytes[bytePos + bCnt] = bytes2[bCnt];
                    numBytes += bytes2.Length;
                }
            }
            return bytes;
        }
    }
}
