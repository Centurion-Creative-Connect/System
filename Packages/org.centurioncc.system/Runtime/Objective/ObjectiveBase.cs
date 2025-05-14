using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common;

namespace CenturionCC.System.Objective
{
    public abstract class ObjectiveBase : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        protected ObjectiveCollection objectives;

        private bool _lastHasCompleted = false;

        private bool _lastIsPaused = false;

        private int _lastOwningTeamId = -1;

        /// <summary>
        /// Objective's current team id.
        /// </summary>
        /// <remarks>
        /// Most likely to be set in OnObjectiveStart.
        /// Might change dynamically depending on objective.
        /// Should never change when HasCompleted is true.
        /// </remarks>
        [field: UdonSynced]
        public virtual int OwningTeamId { get; protected set; }

        /// <summary>
        /// Has the objective been completed? 
        /// </summary>
        /// <remarks>
        /// OwningTeamId should never change when this returns true.
        /// </remarks>
        [field: UdonSynced]
        public virtual bool HasCompleted { get; protected set; }

        [field: UdonSynced]
        public virtual bool IsPaused { get; protected set; }

        public bool IsActiveAndRunning => OwningTeamId != 0 && !IsPaused && !HasCompleted;

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
                objectives.RemoveObjective(this, _lastOwningTeamId);
                objectives.AddObjective(this, OwningTeamId);
                _lastOwningTeamId = OwningTeamId;
            }

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

            if (OwningTeamId != 0 && hasCompletedChanged && HasCompleted)
            {
                OnObjectiveEnd();
            }
        }

        /// <summary>
        /// Called when objective should initialize.
        /// </summary>
        /// <param name="teamId"></param>
        public virtual void OnObjectiveSetup(int teamId)
        {
            HasCompleted = false;
            IsPaused = false;
            OwningTeamId = teamId;
            RequestSerialization();
        }

        /// <summary>
        /// Called when objectyive should activate and start.
        /// </summary>
        public abstract void OnObjectiveStart();

        /// <summary>
        /// Called when objective should pause and halt updates.
        /// </summary>
        public virtual void OnObjectivePause()
        {
        }

        /// <summary>
        /// Called when objective should resume and continue updating.
        /// </summary>
        public virtual void OnObjectiveResume()
        {
        }

        /// <summary>
        /// Called when objective should end.
        /// </summary>
        public abstract void OnObjectiveEnd();
    }
}