using System;
using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Audio
{
    /// <summary>
    /// FootstepGenerator determines which surface the player is on,
    /// and invokes network event at local <see cref="PlayerBase"/> instance.
    /// </summary>
    /// <seealso cref="FootstepMarker"/>
    /// <seealso cref="Utils.ObjectMarker"/>
    /// <seealso cref="PlayerManager.GetLocalPlayer()"/>
    [Obsolete("Use PlayerController instead.")] [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FootstepGenerator : UdonSharpBehaviour
    {
        [SerializeField]
        private PlayerManager playerManager;
        [SerializeField] [Range(0, 2)] [UdonSynced]
        [Tooltip("Plays footstep sound if player went amount of further away")]
        public float footstepTime = 1F;
        [SerializeField] [Range(0, 2)] [UdonSynced]
        [Tooltip("Plays footstep sound if player went footstepLength far this timeframe in seconds")]
        public float footstepLength = 0.9F;
        [SerializeField] [Range(0, 2)] [UdonSynced]
        [Tooltip("Plays slow footstep sound if player played footstep this further than footstep timer")]
        public float slowFootstepThreshold = 0.45F;

        private FootstepType _currentFootstepType = FootstepType.Fallback;
        private Vector3 _lastPlayedPosition = Vector3.zero;
        private bool _lastPlayerGrounded;
        private float _timer;
        [NonSerialized] [UdonSynced]
        public bool PlayFootstep = true;

        private void FixedUpdate()
        {
            if (!PlayFootstep) return;
            _timer += Time.deltaTime;

            if (Networking.LocalPlayer == null || Networking.LocalPlayer.IsPlayerGrounded() == false) return;

            var currentPosition = transform.position;

            if (Vector3.Distance(currentPosition, _lastPlayedPosition) > footstepTime)
            {
                if (_timer < footstepLength && CheckFootstepType())
                {
                    PlayFootstepSound();
                }

                _timer = 0F;
                _lastPlayedPosition = currentPosition;
            }
        }

        public void Apply()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        private void PlayFootstepSound()
        {
            var player = playerManager.GetLocalPlayer();
            if (player == null || playerManager.IsStaffTeamId(player.TeamId)) return;
            var isSlow = _timer > slowFootstepThreshold;

            switch (_currentFootstepType)
            {
                case FootstepType.NoAudio:
                    break;
                case FootstepType.Gravel:
                    player.SendCustomNetworkEvent(NetworkEventTarget.All,
                        isSlow
                            ? "PlaySlowGravelFootstepAudio"
                            : "PlayGravelFootstepAudio");
                    break;
                case FootstepType.Wood:
                    player.SendCustomNetworkEvent(NetworkEventTarget.All,
                        isSlow
                            ? "PlaySlowWoodFootstepAudio"
                            : "PlayWoodFootstepAudio");
                    break;
                case FootstepType.Iron:
                    player.SendCustomNetworkEvent(NetworkEventTarget.All,
                        isSlow
                            ? "PlaySlowMetallicFootstepAudio"
                            : "PlayMetallicFootstepAudio");
                    break;
                case FootstepType.Fallback:
                default:
                    player.SendCustomNetworkEvent(NetworkEventTarget.All,
                        isSlow
                            ? "PlaySlowPrototypeFootstepAudio"
                            : "PlayPrototypeFootstepAudio");
                    break;
            }
        }

        private bool CheckFootstepType()
        {
            const int layerMask = 1 << 11;
            if (!Physics.Raycast(transform.position, Vector3.down, out var hit, 3, layerMask) || !hit.transform)
                return false;

            return TryGetFootstepMarker(hit);
        }

        private bool TryGetFootstepMarker(RaycastHit hit)
        {
            var footstepMarker = hit.transform.GetComponentInParent<FootstepMarker>();
            if (footstepMarker == null)
                return false;

            _currentFootstepType = footstepMarker.Type;
            return true;
        }

        private bool TryGetObjectMarker(RaycastHit hit)
        {
            var marker = hit.transform.GetComponent<ObjectMarkerBase>();
            if (marker == null || marker.Tags.ContainsString("NoFootstep"))
                return false;

            _currentFootstepType = ConvertToFootstepType(marker.ObjectType);
            return true;
        }

        private static FootstepType ConvertToFootstepType(ObjectType type)
        {
            switch (type)
            {
                case ObjectType.Wood:
                    return FootstepType.Wood;
                case ObjectType.Dirt:
                case ObjectType.Gravel:
                    return FootstepType.Gravel;
                case ObjectType.Metallic:
                    return FootstepType.Iron;
                case ObjectType.Concrete:
                case ObjectType.Prototype:
                default:
                    return FootstepType.Fallback;
            }
        }
    }
}