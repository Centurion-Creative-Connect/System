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
    public class CenturionDiagnostic : UdonSharpBehaviour
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

        /// <summary>
        /// Broadcasts a warning log to all players.
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="message"></param>
        public void BroadcastWarning(string message)
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_BroadcastWarning), Networking.LocalPlayer.playerId, message);
        }

        [NetworkCallable(maxEventsPerSecond: 100)]
        public void Internal_BroadcastError(int playerId, string message)
        {
            var playerName = "Unknown";
            var vrcPlayerApi = VRCPlayerApi.GetPlayerById(playerId);
            if (Utilities.IsValid(vrcPlayerApi)) playerName = vrcPlayerApi.displayName;

            var log = $"[<color=red>CenturionDiagnostic</color>] Error received\n{playerName}({playerId}): {message}";

            if (logger)
            {
                logger.LogError(log);
            }
            else
            {
                Debug.LogError("[<color=red>CenturionDiagnostic</color>] No logger found!");
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

        [NetworkCallable(maxEventsPerSecond: 100)]
        public void Internal_BroadcastWarning(int playerId, string message)
        {
            var playerName = "Unknown";
            var vrcPlayerApi = VRCPlayerApi.GetPlayerById(playerId);
            if (Utilities.IsValid(vrcPlayerApi)) playerName = vrcPlayerApi.displayName;

            var log = $"[<color=yellow>CenturionDiagnostic</color>] Warning received\n{playerName}({playerId}): {message}";

            if (logger)
            {
                logger.LogWarn(log);
            }
            else
            {
                Debug.LogError("[<color=red>CenturionDiagnostic</color>] No logger found!");
                Debug.LogWarning(log);
            }
        }

        /// <summary>
        /// Gets the CenturionDiagnostic instance.
        /// </summary>
        /// <remarks>
        /// CenturionDiagnostic instance's GameObject must be named as `__CenturionDiagnostic__` to be found properly.
        /// </remarks>
        /// <returns>The CenturionDiagnostic instance or null if not found.</returns>
        [CanBeNull]
        public static CenturionDiagnostic GetInstance()
        {
            var go = GameObject.Find("__CenturionDiagnostic__");
            return go ? go.GetComponent<CenturionDiagnostic>() : null;
        }

        /// <summary>
        /// Asserts condition is true. If not, sends an error log to all players.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="message"></param>
        /// <returns>true if assertion has failed. false otherwise.</returns>
        [PublicAPI] [ContractAnnotation("condition:false => true")]
        public static bool Assert(bool condition, string message)
        {
            if (condition)
            {
                return false;
            }
            else // very unlikely
            {
                LogError($"Assertion failed: {message}");
                return true;
            }
        }

        /// <summary>
        /// Sends an error log to all players.
        /// </summary>
        /// <remarks>
        /// Staff receives notification when this is called.
        /// </remarks>
        /// <param name="message"></param>
        [PublicAPI]
        public static void LogError(string message)
        {
            var instance = GetInstance();
            if (instance != null)
            {
                instance.BroadcastError(message);
            }
            else
            {
                Debug.LogError("[<color=red>CenturionDiagnostic</color>] No CenturionDiagnostic instance found!");
                Debug.LogError($"[<color=red>CenturionDiagnostic</color>] ERR: {message}");
            }
        }

        /// <summary>
        /// Sends a warning log to all players.
        /// </summary>
        /// <param name="message"></param>
        [PublicAPI]
        public static void LogWarning(string message)
        {
            var instance = GetInstance();
            if (instance != null)
            {
                instance.BroadcastWarning(message);
            }
            else
            {
                Debug.LogError("[<color=red>CenturionDiagnostic</color>] No CenturionDiagnostic instance found!");
                Debug.LogWarning($"[<color=yellow>CenturionDiagnostic</color>] WARN: {message}");
            }
        }
    }
}
