using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
namespace CenturionCC.System.Command.Util
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class StaffOnlyToggleObjectCommand : BoolCommandHandler
    {
        [SerializeField] private bool defaultState = true;

        [Header("Global")] [SerializeField] private GameObject[] globalObjectsToEnable;

        [SerializeField] private GameObject[] globalObjectsToDisable;

        [Header("Moderator")] [SerializeField] private GameObject[] moderatorOnlyObjectsToEnable;

        [SerializeField] private GameObject[] moderatorOnlyObjectsToDisable;

        [SerializeField] private GameObject[] moderatorOnlyObjectsToAlwaysDisable;

        [SerializeField] private GameObject[] moderatorOnlyObjectsToAlwaysEnable;

        [Header("Player")] [SerializeField] private GameObject[] playerOnlyObjectsToEnable;

        [SerializeField] private GameObject[] playerOnlyObjectsToDisable;

        [SerializeField] private GameObject[] playerOnlyObjectsToAlwaysDisable;

        [SerializeField] private GameObject[] playerOnlyObjectsToAlwaysEnable;

        [SerializeField] private Collider[] playerOnlyCollidersToDisable;

        private NewbieConsole _console;

        [UdonSynced, FieldChangeCallback(nameof(CurrentState))]
        private bool _currentState;

        private bool CurrentState
        {
            get => _currentState;
            set
            {
                _currentState = value;
                UpdateObjectsState(value);
            }
        }

        public override string Label => gameObject.name;
        public override string Description => "Game Moderator can only execute this command.";

        private void Start()
        {
            SendCustomEventDelayedFrames(nameof(_LateStart), 1);
        }

        public void _LateStart()
        {
            if (Networking.IsMaster)
            {
                CurrentState = defaultState;
                RequestSerialization();
            }

            UpdateObjectsState(CurrentState);
            RequestSerialization();
        }

        private void SetActiveAll(GameObject[] objs, bool isActive)
        {
            if (objs == null) return;
            foreach (var o in objs)
                if (o != null)
                    o.SetActive(isActive);
        }

        private void UpdateObjectsState(bool state)
        {
            SetActiveAll(globalObjectsToDisable, !state);
            SetActiveAll(globalObjectsToEnable, state);

            if (_console != null && _console.IsSuperUser)
            {
                SetActiveAll(moderatorOnlyObjectsToDisable, !state);
                SetActiveAll(moderatorOnlyObjectsToEnable, state);
                SetActiveAll(moderatorOnlyObjectsToAlwaysDisable, false);
                SetActiveAll(moderatorOnlyObjectsToAlwaysEnable, true);
            }
            else
            {
                SetActiveAll(playerOnlyObjectsToDisable, !state);
                SetActiveAll(playerOnlyObjectsToEnable, state);
                SetActiveAll(playerOnlyObjectsToAlwaysDisable, false);
                SetActiveAll(playerOnlyObjectsToAlwaysEnable, true);
                foreach (var c in playerOnlyCollidersToDisable)
                    if (c != null)
                        c.enabled = !state;
            }
        }

        public void ForceSetActiveTrue()
        {
            CurrentState = true;
        }

        public void ForceSetActiveFalse()
        {
            CurrentState = false;
        }

        public override bool OnBoolCommand(NewbieConsole console, string label, ref string[] vars, ref string[] envVars)
        {
            if (!console.IsSuperUser)
            {
                console.Println("<color=red>Requires game moderator permission to execute this command.</color>");
                return CurrentState;
            }

            var force = false;
            if (vars.ContainsString("-f"))
            {
                force = true;
                vars = vars.RemoveItem("-f");
            }

            if (vars.Length >= 1)
            {
                var result = ConsoleParser.TryParseBoolean(vars[0], CurrentState);
                CurrentState = result;
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                RequestSerialization();

                if (force)
                {
                    SendCustomNetworkEvent(NetworkEventTarget.All,
                        result ? nameof(ForceSetActiveTrue) : nameof(ForceSetActiveFalse));
                }
            }

            console.Println($"{label}: {CurrentState}");
            return CurrentState;
        }

        public override void OnRegistered(NewbieConsole console)
        {
            _console = console;
        }
    }
}
