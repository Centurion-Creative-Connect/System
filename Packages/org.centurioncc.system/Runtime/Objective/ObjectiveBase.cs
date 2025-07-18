using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace CenturionCC.System.Objective
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class ObjectiveBase : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        protected ObjectiveManagerBase objectives;

        private bool _lastHasCompleted;

        private bool _lastIsPaused;

        private int _lastOwningTeamId = -1;

        private float _lastProgress;

        #region PublicAPI

        /// <summary>
        /// Set up Objective to be used by a team.
        /// </summary>
        /// <param name="teamId">New owning team. The Objective will be disabled if set to -1.</param>
        [PublicAPI]
        public void SetupObjective(int teamId)
        {
            Progress = 0;
            IsPaused = false;
            OwningTeamId = teamId;
            RequestSync();
        }

        #endregion

        #region NetworkingChecks

        public override void OnDeserialization()
        {
            CheckSyncedData();
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (result.success)
            {
                CheckSyncedData();
            }
        }

        protected void CheckSyncedData()
        {
            var owningTeamIdChanged = _lastOwningTeamId != OwningTeamId;
            if (owningTeamIdChanged)
            {
                objectives.Internal_RemoveObjective(this, _lastOwningTeamId);
                if (HasOwningTeam) objectives.Internal_AddObjective(this, OwningTeamId);
                _lastOwningTeamId = OwningTeamId;
            }

            var progressChanged = !Mathf.Approximately(_lastProgress, Progress);
            if (progressChanged)
            {
                objectives.Internal_OnObjectiveProgress(this);
                _lastProgress = Progress;
            }

            var hasCompletedChanged = _lastHasCompleted != HasCompleted;
            _lastHasCompleted = HasCompleted;

            if ((owningTeamIdChanged || hasCompletedChanged) && HasOwningTeam && !HasCompleted)
            {
                OnObjectiveStart();
            }

            var hasPausedChanged = _lastIsPaused != IsPaused;
            _lastIsPaused = IsPaused;

            if (HasOwningTeam && !HasCompleted && hasPausedChanged)
            {
                if (IsPaused) OnObjectivePause();
                else OnObjectiveResume();
            }

            if (progressChanged)
            {
                OnObjectiveProgress();
            }

            if (HasOwningTeam && HasCompleted && hasCompletedChanged)
            {
                OnObjectiveCompleted();
            }
        }

        #endregion

        #region ObjectiveBaseAPI

        protected void SetProgress(float progress)
        {
            Progress = progress;
            RequestSync();
        }

        protected void SetOwningTeamId(int teamId)
        {
            OwningTeamId = teamId;
            RequestSync();
        }

        protected void RequestSync()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        #endregion

        #region ObjectiveBaseCallbacks

        /// <summary>
        /// Called when objective goal has activated and started.
        /// </summary>
        protected virtual void OnObjectiveStart()
        {
        }

        /// <summary>
        /// Called when objective goal is paused and halt updates.
        /// </summary>
        protected virtual void OnObjectivePause()
        {
        }

        /// <summary>
        /// Called when objective goal should resume and continue updating.
        /// </summary>
        protected virtual void OnObjectiveResume()
        {
        }


        /// <summary>
        /// Called when objective goal has updated progress.
        /// </summary>
        protected virtual void OnObjectiveProgress()
        {
        }

        /// <summary>
        /// Called when objective goal has completed.
        /// </summary>
        protected virtual void OnObjectiveCompleted()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Objective's current team id.
        /// </summary>
        /// <remarks>
        /// Might change dynamically depending on the objective implementation.
        /// </remarks>
        [field: UdonSynced]
        [PublicAPI]
        public int OwningTeamId { get; private set; }

        [field: UdonSynced]
        [PublicAPI]
        public float Progress { get; private set; }

        [field: UdonSynced]
        [PublicAPI]
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Has the objective goal been completed? 
        /// </summary>
        [PublicAPI]
        public bool HasCompleted => Progress <= 1;

        [PublicAPI]
        public bool HasOwningTeam => OwningTeamId != -1;

        [PublicAPI]
        public bool IsActiveAndRunning => HasOwningTeam && !IsPaused && !HasCompleted;

        #endregion
    }
}