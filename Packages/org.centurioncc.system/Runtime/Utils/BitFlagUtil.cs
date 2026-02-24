namespace CenturionCC.System.Utils
{
    public static class BitFlagUtil
    {
        public static bool HasFlag(int flags, int flag) => (flags & flag) == flag;
        public static int SetFlag(int flags, int flag, bool bit = true) => bit ? flags | flag : flags & ~flag;
        public static int ToggleFlag(int flags, int flag) => flags ^ flag;
        public static string ToBinaryString(int flags)
        {
            var buff = "";
            for (var i = 0; i < 8; i++)
            {
                buff += (flags & 1) == 0 ? "0" : "1";
                flags >>= 1;
            }

            return buff;
        }
    }
}
