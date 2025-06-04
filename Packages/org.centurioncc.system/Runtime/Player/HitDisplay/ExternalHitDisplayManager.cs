using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace CenturionCC.System.Player.External.HitDisplay
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ExternalHitDisplayManager : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManagerBase playerManager;

        [FormerlySerializedAs("sourceRemoteHitDisplay")]
        [SerializeField]
        private GameObject sourceExternalHitDisplay;

        [SerializeField]
        private Transform parent;

        [SerializeField] [Tooltip("Always will play any hits (Local & Remote).\n" +
                                  "Local will play when local player was an attacker.\n" +
                                  "Remote will play when local player was not an attacker.")]
        private HitDisplaySetting playHitDisplay = HitDisplaySetting.Always;

        [SerializeField]
        private bool ignoreForLocalHit = true;

        [SerializeField]
        private bool playWhenLocalIsNotInGame = true;

        [SerializeField] [Tooltip("Bypasses Play Hit Display setting and plays any hits when you're in staff team")]
        private bool bypassPlayHitDisplayWhenStaffTeam = true;

        /// <summary>
        /// When to play HitDisplay?
        /// </summary>
        /// <remarks>
        /// When player was killed, This will be checked against player information.
        /// See <see cref="HitDisplaySetting"/> for each setting's behaviour.
        /// </remarks>
        /// <seealso cref="HitDisplaySetting"/>
        /// <seealso cref="playWhenLocalIsNotInGame"/>
        /// <seealso cref="bypassPlayHitDisplayWhenStaffTeam"/>
        [PublicAPI]
        public HitDisplaySetting PlayHitDisplay
        {
            get => playHitDisplay;
            set => playHitDisplay = value;
        }

        [PublicAPI]
        public bool IgnoreForLocalHit
        {
            get => ignoreForLocalHit;
            set => ignoreForLocalHit = value;
        }

        [PublicAPI]
        public bool PlayWhenLocalIsNotInGame
        {
            get => playWhenLocalIsNotInGame;
            set => playWhenLocalIsNotInGame = value;
        }

        [PublicAPI]
        public bool BypassPlayHitDisplayWhenStaffTeam
        {
            get => bypassPlayHitDisplayWhenStaffTeam;
            set => bypassPlayHitDisplayWhenStaffTeam = value;
        }

        private void Start()
        {
            playerManager.Subscribe(this);
            sourceExternalHitDisplay.SetActive(false);
            if (parent == null)
                parent = transform;
        }

        public override void OnPlayerKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            if (victim.IsLocal && IgnoreForLocalHit)
            {
                return;
            }

            var localPlayer = playerManager.GetLocalPlayer();
            if (localPlayer == null)
            {
                if (playWhenLocalIsNotInGame) Play(victim);
                return;
            }

            if (playerManager.IsStaffTeamId(localPlayer.TeamId) && bypassPlayHitDisplayWhenStaffTeam)
            {
                Play(victim);
                return;
            }

            switch (playHitDisplay)
            {
                default:
                case HitDisplaySetting.Always:
                {
                    Play(victim);
                    return;
                }
                case HitDisplaySetting.LocalOnly:
                {
                    if (attacker.IsLocal) Play(victim);
                    return;
                }
                case HitDisplaySetting.RemoteOnly:
                {
                    if (!attacker.IsLocal) Play(victim);
                    return;
                }
            }
        }

        public void Play(PlayerBase player)
        {
            if (player == null)
            {
                Debug.LogError("[ExternalHitDisplayManager] Provided player is null");
                return;
            }

            var obj = Instantiate(sourceExternalHitDisplay, parent);
            var hitDisplay = obj.GetComponent<ExternalHitDisplayBase>();
            hitDisplay.Play(player);
        }

        public void Clear()
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var obj = parent.GetChild(i);
                if (obj == null || obj.gameObject == sourceExternalHitDisplay)
                    continue;
                Destroy(obj.gameObject);
            }
        }
    }

    /// <summary>
    /// HitDisplay behaviour setting.
    /// </summary>
    /// <seealso cref="HitDisplaySetting.Always"/>
    /// <seealso cref="HitDisplaySetting.LocalOnly"/>
    /// <seealso cref="HitDisplaySetting.RemoteOnly"/>
    public enum HitDisplaySetting
    {
        /// <summary>
        /// Will play any hits.
        /// </summary>
        Always,

        /// <summary>
        /// Will only play when attacker was local.
        /// </summary>
        LocalOnly,

        /// <summary>
        /// Will only play when attacker was remote.
        /// </summary>
        RemoteOnly
    }
}