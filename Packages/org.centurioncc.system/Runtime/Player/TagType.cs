namespace CenturionCC.System.Player
{
    public enum TagType
    {
        Debug,
        Team,
        Master,
        Staff,
        Dev,
        Owner,
        Creator,
        Hit
    }

    public static class TagTypeExtensions
    {
        public static string ToEnumName(this TagType type)
        {
            switch (type)
            {
                case TagType.Debug:
                    return "Debug";
                case TagType.Team:
                    return "Team";
                case TagType.Master:
                    return "Master";
                case TagType.Staff:
                    return "Staff";
                case TagType.Dev:
                    return "Dev";
                case TagType.Owner:
                    return "Owner";
                case TagType.Creator:
                    return "Creator";
                case TagType.Hit:
                    return "Hit";
                default:
                    return "UNKNOWN";
            }
        }
    }
}