using System;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player.MassPlayer
{
    /// <summary>
    /// Pairs <see cref="PlayerModel"/> and <see cref="PlayerView"/> then updates collider position.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(21)]
    public class PlayerUpdater : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;

        [SerializeField] private GameObject playerViewSource;
        [SerializeField] [HideInInspector] private PlayerViewBase[] views;
        [SerializeField] private int viewCount = 12;
        [SerializeField] private int sortStep = 5;

        public int sortStepCount;
        private PlayerModel[] _distanceSortedModels;
        private int _lastSortStepIndex;
        private VRCPlayerApi _localPlayer;

        private PlayerViewBase[] _views = new PlayerViewBase[0];

        [PublicAPI]
        public int ViewCount
        {
            get => viewCount;
            set
            {
                foreach (var view in _views)
                {
                    if (view != null)
                        Destroy(view.gameObject);
                }

                _views = new PlayerViewBase[value];
                for (var i = 0; i < value; i++)
                {
                    var view = Instantiate(playerViewSource, transform, false);
                    view.name = "InstantiatedView" + i;
                    _views[i] = view.GetComponent<PlayerViewBase>();
                    _views[i].Init();
                }

                viewCount = value;

                PairViewAndModel();
            }
        }

        [PublicAPI] public int ModelCount => _distanceSortedModels.Length;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;

            UpdateModels();
            if (_distanceSortedModels.Length == 0)
                SendCustomEventDelayedFrames(nameof(UpdateModels), 1);

            sortStepCount = sortStep;

            if (playerViewSource == null)
            {
                Debug.LogWarning("[PlayerUpdater] PlayerViewSource is null!!!");

                if (views.Length != 0)
                {
                    playerViewSource = views[0].gameObject;
                    foreach (var t in views)
                    {
                        if (t.gameObject != playerViewSource)
                        {
                            Destroy(t.gameObject);
                        }
                    }

                    Debug.LogWarning(
                        $"[PlayerUpdater] PlayerViewSource was migrated to old view source {playerViewSource.name}. Please set it in Inspector directly.");
                }
                else
                {
                    Debug.LogError("[PlayerUpdater] PlayerViewSource is null. PlayerUpdater will not work properly!!!");
                }
            }

            ViewCount = viewCount; // construct views by assigning property

            playerManager.SubscribeCallback(this);
        }

        private void UpdateModels()
        {
            var playerModels = playerManager.GetPlayers();
            _distanceSortedModels = new PlayerModel[playerModels.Length];
            Array.Copy(playerModels, _distanceSortedModels, playerModels.Length);
        }

        public override void PostLateUpdate()
        {
            SortStep();
            ViewUpdate();
        }

        public override void OnPlayerChanged(PlayerBase player, int oldId, int newId)
        {
            PairViewAndModel();
        }

        public PlayerModel[] GetSortedPlayerModels()
        {
            return _distanceSortedModels;
        }

        public string GetOrder()
        {
            var r = "";
            for (var i = 0; i < _distanceSortedModels.Length; i++)
            {
                var m = _distanceSortedModels[i];
                r +=
                    $"\n{i}: {m.name}, {(m.PlayerView != null ? m.PlayerView.name : null)}, {m.Position.ToString("F2")}, {NewbieUtils.GetPlayerName(m.PlayerId)}";
            }

            return r;
        }

        private void SortStep()
        {
            var localPos = _localPlayer.GetPosition();
            var modelCount = _distanceSortedModels.Length;
            var viewCount = _views.Length;

            for (var f = 0; f < sortStepCount; f++)
            {
                var index = f + _lastSortStepIndex;
                if (modelCount <= index + 1)
                    break;

                // Try to sort models by distance step by step
                // Because this is called every frame, it's ok to not sort completely
                var a = _distanceSortedModels[index];
                var b = _distanceSortedModels[index + 1];
                var modelAtIndex = a;

                // Distance check by sqrMag because it's faster than Vector.Distance
                if ((localPos - b.Position).sqrMagnitude < (localPos - a.Position).sqrMagnitude)
                {
                    // Cannot use deconstruction in U#
                    // ReSharper disable once SwapViaDeconstruction
                    _distanceSortedModels[index] = b;
                    _distanceSortedModels[index + 1] = a;
                    modelAtIndex = b;
                }

                // Pair up current index view & model
                if (index < viewCount)
                {
                    var view = _views[index];
                    view.PlayerModel = modelAtIndex;
                    modelAtIndex.PlayerView = view;
                }
                else
                {
                    modelAtIndex.PlayerView = null;
                }
            }

            _lastSortStepIndex += sortStepCount;
            if (_lastSortStepIndex >= modelCount)
                _lastSortStepIndex = 0;
        }

        private void PairViewAndModel()
        {
            for (var i = 0; i < _distanceSortedModels.Length; i++)
            {
                if (i < _views.Length)
                {
                    _distanceSortedModels[i].PlayerView = _views[i];
                    _views[i].PlayerModel = _distanceSortedModels[i];
                }
                else
                {
                    _distanceSortedModels[i].PlayerView = null;
                }
            }
        }

        private void ViewUpdate()
        {
            foreach (var view in _views) view.UpdateCollider();
        }
    }
}