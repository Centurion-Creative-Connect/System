using UdonSharp;

namespace CenturionCC.System.Objective
{
    public abstract class ObjectiveManagerCallbackBase : UdonSharpBehaviour
    {
        public virtual void OnObjectiveAdded(ObjectiveBase objective, int teamId)
        {
        }

        public virtual void OnObjectiveStarted()
        {
        }

        public virtual void OnObjectivePaused()
        {
        }

        public virtual void OnObjectiveResumed()
        {
        }

        public virtual void OnObjectiveProgress(ObjectiveBase objective, int teamId)
        {
        }

        public virtual void OnObjectiveCompleted(int teamId)
        {
        }

        public virtual void OnObjectiveRemoved(ObjectiveBase objective, int teamId)
        {
        }
    }
}