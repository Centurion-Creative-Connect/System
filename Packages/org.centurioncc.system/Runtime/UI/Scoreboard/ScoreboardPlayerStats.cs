using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace CenturionCC.System.UI.Scoreboard
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ScoreboardPlayerStats : UdonSharpBehaviour
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManagerBase gunManager;

        [SerializeField] private Text rankingText;
        [SerializeField] private Text displayNameText;
        [SerializeField] private Text killsText;
        [SerializeField] private Text deathsText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text weaponText;

        private PlayerBase _source;

        public PlayerBase Source
        {
            get => _source;
            set
            {
                _source = value;
                UpdateText();
            }
        }

        public int GetPriority()
        {
            return Source != null ? Source.Kills * 100 - Source.Deaths : -1;
        }

        public void UpdateText()
        {
            if (Source == null)
            {
                SetText(rankingText, "??");
                SetText(displayNameText, "???(InvalidSource)");
                SetText(weaponText, "???");
                SetText(killsText, "??");
                SetText(deathsText, "??");
                SetText(scoreText, "????");
                return;
            }

            SetText(rankingText, $"{(transform.GetSiblingIndex() + 1)}");
            SetText(displayNameText, Source.VrcPlayer.SafeGetDisplayName("???(InvalidVrcPlayer)"));
            SetText(weaponText, SearchForHeldWeapon());
            SetText(killsText, Source.Kills.ToString());
            SetText(deathsText, Source.Deaths.ToString());
            SetText(scoreText, "????");
        }

        private string SearchForHeldWeapon(string unknown = "???")
        {
            if (Source == null) return unknown;
            var vrcPlayer = Source.VrcPlayer;
            if (vrcPlayer == null || !vrcPlayer.IsValid()) return unknown;

            var gunBase = TryGetGunBase(vrcPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Left));
            if (gunBase == null)
                gunBase = TryGetGunBase(vrcPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right));

            return gunBase == null ? unknown : gunBase.WeaponName;
        }

        private static GunBase TryGetGunBase(VRC_Pickup pickup)
        {
            var handle = pickup.GetComponent<GunHandle>();
            if (handle == null) return null;
            return (GunBase)handle.callback;
        }

        private static void SetText(Text t, string msg)
        {
            if (t == null) return;
            t.text = msg;
        }
    }
}
