using System;
using CenturionCC.System.Player;
using NUnit.Framework;

namespace CenturionCC.System.Tests
{
    public class DamageDataResolverInvincibleTest
    {
        [Test]
        public void TestComputeHitResultFromDateTime()
        {
            var now = new DateTime(2022, 3, 25, 1, 0, 0);
            var after = now.AddSeconds(5);
            var before = now.AddSeconds(-5D);

            // Origin before
            Check(before, before, before, true);
            Check(before, now, before, true);
            Check(before, after, before, true);

            Check(before, before, now, true);
            Check(before, now, now, true);
            Check(before, after, now, false);

            Check(before, before, after, true);
            Check(before, now, after, true);
            Check(before, after, after, true);


            // Origin now
            Check(now, before, before, true);
            Check(now, now, before, true);
            Check(now, after, before, true);

            Check(now, before, now, true);
            Check(now, now, now, true);
            Check(now, after, now, true);

            Check(now, before, after, false);
            Check(now, now, after, true);
            Check(now, after, after, true);


            // Origin after
            Check(after, before, before, true);
            Check(after, now, before, false);
            Check(after, after, before, true);

            Check(after, before, now, true);
            Check(after, now, now, true);
            Check(after, after, now, true);

            Check(after, before, after, true);
            Check(after, now, after, true);
            Check(after, after, after, true);
            return;

            void Check(DateTime origin, DateTime atkDied, DateTime atkRev, bool expected)
            {
                var result = DamageDataResolver.ComputeHitResultFromDateTime(origin, atkDied, atkRev);
                Assert.That(result, Is.EqualTo(expected));
            }
        }
    }
}