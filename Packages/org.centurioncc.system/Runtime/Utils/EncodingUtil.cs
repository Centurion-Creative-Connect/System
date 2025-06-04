using System;
using UnityEngine;

namespace CenturionCC.System.Utils
{
    public static class EncodingUtil
    {
        public static byte[] GetBytes(Vector3 vec3)
        {
            var bytes = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(vec3.x), 0, bytes, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(vec3.y), 0, bytes, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(vec3.z), 0, bytes, 8, 4);
            return bytes;
        }

        public static byte[] GetBytes(Quaternion q)
        {
            var bytes = new byte[16];
            Buffer.BlockCopy(BitConverter.GetBytes(q.x), 0, bytes, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(q.y), 0, bytes, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(q.z), 0, bytes, 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(q.w), 0, bytes, 12, 4);
            return bytes;
        }

        public static byte[] GetBytes(DetectionType detectionType,
            bool respectFriendlyFire, bool canDamageSelf, bool canDamageFriendly, bool canDamageEnemy)
        {
            var bytes = new byte[1];
            bytes[0] = (byte)(detectionType.ToByte() |
                              (respectFriendlyFire ? 1 : 0) << 2 |
                              (canDamageSelf ? 1 : 0) << 3 |
                              (canDamageFriendly ? 1 : 0) << 4 |
                              (canDamageEnemy ? 1 : 0) << 5);
            return bytes;
        }

        public static void ToDamageOptions(byte[] bytes, int index, out DetectionType detectionType,
            out bool respectFriendlyFire, out bool canDamageSelf, out bool canDamageFriendly, out bool canDamageEnemy)
        {
            var b = bytes[index];
            detectionType = (DetectionType)(b & 0b11);
            respectFriendlyFire = (b & 0b100) == 0b100;
            canDamageSelf = (b & 0b1000) == 0b1000;
            canDamageFriendly = (b & 0b10000) == 0b10000;
            canDamageEnemy = (b & 0b100000) == 0b100000;
        }

        public static Vector3 ToVector3(byte[] bytes, int offset)
        {
            return new Vector3(
                BitConverter.ToSingle(bytes, offset),
                BitConverter.ToSingle(bytes, offset + 4),
                BitConverter.ToSingle(bytes, offset + 8)
            );
        }

        public static Quaternion ToQuaternion(byte[] bytes, int offset)
        {
            return new Quaternion(
                BitConverter.ToSingle(bytes, offset),
                BitConverter.ToSingle(bytes, offset + 4),
                BitConverter.ToSingle(bytes, offset + 8),
                BitConverter.ToSingle(bytes, offset + 12)
            );
        }
    }
}