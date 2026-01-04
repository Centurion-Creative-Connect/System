using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
namespace CenturionCC.System.Gimmick.Duel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DuelGameUI : UdonSharpBehaviour
    {
        [SerializeField]
        private DuelGame instance;
        [SerializeField]
        private GameObject[] entryPanel;
        [SerializeField]
        private GameObject[] makeReadyPanel;
        [SerializeField]
        private GameObject[] inGamePanel;

        [SerializeField]
        private Text gameStatus;

        [SerializeField]
        private Text[] aTeamPlayer;
        [SerializeField]
        private Text aTeamScore;
        [SerializeField]
        private Text aTeamReady;
        [SerializeField]
        private Animator aTeamAnimator;
        [SerializeField]
        private Text[] bTeamPlayer;
        [SerializeField]
        private Text bTeamScore;
        [SerializeField]
        private Text bTeamReady;
        [SerializeField]
        private Animator bTeamAnimator;
        private readonly int _animatorState = Animator.StringToHash("AnimatorState");

        private void Start()
        {
            UpdateUI();
        }

        public void OnResetButton()
        {
            instance.ResetGame();
            if (!Networking.IsOwner(instance.gameObject))
                Networking.SetOwner(Networking.LocalPlayer, instance.gameObject);
            instance.RequestSerialization();
        }

        public void OnATeamEntryButton()
        {
            ProcessEntryButton(instance.playerA, instance.playerB);
            UpdateUI();
        }

        public void OnBTeamEntryButton()
        {
            ProcessEntryButton(instance.playerB, instance.playerA);
            UpdateUI();
        }

        public void UpdateUI()
        {
            gameStatus.text = GetGameStatusText(instance);
            foreach (var aTeam in aTeamPlayer)
                aTeam.text = Utilities.IsValid(instance.playerA.vrcPlayerApi)
                    ? instance.playerA.vrcPlayerApi.displayName
                    : "None";

            foreach (var bTeam in bTeamPlayer)
                bTeam.text = Utilities.IsValid(instance.playerB.vrcPlayerApi)
                    ? instance.playerB.vrcPlayerApi.displayName
                    : "None";

            aTeamScore.text = $"{instance.teamAScore}";
            bTeamScore.text = $"{instance.teamBScore}";

            aTeamReady.text = instance.playerA.isReady ? "Ready" : "Not Rdy";
            bTeamReady.text = instance.playerB.isReady ? "Ready" : "Not Rdy";

            aTeamAnimator.SetInteger(_animatorState, instance.playerA.isReady ? 1 : 0);
            bTeamAnimator.SetInteger(_animatorState, instance.playerB.isReady ? 1 : 0);

            var isEntryTime = instance.State == DuelGameState.WaitingForPlayers;

            SetActiveAll(entryPanel, isEntryTime);
            SetActiveAll(makeReadyPanel,
                instance.State == DuelGameState.WaitingForReady || instance.State == DuelGameState.WaitingForStart);
            SetActiveAll(inGamePanel, !isEntryTime);
        }

        private static void SetActiveAll(GameObject[] arr, bool isActive)
        {
            foreach (var o in arr)
                if (o != null)
                    o.SetActive(isActive);
        }

        private static string GetGameStatusText(DuelGame instance)
        {
            switch (instance.State)
            {
                case DuelGameState.WaitingForPlayers:
                {
                    var playerCount = 0;
                    if (instance.playerA.vrcPlayerApi != null && instance.playerA.vrcPlayerApi.IsValid())
                        ++playerCount;
                    if (instance.playerB.vrcPlayerApi != null && instance.playerB.vrcPlayerApi.IsValid())
                        ++playerCount;

                    return $"Waiting For Players... ({playerCount} / 2)";
                }
                case DuelGameState.WaitingForReady:
                {
                    var readyCount = 0;
                    if (instance.playerA.isReady)
                        ++readyCount;
                    if (instance.playerB.isReady)
                        ++readyCount;

                    return $"Checking Ready... ({readyCount} / 2)";
                }
                case DuelGameState.WaitingForStart:
                {
                    return
                        $"Round {instance.RoundCount} Starting In {instance.MatchStartingTime.Subtract(Networking.GetNetworkDateTime()).TotalSeconds:F1} Seconds";
                }
                case DuelGameState.MatchInProgress:
                {
                    return
                        $"Round {instance.RoundCount} In Progress For {Networking.GetNetworkDateTime().Subtract(instance.MatchStartingTime).TotalSeconds:F1} Seconds";
                }
                default:
                {
                    return "Unknown State!!!";
                }
            }
        }

        private static void ProcessEntryButton(DuelGamePlayer player, DuelGamePlayer opposite)
        {
            if (Utilities.IsValid(player.vrcPlayerApi) && player.vrcPlayerApi.isLocal)
            {
                player.ResetPlayer();
                player.Sync();
                return;
            }

            if (Utilities.IsValid(opposite.vrcPlayerApi) && opposite.vrcPlayerApi.isLocal)
                return;

            player.AssignLocal();
            player.Sync();
        }
    }
}
