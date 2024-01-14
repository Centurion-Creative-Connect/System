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
        [SerializeField]
        private byte gunVariationId;
        [SerializeField]
        private bool useRandomIds;
        [SerializeField]
        private byte[] randomPoolIds;
        [SerializeField]
        private Transform summonPosition;
        [SerializeField]
        private Transform popupPosition;
        [SerializeField]
        private float summonTime = 5F;
        [SerializeField]
        private bool staffOnly;

        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManager gunManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private RoleProvider roleProvider;
        [SerializeField] [HideInInspector] [NewbieInject]
        private NewbieLogger logger;
        private readonly string[] _summoningPopupHints =
        {
            "Logics/System/UI/SummonerUI/SummoningImage",
            "Logics/UI/SummonerUI/SummoningImage"
        };

        private DateTime _lastSummonedTime;
        private PopUpImage _summoningPopUp;

        private void Start()
        {
            if (summonPosition == null)
                summonPosition = transform;
            if (popupPosition == null)
                popupPosition = summonPosition;

            if (_summoningPopUp == null)
            {
                foreach (var hint in _summoningPopupHints)
                {
                    var go = GameObject.Find(hint);
                    if (go == null) continue;
                    _summoningPopUp = go.GetComponent<PopUpImage>();
                    break;
                }
            }
        }

        public void Spawn()
        {
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(MasterOnly_TryToSpawn));
            logger.Log(
                $"[GunSummoner-{gunVariationId}{(useRandomIds ? "*" : "")}] Requested to spawn a gun as {gunVariationId}");
            if (_summoningPopUp)
            {
                var par = _summoningPopUp.transform.parent;
                par.SetParent(popupPosition, false);
                par.localPosition = summonPosition == popupPosition ? new Vector3(0, -.2F, 0.3F) : Vector3.zero;

                _summoningPopUp.ShowAndHideLater(summonTime);
            }
        }

        public void MasterOnly_TryToSpawn()
        {
            if (!Networking.IsMaster)
            {
                logger.LogError(
                    $"[GunSummoner-{gunVariationId}{(useRandomIds ? "*" : "")}] You must be a master to execute {nameof(MasterOnly_TryToSpawn)}!");
                return;
            }

            var varId = gunVariationId;
            if (useRandomIds && randomPoolIds.Length != 0)
                varId = randomPoolIds[UnityEngine.Random.Range(0, randomPoolIds.Length)];
            var remote = gunManager.MasterOnly_Spawn(varId, summonPosition.position, summonPosition.rotation);
            logger.Log(
                remote
                    ? $"[GunSummoner-{gunVariationId}{(useRandomIds ? $"*({varId})" : "")}] Spawned {remote.name}"
                    : $"[GunSummoner-{gunVariationId}{(useRandomIds ? $"*({varId})" : "")}] Failed to spawn a gun");
        }

        public override void Interact()
        {
            if (staffOnly && !roleProvider.GetPlayerRole().IsGameStaff())
            {
                logger.LogWarn(
                    $"[GunSummoner-{gunVariationId}{(useRandomIds ? "*" : "")}] This summoner is staff-only!");
                return;
            }

            if (DateTime.Now.Subtract(_lastSummonedTime).TotalSeconds < summonTime)
            {
                logger.LogWarn(
                    $"[GunSummoner-{gunVariationId}{(useRandomIds ? "*" : "")}] You're trying to spawn a gun too fast!");
                return;
            }

            Spawn();
            _lastSummonedTime = DateTime.Now;
        }
    }
}