using System;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using DerpyNewbie.Common.UI;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class GunSummoner : UdonSharpBehaviour
    {
        [NonSerialized]
        private const string SummoningPopUpHint = "Logics/System/UI/SummonerUI/SummoningImage";
        [SerializeField]
        private byte gunVariationId;
        [SerializeField]
        private Transform summonPosition;
        [SerializeField]
        private float summonTime = 5F;
        [SerializeField]
        private bool staffOnly;

        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManager gunManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private RoleProvider roleProvider;

        private DateTime _lastSummonedTime;
        private PopUpImage _summoningPopUp;

        private void Start()
        {
            if (summonPosition == null)
                summonPosition = transform;
            if (_summoningPopUp == null)
            {
                var go = GameObject.Find(SummoningPopUpHint);
                if (go != null)
                    _summoningPopUp = go.GetComponent<PopUpImage>();
            }
        }

        public void Spawn()
        {
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(MasterOnly_TryToSpawn));
            gunManager.Logger.Log($"[GunSummoner-{gunVariationId}] Requested to spawn a gun as {gunVariationId}");
            if (_summoningPopUp)
            {
                var par = _summoningPopUp.transform.parent;
                par.SetParent(summonPosition, false);
                par.localPosition = new Vector3(0, -.2F, 0.3F);

                _summoningPopUp.ShowAndHideLater(summonTime);
            }
        }

        public void MasterOnly_TryToSpawn()
        {
            if (!Networking.IsMaster)
            {
                gunManager.Logger.LogError(
                    $"[GunSummoner-{gunVariationId}] You must be a master to execute {nameof(MasterOnly_TryToSpawn)}!");
                return;
            }

            var remote = gunManager.MasterOnly_Spawn(gunVariationId, summonPosition.position, summonPosition.rotation);
            gunManager.Logger.Log(
                remote
                    ? $"[GunSummoner-{gunVariationId}] Spawned {remote.name}"
                    : $"[GunSummoner-{gunVariationId}] Failed to spawn a gun");
        }

        public override void Interact()
        {
            if (staffOnly && !roleProvider.GetPlayerRole().IsGameStaff())
            {
                gunManager.Logger.LogWarn($"[GunSummoner-{gunVariationId}] This summoner is staff-only!");
                return;
            }

            if (DateTime.Now.Subtract(_lastSummonedTime).TotalSeconds < summonTime)
            {
                gunManager.Logger.LogWarn($"[GunSummoner-{gunVariationId}] You're trying to spawn a gun too fast!");
                return;
            }

            Spawn();
            _lastSummonedTime = DateTime.Now;
        }
    }
}