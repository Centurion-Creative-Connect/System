using CenturionCC.System.Player;
using NUnit.Framework;

namespace CenturionCC.System.Tests
{
    public class DamageDataSyncerTest
    {
        [Test]
        public void TestEncoding()
        {
            Check(new EncodedData(1, 1, 2, SyncState.Sending, SyncResult.Unassigned, KillType.Default, BodyParts.Body));
            Check(new EncodedData(255, 255, 21, SyncState.Received, SyncResult.Hit, KillType.FriendlyFire,
                BodyParts.Head));
            return;

            void Check(EncodedData data)
            {
                var encoded = DamageDataSyncer.EncodeData(
                    data.sender, data.victim, data.attacker,
                    (int)data.state, (int)data.result, (int)data.type, (int)data.parts
                );

                DamageDataSyncer.DecodeData(
                    encoded,
                    out var s, out var v, out var a,
                    out var ss, out var sr, out var kt, out var pt
                );

                Assert.That(data.sender == s, "0x{0:X}, 0x{1:X}, Expected sender {2} == {3}", encoded, s, data.sender,
                    s);
                Assert.That(data.victim == v, "Expected victim {0} == {1}", data.victim, v);
                Assert.That(data.attacker == a, "Expected attacker {0} == {1}", data.attacker, a);
                Assert.That(data.state == ss, "Expected state {0} == {1}", data.state, ss);
                Assert.That(data.result == sr, "Expected result {0} == {1}", data.result, sr);
                Assert.That(data.type == kt, "Expected type {0} == {1}", data.type, kt);
                Assert.That(data.parts == pt, "Expected parts {0} == {1}", data.parts, pt);
            }
        }

        private readonly struct EncodedData
        {
            public readonly int sender;
            public readonly int victim;
            public readonly int attacker;
            public readonly SyncState state;
            public readonly SyncResult result;
            public readonly KillType type;
            public readonly BodyParts parts;

            public EncodedData(int s, int v, int a, SyncState ss, SyncResult sr, KillType kt, BodyParts bp)
            {
                sender = s;
                victim = v;
                attacker = a;
                state = ss;
                result = sr;
                type = kt;
                parts = bp;
            }
        }
    }
}