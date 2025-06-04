using System;
using System.Reflection;
using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using DerpyNewbie.Common.Role;
using NUnit.Framework;
using UnityEngine;
using VRC.SDKBase;
using NotImplementedException = System.NotImplementedException;

namespace CenturionCC.System.Tests.Editor
{
    internal class MockPlayerBase : PlayerBase
    {
        private readonly bool _isDead;
        private readonly RoleData[] _roles;
        private readonly int _teamId;

        internal MockPlayerBase(int teamId, bool isDead, RoleData[] roles)
        {
            _teamId = teamId;
            _isDead = isDead;
            _roles = roles;
        }

        public override float Health { get; }
        public override float MaxHealth { get; }
        public override int TeamId => _teamId;
        public override int Kills { get; set; }
        public override int Deaths { get; set; }
        public override VRCPlayerApi VrcPlayer { get; }
        public override RoleData[] Roles => _roles;
        public override bool IsDead => _isDead;

        public override void UpdateView()
        {
            throw new NotImplementedException();
        }

        public override void ResetToDefault()
        {
            throw new NotImplementedException();
        }

        public override void SetTeam(int teamId)
        {
            throw new NotImplementedException();
        }

        public override void SetHealth(float health)
        {
            throw new NotImplementedException();
        }

        public override void SetMaxHealth(float maxHealth)
        {
            throw new NotImplementedException();
        }

        public override void OnLocalHit(PlayerColliderBase playerCollider, DamageData data, Vector3 contactPoint)
        {
            throw new NotImplementedException();
        }

        public override void ApplyDamage(DamageInfo info)
        {
            throw new NotImplementedException();
        }

        public override void Kill()
        {
            throw new NotImplementedException();
        }

        public override void Revive()
        {
            throw new NotImplementedException();
        }
    }

    internal class MockPlayerManager : PlayerManagerBase
    {
        private PlayerBase _mockLocalPlayer;
        private PlayerBase[] _mockPlayers;

        internal MockPlayerManager(PlayerBase mockLocalPlayer, PlayerBase[] mockPlayers)
        {
            _mockLocalPlayer = mockLocalPlayer;
            _mockPlayers = mockPlayers;
        }

        public override bool IsDebug { get; set; }
        public override bool ShowTeamTag { get; protected set; }
        public override bool ShowStaffTag { get; protected set; }
        public override bool ShowCreatorTag { get; protected set; }
        public override FriendlyFireMode FriendlyFireMode { get; protected set; }

        public override PlayerBase GetLocalPlayer()
        {
            return _mockLocalPlayer;
        }

        public override PlayerBase GetPlayer(VRCPlayerApi player)
        {
            return player == null ? null : _mockPlayers[player.playerId];
        }

        public override PlayerBase[] GetPlayers()
        {
            return _mockPlayers;
        }

        public override void SetPlayerTag(TagType type, bool isOn)
        {
            throw new NotImplementedException();
        }

        public override void SetFriendlyFireMode(FriendlyFireMode mode)
        {
            throw new NotImplementedException();
        }

        public override Color GetTeamColor(int teamId)
        {
            throw new NotImplementedException();
        }
    }

    public class PlayerManagerBaseTest
    {
        private static RoleData Create(string roleName, string[] roleProperties)
        {
            // ReSharper disable Unity.IncorrectMonoBehaviourInstantiation
            var gameObject = new GameObject("[DESTROY_ME] RoleDataTestObject");
            var roleData = gameObject.AddComponent<RoleData>();
            // ReSharper restore Unity.IncorrectMonoBehaviourInstantiation

            Assert.That(roleData, Is.Not.Null);

            var type = roleData.GetType();
            type.GetField("roleName", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(roleData, roleName);

            type.GetField("roleProperties", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(roleData, roleProperties);

            Assert.That(roleData.RoleName, Is.EqualTo(roleName), "RoleName does not match");
            Assert.That(roleData.RoleProperties, Is.EquivalentTo(roleProperties), "RoleProperties does not match");
            return roleData;
        }

        [Test]
        public void TestGetPlayerUtilities()
        {
            var userRole = Create("User", Array.Empty<string>());
            var staffRole = Create("Staff", new[] { "staff", "moderator" });

            // ReSharper disable Unity.IncorrectMonoBehaviourInstantiation
            PlayerBase[] mockPlayers =
            {
                new MockPlayerBase(0, false, new[] { userRole }),
                new MockPlayerBase(0, false, new[] { staffRole }),
                new MockPlayerBase(0, true, new[] { userRole }),
                new MockPlayerBase(0, true, new[] { staffRole }),
                new MockPlayerBase(1, false, new[] { userRole }),
                new MockPlayerBase(1, false, new[] { staffRole }),
                new MockPlayerBase(1, true, new[] { userRole }),
                new MockPlayerBase(1, true, new[] { staffRole }),
                new MockPlayerBase(1, false, new[] { userRole }),
                new MockPlayerBase(1, false, new[] { staffRole }),
                new MockPlayerBase(1, true, new[] { userRole }),
                new MockPlayerBase(1, true, new[] { staffRole })
            };

            var mockLocalPlayer = new MockPlayerBase(0, false, new[] { userRole });
            var mockPlayerManager = new MockPlayerManager(mockLocalPlayer, mockPlayers);

            // ReSharper restore Unity.IncorrectMonoBehaviourInstantiation


            CheckTeam(0, 4);
            CheckTeam(1, 8);

            CheckDead(6);
            CheckModerators(6);

            CheckDeadInTeam(0, 2);
            CheckDeadInTeam(1, 4);

            CheckModeratorsInTeam(0, 2);
            CheckModeratorsInTeam(1, 4);

            return;

            void CheckTeam(int teamId, int length)
            {
                var teamPlayers = mockPlayerManager.GetPlayersInTeam(teamId);
                Assert.That(teamPlayers.Length, Is.EqualTo(length), "TeamPlayers length does not match");
                foreach (var player in teamPlayers)
                {
                    Assert.That(player.TeamId, Is.EqualTo(teamId), "TeamId does not match");
                }
            }

            void CheckDead(int length)
            {
                var deadPlayers = mockPlayerManager.GetDeadPlayers();
                Assert.That(deadPlayers.Length, Is.EqualTo(length), "DeadPlayers length does not match");
                foreach (var player in deadPlayers)
                {
                    Assert.That(player.IsDead, Is.True, "IsDead does not match");
                }
            }

            void CheckModerators(int length)
            {
                var moderatorPlayers = mockPlayerManager.GetModeratorPlayers();
                Assert.That(moderatorPlayers.Length, Is.EqualTo(length), "ModPlayers length does not match");
                foreach (var player in moderatorPlayers)
                {
                    Assert.IsTrue(player.Roles.HasPermission(), "player.Roles.HasPermission() returned false");
                }
            }

            void CheckDeadInTeam(int teamId, int length)
            {
                var deadPlayers = mockPlayerManager.GetDeadPlayersInTeam(teamId);
                Assert.That(deadPlayers.Length, Is.EqualTo(length), "DeadPlayers length does not match");
                foreach (var player in deadPlayers)
                {
                    Assert.That(player.IsDead, Is.True, "IsDead does not match");
                }
            }

            void CheckModeratorsInTeam(int teamId, int length)
            {
                var moderatorPlayers = mockPlayerManager.GetModeratorPlayersInTeam(teamId);
                Assert.That(moderatorPlayers.Length, Is.EqualTo(length), "ModPlayers length does not match");
                foreach (var player in moderatorPlayers)
                {
                    Assert.IsTrue(player.Roles.HasPermission(), "player.Roles.HasPermission() returned false");
                }
            }
        }
    }
}