using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Objective
{
    public abstract class ObjectiveManagerBase : UdonSharpBehaviour
    {
        private const string LogPrefix = "[ObjectiveCollection] ";

        [SerializeField] [NewbieInject]
        protected PrintableBase logger;

        /// <summary>
        /// Called by ObjectiveBase when OwningTeamId is changed.
        /// </summary>
        /// <param name="objective"></param>
        /// <param name="teamId"></param>
        public abstract void Internal_AddObjective(ObjectiveBase objective, int teamId);


        /// <summary>
        /// Called by ObjectiveBase when OwningTeamId is changed.
        /// </summary>
        /// <param name="objective"></param>
        /// <param name="teamId"></param>
        public abstract void Internal_RemoveObjective(ObjectiveBase objective, int teamId);

        /// <summary>
        /// Retrieves current active objectives for a specified team.
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns>Array of ObjectiveBase assigned for a team. Empty array if nothing was found.</returns>
        [PublicAPI]
        public abstract ObjectiveBase[] GetObjectives(int teamId);

        /// <summary>
        /// Removes all objectives.
        /// </summary>
        [PublicAPI]
        public abstract void ResetObjectives();

        #region EventCallbacks

        protected int EventCallbacksCount;
        protected UdonSharpBehaviour[] EventCallbacks;

        [PublicAPI]
        public virtual void Subscribe(UdonSharpBehaviour callback)
        {
            CallbackUtil.AddBehaviour(callback, ref EventCallbacksCount, ref EventCallbacks);
        }

        [PublicAPI]
        public virtual void Unsubscribe(UdonSharpBehaviour callback)
        {
            CallbackUtil.RemoveBehaviour(callback, ref EventCallbacksCount, ref EventCallbacks);
        }

        public virtual void Invoke_OnObjectiveAdded(ObjectiveBase objective, int teamId)
        {
            logger.Log($"{LogPrefix}OnObjectiveAdded: {objective.name}, {teamId}");
            foreach (var callback in EventCallbacks)
            {
                var ocCallback = (ObjectiveManagerCallbackBase)callback;
                if (ocCallback) ocCallback.OnObjectiveAdded(objective, teamId);
            }
        }

        public virtual void Invoke_OnObjectiveStarted()
        {
            logger.Log($"{LogPrefix}OnObjectiveStarted");
            foreach (var callback in EventCallbacks)
            {
                var ocCallback = (ObjectiveManagerCallbackBase)callback;
                if (ocCallback) ocCallback.OnObjectiveStarted();
            }
        }

        public virtual void Invoke_OnObjectivePaused()
        {
            logger.Log($"{LogPrefix}OnObjectivePaused");
            foreach (var callback in EventCallbacks)
            {
                var ocCallback = (ObjectiveManagerCallbackBase)callback;
                if (ocCallback) ocCallback.OnObjectivePaused();
            }
        }

        public virtual void Invoke_OnObjectiveResumed()
        {
            logger.Log($"{LogPrefix}OnObjectiveResumed");
            foreach (var callback in EventCallbacks)
            {
                var ocCallback = (ObjectiveManagerCallbackBase)callback;
                if (ocCallback) ocCallback.OnObjectiveResumed();
            }
        }

        public virtual void Invoke_OnObjectiveProgress(ObjectiveBase objective, int teamId)
        {
            logger.Log($"{LogPrefix}OnObjectiveProgress: {objective.name}, {teamId}");
            foreach (var callback in EventCallbacks)
            {
                var ocCallback = (ObjectiveManagerCallbackBase)callback;
                if (ocCallback) ocCallback.OnObjectiveProgress(objective, teamId);
            }
        }

        public virtual void Invoke_OnObjectiveCompleted(int teamId)
        {
            logger.Log($"{LogPrefix}OnObjectiveCompleted: {teamId}");
            foreach (var callback in EventCallbacks)
            {
                var ocCallback = (ObjectiveManagerCallbackBase)callback;
                if (ocCallback) ocCallback.OnObjectiveCompleted(teamId);
            }
        }

        public virtual void Invoke_OnObjectiveRemoved(ObjectiveBase objective, int teamId)
        {
            logger.Log($"{LogPrefix}OnObjectiveRemoved: {objective.name}, {teamId}");
            foreach (var callback in EventCallbacks)
            {
                var ocCallback = (ObjectiveManagerCallbackBase)callback;
                if (ocCallback) ocCallback.OnObjectiveRemoved(objective, teamId);
            }
        }

        #endregion
    }
}