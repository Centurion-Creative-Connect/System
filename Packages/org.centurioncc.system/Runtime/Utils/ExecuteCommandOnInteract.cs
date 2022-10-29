using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ExecuteCommandOnInteract : UdonSharpBehaviour
    {
        [SerializeField]
        private NewbieConsole console;

        [SerializeField]
        private string command = "ping";
        [Header("Optional")]
        [SerializeField] [Tooltip("Replaces <toggle:$index> to [true|false] if exists")]
        private Toggle[] toggle;
        [SerializeField]
        private UdonSharpBehaviour relayCallback;
        [SerializeField]
        private string relayCallbackMethod;

        private void Start()
        {
            if (console == null)
                console = CenturionSystemReference.GetConsole();
        }

        public override void Interact()
        {
            Execute();
        }

        [PublicAPI]
        public void Execute()
        {
            var executingCommand = command;
            if (toggle != null)
                for (var i = 0; i < toggle.Length; i++)
                    executingCommand =
                        executingCommand
                            .Replace($"<toggle:{i}>", $"{toggle[i].isOn}")
                            .Replace($"<!toggle:{i}>", $"{!toggle[i].isOn}");

            console.Evaluate(executingCommand);

            if (relayCallback)
                relayCallback.SendCustomEvent(relayCallbackMethod);
        }
    }
}