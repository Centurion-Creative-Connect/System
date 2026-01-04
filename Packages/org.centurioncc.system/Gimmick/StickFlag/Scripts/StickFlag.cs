using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
namespace CenturionCC.System.Gimmick.StickFlag
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class StickFlag : UdonSharpBehaviour
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private UpdateManager updateManager;
        [SerializeField]
        private StickFlagStick stick;
        [SerializeField]
        private bool snapToPositionOnDrop = true;
        [SerializeField] [Range(0, 1F)]
        private float dotThreshold = .85F;
        [SerializeField] [Range(.01F, 10F)]
        private float distanceThreshold = 0.1F;
        [SerializeField]
        private Transform neutralReference;
        [SerializeField]
        private Transform redReference;
        [SerializeField]
        private Transform yellowReference;
        [SerializeField]
        private GameObject[] neutralObjects;
        [SerializeField]
        private GameObject[] redObjects;
        [SerializeField]
        private GameObject[] yellowObjects;

        [UdonSynced] [FieldChangeCallback(nameof(FlagState))]
        private StickFlagState _flagState;

        public StickFlagState FlagState
        {
            get => _flagState;
            set
            {
                _flagState = value;
                SetObjectsActive(StickFlagState.Neutral, false);
                SetObjectsActive(StickFlagState.Red, false);
                SetObjectsActive(StickFlagState.Yellow, false);
                SetObjectsActive(value, true);
            }
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP

        private void OnDrawGizmosSelected()
        {
            var stickRefPos = stick.reference.position;
            var stickRefRot = stick.reference.rotation;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                stickRefPos,
                stickRefPos + stickRefRot * (Vector3.right * 0.01F)
            );

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                stickRefPos,
                stickRefPos + stickRefRot * (Vector3.left * 0.01F)
            );

            var redPos = redReference.position;
            var redRot = redReference.rotation * Quaternion.Euler(0, 0, 90);
            var yelPos = yellowReference.position;
            var yelRot = yellowReference.rotation * Quaternion.Euler(0, 0, -90);

            if (Vector3.Distance(redPos, yelPos) < Mathf.Epsilon &&
                Mathf.Approximately(Vector3.Dot(redRot * Vector3.right, yelRot * Vector3.right), 1))
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(redPos, distanceThreshold);
                GizmosUtil.DrawWireAngleCone(
                    redPos,
                    redRot,
                    distanceThreshold,
                    (1 - dotThreshold) * 90
                );
            }
            else
            {
                Gizmos.color = Color.red;

                Gizmos.DrawWireSphere(redPos, distanceThreshold);
                GizmosUtil.DrawWireAngleCone(
                    redPos,
                    redRot,
                    distanceThreshold,
                    (1 - dotThreshold) * 90
                );

                Gizmos.color = Color.yellow;

                Gizmos.DrawWireSphere(yelPos, distanceThreshold);
                GizmosUtil.DrawWireAngleCone(
                    yelPos,
                    yelRot,
                    distanceThreshold,
                    (1 - dotThreshold) * 90
                );
            }
        }

#endif

        public void _Update()
        {
            var stickPos = stick.reference.position;
            var refRight = stick.reference.right;
            var redRight = redReference.right;
            var yelRight = yellowReference.right;

            if (Vector3.Dot(refRight, redRight) > dotThreshold &&
                Vector3.Distance(stickPos, redReference.position) < distanceThreshold)
            {
                if (FlagState == StickFlagState.Red) return;

                FlagState = StickFlagState.Red;
                Sync();
                return;
            }

            if (Vector3.Dot(refRight, yelRight) > dotThreshold &&
                Vector3.Distance(stickPos, yellowReference.position) < distanceThreshold)
            {
                if (FlagState == StickFlagState.Yellow) return;

                FlagState = StickFlagState.Yellow;
                Sync();
                return;
            }

            if (FlagState != StickFlagState.Neutral)
            {
                FlagState = StickFlagState.Neutral;
                Sync();
                return;
            }
        }

        public override void OnPickup()
        {
            updateManager.SubscribeUpdate(this);
        }

        public override void OnDrop()
        {
            updateManager.UnsubscribeUpdate(this);
            if (snapToPositionOnDrop) SnapStickPosition();
        }

        [PublicAPI]
        public void ResetFlagStateAll()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ResetFlagState));
        }

        [PublicAPI]
        public void ResetFlagState()
        {
            Debug.Log($"[{name}] ResetFlagState");
            FlagState = StickFlagState.Neutral;
            SnapStickPosition();
        }

        [PublicAPI]
        public void Sync()
        {
            Debug.Log($"[{name}] Sync");

            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        private void SnapStickPosition()
        {
            Debug.Log($"[{name}] SnapStickPosition");

            switch (FlagState)
            {
                default:
                case StickFlagState.Neutral:
                    stick.SetPositionAndRotation(neutralReference.position, neutralReference.rotation);
                    break;
                case StickFlagState.Red:
                    stick.SetPositionAndRotation(redReference.position, redReference.rotation);
                    break;
                case StickFlagState.Yellow:
                    stick.SetPositionAndRotation(yellowReference.position, yellowReference.rotation);
                    break;
            }

            stick.Sync();
        }

        private void SetObjectsActive(StickFlagState state, bool isActive)
        {
            switch (state)
            {
                default:
                case StickFlagState.Neutral:
                    SetObjectsActive(neutralObjects, isActive);
                    break;
                case StickFlagState.Red:
                    SetObjectsActive(redObjects, isActive);
                    break;
                case StickFlagState.Yellow:
                    SetObjectsActive(yellowObjects, isActive);
                    break;
            }
        }

        private static void SetObjectsActive(GameObject[] objs, bool isActive)
        {
            foreach (var o in objs)
                if (o != null)
                    o.SetActive(isActive);
        }
    }

    public enum StickFlagState
    {
        Neutral,
        Red,
        Yellow,
    }
}
