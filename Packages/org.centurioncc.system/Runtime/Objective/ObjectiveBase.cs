using System;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Objective
{
    public abstract class ObjectiveBase : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        protected ObjectiveManager objectiveManager;

        /// <summary>
        /// Objective's current team id.
        /// </summary>
        /// <remarks>
        /// Most likely to be set in OnObjectiveStart.
        /// Might change dynamically depending on objective.
        /// Should never change when HasCompleted is true.
        /// </remarks>
        public abstract int OwningTeamId { get; }

        /// <summary>
        /// Has the objective been completed? 
        /// </summary>
        /// <remarks>
        /// OwningTeamId should never change when this returns true.
        /// </remarks>
        public abstract bool HasCompleted { get; }

        /// <summary>
        /// Called when objective should initialize.
        /// </summary>
        /// <param name="teamId"></param>
        public abstract void OnObjectiveSetup(int teamId);

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