namespace CenturionCC.System.Player
{
    public enum KillType
    {
        Default,
        FriendlyFire,
        ReverseFriendlyFire
    }

    public enum BodyParts
    {
        Body,
        Head,
        LeftArm,
        RightArm,
        LeftLeg,
        RightLeg
    }

    public enum FriendlyFireMode
    {
        Always,
        Reverse,
        Both,
        Warning,
        Never,
    }

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

    public static class PlayerEnums
    {
        public static string ToEnumName(this KillType type)
        {
            switch (type)
            {
                case KillType.Default:
                    return "Default";
                case KillType.FriendlyFire:
                    return "FriendlyFire";
                case KillType.ReverseFriendlyFire:
                    return "ReverseFriendlyFire";
            }

            return "UNDEFINED_RANGE";
        }

        public static string ToEnumName(this BodyParts parts)
        {
            switch (parts)
            {
                case BodyParts.Body:
                    return "Body";
                case BodyParts.Head:
                    return "Head";
                case BodyParts.LeftArm:
                    return "LeftArm";
                case BodyParts.RightArm:
                    return "RightArm";
                case BodyParts.LeftLeg:
                    return "LeftLeg";
                case BodyParts.RightLeg:
                    return "RightLeg";
            }

            return "UNDEFINED_RANGE";
        }

        public static string ToEnumName(this FriendlyFireMode ffMode)
        {
            switch (ffMode)
            {
                case FriendlyFireMode.Always:
                    return "Always";
                case FriendlyFireMode.Reverse:
                    return "Reverse";
                case FriendlyFireMode.Both:
                    return "Both";
                case FriendlyFireMode.Warning:
                    return "Warning";
                case FriendlyFireMode.Never:
                    return "Never";
            }

            return "UNDEFINED_RANGE";
        }


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
            }

            return "UNDEFINED_RANGE";
        }


        public static byte ToByte(this BodyParts parts)
        {
            switch (parts)
            {
                case BodyParts.Body: return 0;
                case BodyParts.Head: return 1;
                case BodyParts.LeftArm: return 2;
                case BodyParts.RightArm: return 3;
                case BodyParts.LeftLeg: return 4;
                case BodyParts.RightLeg: return 5;
                default: return 0xFF;
            }
        }
    }
}