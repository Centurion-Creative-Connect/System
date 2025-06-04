using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common;

namespace CenturionCC.System.Objective
{
    public abstract class ObjectiveBase : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        protected ObjectiveManagerBase objectives;

        private bool _lastHasCompleted;

        private bool _lastIsPaused;

        private int _lastOwningTeamId = -1;

        private float _lastProgress;


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
                objectives.Internal_AddObjective(this, OwningTeamId);
                _lastOwningTeamId = OwningTeamId;
            }

            var progressChanged = !Mathf.Approximately(_lastProgress, Progress);
            if (progressChanged) _lastProgress = Progress;

            var hasCompletedChanged = _lastHasCompleted != HasCompleted;
            _lastHasCompleted = HasCompleted;

            var hasPausedChanged = _lastIsPaused != IsPaused;
            _lastIsPaused = IsPaused;


            if ((owningTeamIdChanged || hasCompletedChanged) && OwningTeamId != 0 && !HasCompleted)
            {
                OnObjectiveStart();
            }

            if (OwningTeamId != 0 && !HasCompleted && hasPausedChanged)
            {
                if (IsPaused) OnObjectivePause();
                else OnObjectiveResume();
            }

            if (progressChanged)
            {
                OnObjectiveProgress();
            }

            if (OwningTeamId != 0 && hasCompletedChanged && HasCompleted)
            {
                OnObjectiveCompleted();
            }
        }

        /// <summary>
        /// Called when ObjectiveBase should initialize.
        /// </summary>
        /// <param name="teamId"></param>
        public virtual void OnObjectiveSetup(int teamId)
        {
            Progress = 0;
            IsPaused = false;
            OwningTeamId = teamId;
            RequestSerialization();
        }

        /// <summary>
        /// Called when objective goal has activated and started.
        /// </summary>
        public virtual void OnObjectiveStart()
        {
        }

        /// <summary>
        /// Called when objective goal is paused and halt updates.
        /// </summary>
        public virtual void OnObjectivePause()
        {
        }

        /// <summary>
        /// Called when objective goal should resume and continue updating.
        /// </summary>
        public virtual void OnObjectiveResume()
        {
        }


        /// <summary>
        /// Called when objective goal has updated progress.
        /// </summary>
        public virtual void OnObjectiveProgress()
        {
        }

        /// <summary>
        /// Called when objective goal has completed.
        /// </summary>
        public virtual void OnObjectiveCompleted()
        {
        }

        #region Properties

        /// <summary>
        /// Objective's current team id.
        /// </summary>
        /// <remarks>
        /// Might change dynamically depending on the objective implementation.
        /// </remarks>
        [field: UdonSynced]
        [PublicAPI]
        public virtual int OwningTeamId { get; protected set; }

        [field: UdonSynced]
        [PublicAPI]
        public virtual float Progress { get; protected set; }

        [field: UdonSynced]
        [PublicAPI]
        public virtual bool IsPaused { get; protected set; }

        /// <summary>
        /// Has the objective goal been completed? 
        /// </summary>
        [PublicAPI]
        public virtual bool HasCompleted => Progress <= 1;

        [PublicAPI]
        public bool IsActiveAndRunning => OwningTeamId != 0 && !IsPaused && !HasCompleted;

        #endregion
    }
}