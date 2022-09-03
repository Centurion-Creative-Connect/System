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

        private string _currentFootstepType = FT_Fallback;
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
            if (!player || player.Team == 4) return;
            var isSlow = _timer > slowFootstepThreshold;

            switch (_currentFootstepType)
            {
                case FT_NoAudio:
                    break;
                case FT_Ground:
                    player.SendCustomNetworkEvent(NetworkEventTarget.All,
                        isSlow
                            ? nameof(player.PlaySlowGroundFootstepAudio)
                            : nameof(player.PlayGroundFootstepAudio));
                    break;
                case FT_Wood:
                    player.SendCustomNetworkEvent(NetworkEventTarget.All,
                        isSlow
                            ? nameof(player.PlaySlowWoodFootstepAudio)
                            : nameof(player.PlayWoodFootstepAudio));
                    break;
                default:
                    player.SendCustomNetworkEvent(NetworkEventTarget.All,
                        isSlow
                            ? nameof(player.PlaySlowFallbackFootstepAudio)
                            : nameof(player.PlayFallbackFootstepAudio));
                    break;
            }
        }

        private void CheckFootstepType()
        {
            const int layerMask = 1 << 11;
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 3, layerMask))
            {
                if (!hit.transform)
                    return;
                var footstepMarker = hit.transform.GetComponent<FootstepMarker>();
                if (!footstepMarker)
                    footstepMarker = hit.transform.parent.GetComponent<FootstepMarker>();
                if (!footstepMarker)
                    return;
                _currentFootstepType = footstepMarker.FootstepType;
            }
        }

        #region FootstepType

        // ReSharper disable InconsistentNaming
        private const string FT_Fallback = "Fallback";
        private const string FT_Ground = "Ground";
        private const string FT_Wood = "Wood";
        private const string FT_NoAudio = "NoAudio";
        // ReSharper restore InconsistentNaming

        #endregion
    }
}