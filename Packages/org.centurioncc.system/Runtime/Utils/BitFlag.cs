namespace CenturionCC.System.Utils
{
    public static class BitFlag
    {
        public static bool IsSet(int flags, int flag) => (flags & flag) == flag;
        public static int Set(int flags, int flag, bool bit = true) => bit ? flags | flag : flags & ~flag;
        public static int Toggle(int flags, int flag) => flags ^ flag;
    }
}
