using CenturionCC.System.UI;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ErrorDiagnostic : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        private PrintableBase logger;

        [SerializeField] [NewbieInject]
        private NotificationProvider notification;

        [SerializeField] [NewbieInject]
        private RoleProvider roleProvider;

        /// <summary>
        /// Broadcasts an error log to all players.
        /// </summary>
        /// <param name="message"></param>
        public void BroadcastError(string message)
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_BroadcastError), Networking.LocalPlayer.playerId, message);
        }

        [NetworkCallable(maxEventsPerSecond: 100)]
        public void Internal_BroadcastError(int playerId, string message)
        {
            var playerName = "Unknown";
            var vrcPlayerApi = VRCPlayerApi.GetPlayerById(playerId);
            if (Utilities.IsValid(vrcPlayerApi)) playerName = vrcPlayerApi.displayName;

            var log = $"[<color=red>ErrorDiagnostic</color>] Error detected at player {playerName}({playerId}) {message}";

            if (logger)
            {
                logger.LogError(log);
            }
            else
            {
                Debug.LogError("[<color=red>ErrorDiagnostic</color>] No logger found!");
                Debug.LogError(log);
            }

            if (roleProvider.GetPlayerRoles().IsGameStaff())
            {
                var preferJapanese = VRCPlayerApi.GetCurrentLanguage() == "ja";
                notification.ShowError(
                    preferJapanese ?
                        $"STAFF ONLY: プレイヤー {playerName}({playerId}) がシステムエラーを通知しました。内容は以下です。" :
                        $"STAFF ONLY: Error detected at {playerName}({playerId}). The details are as follows:",
                    10f,
                    Random.Range(int.MinValue, int.MaxValue)
                );
                notification.ShowError(message, 10f, Random.Range(int.MinValue, int.MaxValue));
            }
        }

        /// <summary>
        /// Gets the ErrorDiagnostic instance.
        /// </summary>
        /// <remarks>
        /// ErrorDiagnostic instance's GameObject must be named as `__CenturionErrorDiagnostic__` to be found properly.
        /// </remarks>
        /// <returns>The ErrorDiagnostic instance or null if not found.</returns>
        [CanBeNull]
        public static ErrorDiagnostic GetInstance()
        {
            var go = GameObject.Find("__CenturionErrorDiagnostic__");
            return go ? go.GetComponent<ErrorDiagnostic>() : null;
        }

        /// <summary>
        /// Asserts condition is true. If not, sends an error log to all players.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="message"></param>
        /// <returns>true if assertion has failed. false otherwise.</returns>
        [PublicAPI]
        public static bool Assert(bool condition, string message)
        {
            if (condition)
            {
                return false;
            }
            else // very unlikely
            {
                var instance = GetInstance();
                if (instance != null)
                {
                    instance.BroadcastError($"Assertion failed: {message}");
                }
                else
                {
                    Debug.LogError("[<color=red>ErrorDiagnostic</color>] No ErrorDiagnostic instance found!");
                    Debug.LogError($"[<color=red>ErrorDiagnostic</color>] Assertion failed: {message}");
                }
                return true;
            }
        }
    }
}
