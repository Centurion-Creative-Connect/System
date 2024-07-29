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
    public class PlayerUpdater : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;

        [SerializeField] private PlayerModel[] models;

        [SerializeField] private PlayerViewBase[] views;

        [SerializeField] private int sortStep = 5;

        public int sortStepCount;

        private PlayerModel[] _distanceSortedModels;
        private int _lastSortStepIndex;
        private VRCPlayerApi _localPlayer;

        [PublicAPI] public int ModelCount => models.Length;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            _distanceSortedModels = new PlayerModel[models.Length];
            Array.Copy(models, _distanceSortedModels, models.Length);

            sortStepCount = sortStep;

            playerManager.SubscribeCallback(this);
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
                    $"\n{i}: {m.name}, {(m.playerView != null ? m.playerView.name : null)}, {m.Position.ToString("F2")}, {NewbieUtils.GetPlayerName(m.PlayerId)}";
            }

            return r;
        }

        private void SortStep()
        {
            var localPos = _localPlayer.GetPosition();
            var modelCount = _distanceSortedModels.Length;
            var viewCount = views.Length;

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
                    var view = views[index];
                    view.PlayerModel = modelAtIndex;
                    modelAtIndex.playerView = view;
                }
                else
                {
                    modelAtIndex.playerView = null;
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
                if (i < views.Length)
                {
                    _distanceSortedModels[i].playerView = views[i];
                    views[i].PlayerModel = _distanceSortedModels[i];
                }
                else
                {
                    _distanceSortedModels[i].playerView = null;
                }
            }
        }

        private void ViewUpdate()
        {
            foreach (var view in views)
                view.UpdateCollider();

            // if (!playerManager.IsDebug) return;
            //
            // foreach (var view in views)
            // {
            //     var modelName = "N/A";
            //     var playerId = 0;
            //     var teamId = 0;
            //     var kills = 0;
            //     var deaths = 0;
            //     var kdr = float.NaN;
            //
            //     if (view.playerModel != null)
            //     {
            //         var model = view.playerModel;
            //         modelName = model.name;
            //         playerId = model.PlayerId;
            //         teamId = model.TeamId;
            //         kills = model.Kills;
            //         deaths = model.Deaths;
            //         kdr = kills / (float)deaths;
            //     }
            //
            //     view.playerTag.SetDebugTagText($"Name  : {view.name}\n" +
            //                                    $"Model : {modelName}\n" +
            //                                    $"Player: {playerId}\n" +
            //                                    $"Team  : {teamId}\n" +
            //                                    $"KD(R) : {kills}/{deaths} ({kdr})");
            // }
        }
    }
}