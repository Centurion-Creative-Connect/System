using System;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.Udon.Common;

namespace CenturionCC.System.Objective
{
    public abstract class ObjectiveBase : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        protected ObjectiveCollection objectives;

        private bool _lastHasCompleted = false;

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
            if (hasCompletedChanged)
            {
                _lastHasCompleted = HasCompleted;
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
        public abstract void OnObjectivePause();

        /// <summary>
        /// Called when objective should end.
        /// </summary>
        public abstract void OnObjectiveEnd();
    }
}