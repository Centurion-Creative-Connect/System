using System;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun.MassGun
{
    /// <summary>
    /// Sort models by distance then update closest <see cref="maxInstancesToUpdateFrequently"/> models.
    /// </summary>
    /// <remarks>
    /// This is more performant than it used to be because
    /// 1. No unnecessary Update or FixedUpdate calls happening on each instances.
    /// 2. Limiting maximum calls happening in one frame.
    /// Also more scalable because <see cref="maxInstancesToUpdateFrequently"/> and <see cref="sortStepCount"/>.
    /// </remarks>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunUpdater : UdonSharpBehaviour
    {
        [SerializeField]
        private GunModel[] models;
        [SerializeField] [HideInInspector] [NewbieInject]
        private UpdateManager updateManager;

        public int sortStepCount = 12;
        public int maxInstancesToUpdateFrequently = 5;
        private int _lastSortStepIndex;

        private GunModel[] distanceSortedModels;

        [PublicAPI]
        public int ModelCount => distanceSortedModels.Length;

        private void Start()
        {
            distanceSortedModels = new GunModel[models.Length];
            Array.Copy(models, distanceSortedModels, models.Length);

            // Delay update calls to prevent model crashing on `OnGunUpdate` call
            SendCustomEventDelayedFrames(nameof(SubscribeUpdateManager), 10);
        }

        public void SubscribeUpdateManager()
        {
            updateManager.SubscribePostLateUpdate(this);
            updateManager.SubscribeUpdate(this);
        }

        public void _PostLateUpdate()
        {
            for (var i = 0; i < maxInstancesToUpdateFrequently; i++)
                distanceSortedModels[i].OnGunUpdate();
        }

        public void _Update()
        {
            SortStep();
        }

        public string GetOrder()
        {
            var r = "";
            for (var i = 0; i < distanceSortedModels.Length; i++)
            {
                var m = distanceSortedModels[i];
                r +=
                    $"\n{i}: {m.name}, {m.Position.ToString("F2")}, {m.WeaponName}, {(m.CurrentHolder != null ? m.CurrentHolder.displayName : "N/A")}";
            }

            return r;
        }

        private void SortStep()
        {
            var localPos = Networking.LocalPlayer.GetPosition();
            var modelCount = distanceSortedModels.Length;

            for (var i = 0; i < sortStepCount; i++)
            {
                var index = i + _lastSortStepIndex;
                if (modelCount <= index + 1)
                    break;

                // Try to sort models by distance step by step
                // Because this is called every frame, it's ok to not sort completely
                var a = distanceSortedModels[index];
                var b = distanceSortedModels[index + 1];

                // Distance check by sqrMag because it's faster than Vector.Distance
                if ((localPos - b.Position).sqrMagnitude < (localPos - a.Position).sqrMagnitude)
                {
                    // Cannot use deconstruction in U#
                    // ReSharper disable once SwapViaDeconstruction
                    distanceSortedModels[index] = b;
                    distanceSortedModels[index + 1] = a;
                }

                if (a.Model != null)
                    a.Model.SetActive(index < maxInstancesToUpdateFrequently);
            }

            _lastSortStepIndex += sortStepCount;
            if (_lastSortStepIndex >= modelCount)
                _lastSortStepIndex = 0;
        }
    }
}