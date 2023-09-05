using System;
using CenturionCC.System.Player;
using NUnit.Framework;
using UnityEngine;

namespace CenturionCC.System.Tests
{
    public class DamageDataResolverTest
    {
        private DamageDataResolver _instance;

        [SetUp]
        public void Setup()
        {
            _instance = new GameObject().AddComponent<DamageDataResolver>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_instance.gameObject);
            _instance = null;
        }

        [Test]
        public void TestInvincibleTime()
        {
            _instance.InvincibleDuration = 10D;
            var now = new DateTime(2022, 3, 25, 1, 0, 0);
            Assert.That(_instance.IsInInvincibleDuration(now, now));
            Assert.That(_instance.IsInInvincibleDuration(now, now.AddSeconds(-1D)));
            Assert.That(_instance.IsInInvincibleDuration(now, now.AddSeconds(-9D)));
            Assert.That(_instance.IsInInvincibleDuration(now, now.AddSeconds(-10D)) == false);
            Assert.That(_instance.IsInInvincibleDuration(now, now.AddSeconds(-11D)) == false);
            Assert.That(_instance.IsInInvincibleDuration(now, now.AddSeconds(1D)) == false);
        }

        [Test]
        public void TestComputeHitResultFromDateTime()
        {
            _instance.InvincibleDuration = 10D;
            var now = new DateTime(2022, 3, 25, 1, 0, 0);
            var outOfDuration = now.AddSeconds(-50D);

            void Check(DateTime origin, DateTime hit, DateTime atkDied, DateTime vicDied, bool expected)
            {
                var result = _instance.ComputeHitResultFromDateTime(origin, hit, atkDied, vicDied);
                Assert.That(result == expected, "Expected: {0}\nBut was: {1}", expected, result);
            }

            Check(now.AddSeconds(10D), now.AddSeconds(10D), now, outOfDuration, true);
            Check(now.AddSeconds(10D), now.AddSeconds(11D), now, outOfDuration, true);

            Check(now.AddSeconds(10D), now.AddSeconds(10D), outOfDuration, now, true);
            Check(now.AddSeconds(10D), now.AddSeconds(11D), outOfDuration, now, true);

            Check(now.AddSeconds(1D), now, now, now, false);
            Check(now.AddSeconds(11D), now.AddSeconds(10D), now, outOfDuration, false);
            Check(now.AddSeconds(11D), now.AddSeconds(10D), outOfDuration, now, false);

            Check(now, now, now, now, false);
            Check(now.AddSeconds(1D), now.AddSeconds(2D), now, outOfDuration, false);
            Check(now.AddSeconds(9D), now.AddSeconds(10D), now, outOfDuration, false);

            Check(now, now.AddSeconds(1D), outOfDuration, now, false);
            Check(now, now.AddSeconds(9D), outOfDuration, now, false);
        }
    }
}