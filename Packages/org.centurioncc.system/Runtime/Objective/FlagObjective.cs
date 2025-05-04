using UdonSharp;
using NotImplementedException = System.NotImplementedException;

namespace CenturionCC.System.Objective
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class FlagObjective : ObjectiveBase
    {
        public override void OnObjectiveStart()
        {
            throw new NotImplementedException();
        }

        public override void OnObjectivePause()
        {
            throw new NotImplementedException();
        }

        public override void OnObjectiveEnd()
        {
            throw new NotImplementedException();
        }
    }
}