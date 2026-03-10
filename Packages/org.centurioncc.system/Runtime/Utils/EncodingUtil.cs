using System;
using UnityEngine;

namespace CenturionCC.System.Utils
{
    public static class EncodingUtil
    {
        /// <summary>
        /// 1 byte to represent a float
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte PackedFloat(float value)
        {
            return (byte)((int)(value * 100) & 0xFF);
        }

        public static float UnpackFloat(byte value)
        {
            return value / 100f;
        }

        /// <summary>
        /// 3 bytes to represent a Vector3
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int PackedVector3(Vector3 value)
        {
            return PackedFloat(value.x) | (PackedFloat(value.y) << 8) | (PackedFloat(value.z) << 16);
        }

        public static Vector3 UnpackVector3(int value)
        {
            return new Vector3(UnpackFloat((byte)(value & 0xFF)), UnpackFloat((byte)((value >> 8) & 0xFF)), UnpackFloat((byte)((value >> 16) & 0xFF)));
        }

        /// <summary>
        /// 4 bytes to represent a Quaternion
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int PackedQuaternion(Quaternion value)
        {
            return PackedFloat(value.x) | (PackedFloat(value.y) << 8) | (PackedFloat(value.z) << 16) | (PackedFloat(value.w) << 24);
        }

        public static Quaternion UnpackQuaternion(int value)
        {
            return new Quaternion(UnpackFloat((byte)(value & 0xFF)), UnpackFloat((byte)((value >> 8) & 0xFF)), UnpackFloat((byte)((value >> 16) & 0xFF)), UnpackFloat((byte)((value >> 24) & 0xFF))).normalized;
        }

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

        public static string ToBinaryString(int flags, int length = 8)
        {
            var buff = "";
            for (var i = 0; i < length; i++)
            {
                buff += (flags & 1) == 0 ? "0" : "1";
                flags >>= 1;
            }

            return buff;
        }

        public static string ToBinaryString(ulong flags, int length = 8)
        {
            var buff = "";
            for (var i = 0; i < length; i++)
            {
                buff += (flags & 1) == 0 ? "0" : "1";
                flags >>= 1;
            }

            return buff;
        }
    }
}
