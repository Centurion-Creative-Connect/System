using System;
using CenturionCC.System.Player;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Audio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
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
            if (!player || playerManager.IsStaffTeamId(player.TeamId)) return;
            var isSlow = _timer > slowFootstepThreshold;

            switch (_currentFootstepType)
            {
                case FootstepType.NoAudio:
                    break;
                case FootstepType.Gravel:
                    player.SendCustomNetworkEvent(NetworkEventTarget.All,
                        isSlow
                            ? "PlaySlowGroundFootstepAudio"
                            : "PlayGroundFootstepAudio");
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
                            ? "PlaySlowIronFootstepAudio"
                            : "PlayIronFootstepAudio");
                    break;
                case FootstepType.Fallback:
                default:
                    player.SendCustomNetworkEvent(NetworkEventTarget.All,
                        isSlow
                            ? "PlaySlowFallbackFootstepAudio"
                            : "PlayFallbackFootstepAudio");
                    break;
            }
        }

        private bool CheckFootstepType()
        {
            const int layerMask = 1 << 11;
            if (!Physics.Raycast(transform.position, Vector3.down, out var hit, 3, layerMask) || !hit.transform)
                return false;

            var footstepMarker = hit.transform.GetComponentInParent<FootstepMarker>();
            if (footstepMarker == null)
                return false;

            _currentFootstepType = footstepMarker.Type;
            return true;
        }
    }
}