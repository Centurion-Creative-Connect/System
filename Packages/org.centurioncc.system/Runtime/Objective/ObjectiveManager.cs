using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using NotImplementedException = System.NotImplementedException;

namespace CenturionCC.System.Objective
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ObjectiveManager : ObjectiveManagerBase
    {
        private readonly DataDictionary _teamObjectivesDict = new DataDictionary();

        public override void Internal_AddObjective(ObjectiveBase objective, int teamId)
        {
            GetTeamObjectivesDataList(teamId).Add(objective);
            Invoke_OnObjectiveAdded(objective, teamId);
        }

        public override void Internal_RemoveObjective(ObjectiveBase objective, int teamId)
        {
            GetTeamObjectivesDataList(teamId).RemoveAll(objective);
            Invoke_OnObjectiveRemoved(objective, teamId);
        }

        public override void Internal_OnObjectiveProgress(ObjectiveBase objective)
        {
            Invoke_OnObjectiveProgress(objective, objective.OwningTeamId);
        }

        public override void Internal_OnObjectiveCompleted(ObjectiveBase objective)
        {
            if (Mathf.Approximately(GetObjectiveProgress(objective.OwningTeamId), 1))
            {
                Invoke_OnObjectiveCompleted(objective.OwningTeamId);
            }
        }

        public override void StartObjectives()
        {
            Invoke_OnObjectiveStarted();
        }

        public override void PauseObjectives()
        {
            Invoke_OnObjectivePaused();
        }

        public override void ResumeObjectives()
        {
            Invoke_OnObjectiveResumed();
        }

        public override void ResetObjectives()
        {
            var teamIdTokens = _teamObjectivesDict.GetKeys().ToArray();
            foreach (var teamIdToken in teamIdTokens)
            {
                var objectivesToken = _teamObjectivesDict[teamIdToken].DataList.ToArray();
                foreach (var objectiveToken in objectivesToken)
                {
                    Internal_RemoveObjective((ObjectiveBase)objectiveToken.Reference, teamIdToken.Int);
                }
            }
        }

        public override ObjectiveBase[] GetObjectives(int teamId)
        {
            var teamObjectives = GetTeamObjectivesDataList(teamId);

            var result = new ObjectiveBase[teamObjectives.Count];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = (ObjectiveBase)teamObjectives[i].Reference;
            }

            return result;
        }

        private DataList GetTeamObjectivesDataList(int teamId)
        {
            if (_teamObjectivesDict.TryGetValue(teamId, TokenType.DataList, out var teamObjectives))
                return teamObjectives.DataList;
            teamObjectives = new DataToken(new DataList());
            _teamObjectivesDict.SetValue(teamId, teamObjectives);
            return teamObjectives.DataList;
        }
    }
}