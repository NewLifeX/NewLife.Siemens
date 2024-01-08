using NewLife.Serialization;

namespace NewLife.Siemens;

static class Helper
{
    /// <summary>是否已达到末尾</summary>
    /// <param name="binary"></param>
    /// <returns></returns>
    public static Boolean EndOfStream(this Binary binary) => binary.Stream.Position >= binary.Stream.Length;

    /// <summary>检查剩余量是否足够</summary>
    /// <param name="binary"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static Boolean CheckRemain(this Binary binary, Int32 size) => binary.Stream.Position + size <= binary.Stream.Length;
}
