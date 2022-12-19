namespace NewLife.Siemens.Types
{
    internal class ByteArray
    {
        private List<System.Byte> list = new List<System.Byte>();

        public System.Byte this[Int32 index]
        {
            get => list[index];
            set => list[index] = value;
        }

        public System.Byte[] Array => list.ToArray();

        public Int32 Length => list.Count;

        public ByteArray() => list = new List<System.Byte>();

        public ByteArray(Int32 size) => list = new List<System.Byte>(size);

        public void Clear() => list = new List<System.Byte>();

        public void Add(System.Byte item) => list.Add(item);

        public void AddWord(UInt16 value)
        {
            list.Add((System.Byte)(value >> 8));
            list.Add((System.Byte)value);
        }

        public void Add(System.Byte[] items) => list.AddRange(items);

        public void Add(IEnumerable<System.Byte> items) => list.AddRange(items);

        public void Add(ByteArray byteArray) => list.AddRange(byteArray.Array);
    }
}
