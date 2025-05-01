using JetBrains.Annotations;
using UdonSharp;
using VRC.SDK3.Data;

namespace CenturionCC.System.Objective
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ObjectiveManager : UdonSharpBehaviour
    {
        private readonly DataDictionary _teamObjectivesDict = new DataDictionary();

        /// <summary>
        /// Adds objective for specified team.
        /// </summary>
        /// <param name="objective"></param>
        /// <param name="teamId"></param>
        [PublicAPI]
        public void AddObjective(ObjectiveBase objective, int teamId)
        {
            GetTeamObjectives(teamId).Add(objective);
        }

        /// <summary>
        /// Start all objectives.
        /// </summary>
        [PublicAPI]
        public void StartObjectives()
        {
            var keys = _teamObjectivesDict.GetKeys().ToArray();
            foreach (var key in keys)
            {
                var objectives = _teamObjectivesDict[key].DataList.ToArray();
                foreach (var objective in objectives)
                {
                    ((ObjectiveBase)objective.Reference).OnObjectiveStart();
                }
            }
        }

        /// <summary>
        /// Resets objective then clears current objectives list.
        /// </summary>
        [PublicAPI]
        public void EndObjectives()
        {
            var keys = _teamObjectivesDict.GetKeys().ToArray();
            foreach (var key in keys)
            {
                var objectives = _teamObjectivesDict[key].DataList.ToArray();
                foreach (var objective in objectives)
                {
                    ((ObjectiveBase)objective.Reference).OnObjectiveEnd();
                }
            }

            _teamObjectivesDict.Clear();
        }

        /// <summary>
        /// Retrieves current active objectives for specified team.
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        [PublicAPI]
        public DataList GetTeamObjectives(int teamId)
        {
            if (_teamObjectivesDict.TryGetValue(teamId, TokenType.DataList, out var teamObjectives))
                return teamObjectives.DataList;
            teamObjectives = new DataToken(new DataList());
            _teamObjectivesDict.SetValue(teamId, teamObjectives);
            return teamObjectives.DataList;
        }
    }
}