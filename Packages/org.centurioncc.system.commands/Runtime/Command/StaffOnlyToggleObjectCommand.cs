using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class StaffOnlyToggleObjectCommand : BoolCommandHandler
    {
        [SerializeField]
        private bool defaultState = true;
        [Header("Global")]
        [SerializeField]
        private GameObject[] globalObjectsToEnable;
        [SerializeField]
        private GameObject[] globalObjectsToDisable;
        [Header("Moderator")]
        [SerializeField]
        private GameObject[] moderatorOnlyObjectsToEnable;
        [SerializeField]
        private GameObject[] moderatorOnlyObjectsToDisable;
        [SerializeField]
        private GameObject[] moderatorOnlyObjectsToAlwaysDisable;
        [SerializeField]
        private GameObject[] moderatorOnlyObjectsToAlwaysEnable;
        [Header("Player")]
        [SerializeField]
        private GameObject[] playerOnlyObjectsToEnable;
        [SerializeField]
        private GameObject[] playerOnlyObjectsToDisable;
        [SerializeField]
        private GameObject[] playerOnlyObjectsToAlwaysDisable;
        [SerializeField]
        private GameObject[] playerOnlyObjectsToAlwaysEnable;
        [SerializeField]
        private Collider[] playerOnlyCollidersToDisable;

        private NewbieConsole _console;
        [UdonSynced, FieldChangeCallback(nameof(CurrentState))]
        private bool _currentState;

        private bool CurrentState
        {
            get => _currentState;
            set
            {
                _currentState = value;
                UpdateObjectsState();
            }
        }

        public override string Label => gameObject.name;
        public override string Description => "Game Moderator can only execute this command.";

        private void Start()
        {
            if (Networking.IsMaster)
            {
                CurrentState = defaultState;
                RequestSerialization();
            }

            UpdateObjectsState();
        }

        private void SetActiveAll(GameObject[] objs, bool isActive)
        {
            if (objs == null) return;
            foreach (var o in objs)
                if (o != null)
                    o.SetActive(isActive);
        }

        private void UpdateObjectsState()
        {
            SetActiveAll(globalObjectsToDisable, !CurrentState);
            SetActiveAll(globalObjectsToEnable, CurrentState);

            if (_console != null && _console.IsSuperUser)
            {
                SetActiveAll(moderatorOnlyObjectsToDisable, !CurrentState);
                SetActiveAll(moderatorOnlyObjectsToEnable, CurrentState);
                SetActiveAll(moderatorOnlyObjectsToAlwaysDisable, false);
                SetActiveAll(moderatorOnlyObjectsToAlwaysEnable, true);
            }
            else
            {
                SetActiveAll(playerOnlyObjectsToDisable, !CurrentState);
                SetActiveAll(playerOnlyObjectsToEnable, CurrentState);
                SetActiveAll(playerOnlyObjectsToAlwaysDisable, false);
                SetActiveAll(playerOnlyObjectsToAlwaysEnable, true);
                foreach (var c in playerOnlyCollidersToDisable)
                    if (c != null)
                        c.enabled = !CurrentState;
            }
        }

        public override bool OnBoolCommand(NewbieConsole console, string label, ref string[] vars, ref string[] envVars)
        {
            if (!console.IsSuperUser)
            {
                console.Println("<color=red>Requires game moderator permission to execute this command.</color>");
                return CurrentState;
            }

            if (vars.Length >= 1)
            {
                var result = ConsoleParser.TryParseBoolean(vars[0], CurrentState);
                CurrentState = result;
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                RequestSerialization();
            }

            console.Println($"{label}: {CurrentState}");
            return CurrentState;
        }

        public override void OnRegistered(NewbieConsole console)
        {
            _console = console;
            UpdateObjectsState();
        }
    }
}