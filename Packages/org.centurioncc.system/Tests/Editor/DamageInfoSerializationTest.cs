using System;
using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using NUnit.Framework;
using UnityEngine.TestTools;
using Random = UnityEngine.Random;

namespace CenturionCC.System.Tests.Editor
{
    public class DamageInfoSerializationTest
    {
        [Test]
        public void DamageInfoByteSerializationTest()
        {
            void CheckEqual(DamageInfo lhs, DamageInfo rhs)
            {
                Assert.That(lhs.EventId(), Is.EqualTo(rhs.EventId()), "EventId does not match");
                Assert.That(lhs.AttackerId(), Is.EqualTo(rhs.AttackerId()), "AttackerId does not match");
                Assert.That(lhs.VictimId(), Is.EqualTo(rhs.VictimId()), "VictimId does not match");
                Assert.That(lhs.DamageType(), Is.EqualTo(rhs.DamageType()), "DamageType does not match");
                Assert.That(lhs.HitParts(), Is.EqualTo(rhs.HitParts()), "HitParts does not match");
                Assert.That(lhs.HitPosition(), Is.EqualTo(rhs.HitPosition()), "HitPosition does not match");
                Assert.That(lhs.HitTime(), Is.EqualTo(rhs.HitTime()), "HitTime does not match");
                Assert.That(lhs.OriginatedPosition(), Is.EqualTo(rhs.OriginatedPosition()),
                    "OriginatedPosition does not match");
                Assert.That(lhs.OriginatedRotation(), Is.EqualTo(rhs.OriginatedRotation()),
                    "OriginatedRotation does not match");
                Assert.That(lhs.OriginatedTime(), Is.EqualTo(rhs.OriginatedTime()), "OriginatedTime does not match");
                Assert.That(lhs.CanDamageSelf(), Is.EqualTo(rhs.CanDamageSelf()), "CanDamageSelf does not match");
                Assert.That(lhs.CanDamageFriendly(), Is.EqualTo(rhs.CanDamageFriendly()),
                    "CanDamageFriendly does not match");
                Assert.That(lhs.CanDamageEnemy(), Is.EqualTo(rhs.CanDamageEnemy()), "CanDamageEnemy does not match");
                Assert.That(lhs.RespectFriendlyFireSetting(), Is.EqualTo(rhs.RespectFriendlyFireSetting()),
                    "RespectFriendlyFireSetting does not match");
            }

            for (var i = 0; i < 100; i++)
            {
                var original = DamageInfo.New(
                    Guid.NewGuid(),
                    Random.Range(1, int.MaxValue),
                    Random.Range(1, int.MaxValue),
                    Random.onUnitSphere * Random.Range(int.MinValue, int.MaxValue),
                    (BodyParts)Random.Range(0, 6),
                    Random.onUnitSphere * Random.Range(int.MinValue, int.MaxValue),
                    Random.rotation,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    "TestDamageType",
                    Random.value,
                    (DetectionType)Random.Range(0, 3),
                    Random.Range(0, 1) == 0,
                    Random.Range(0, 1) == 0,
                    Random.Range(0, 1) == 0,
                    Random.Range(0, 1) == 0
                );

                var deserialized = DamageInfo.FromBytes(original.ToBytes());
                CheckEqual(original, deserialized);
            }
        }
    }
}