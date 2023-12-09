namespace CenturionCC.System.Player
{
    public enum KillType
    {
        Default,
        FriendlyFire
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
    }
}