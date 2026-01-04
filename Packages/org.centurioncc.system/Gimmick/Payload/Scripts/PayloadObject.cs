using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Gimmick.Payload
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PayloadObject : ObjectMarkerBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private UpdateManager updateManager;

        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerController controller;

        [SerializeField] [HideInInspector] [NewbieInject]
        private PayloadUI ui;

        [SerializeField]
        private AreaPlayerCounter.AreaPlayerCounter areaCounter;

        [Header("Speed Settings")]
        [SerializeField]
        public float speedLv1Weight = 70F;

        [SerializeField]
        public float speedLv2Weight = 60F;

        [SerializeField]
        public float speedLv3Weight = 50F;

        public PayloadMode currentMode;
        private PayloadStatusContext _currentContext;
        private PayloadStatus _currentStatus;
        private float _objectWeight;

        public override ObjectType ObjectType => ObjectType.Prototype;
        public override float ObjectWeight => _objectWeight;
        public override float WalkingSpeedMultiplier => 1F;
        public override string[] Tags => new string[0];

        private void Start()
        {
            areaCounter.SubscribeCallback(this);
        }

        public override void OnPickup()
        {
            updateManager.SubscribeUpdate(this);
            controller.AddHoldingObject(this);
            UpdateUI();
        }

        public override void OnPickupUseUp()
        {
            currentMode = CyclePayloadMode(currentMode);
            ui.UpdateMode(currentMode);
            OnAreaPlayerCountChanged();
        }

        public override void OnDrop()
        {
            controller.RemoveHoldingObject(this);
            updateManager.UnsubscribeUpdate(this);
        }

        public void OnAreaPlayerCountChanged()
        {
            int friendlyPlayerCount, enemyPlayerCount;

            areaCounter.GetPlayerCount(
                out var ignore,
                out var redPlayerCount,
                out var yellowPlayerCount
            );

            ui.UpdatePlayerCount(redPlayerCount, yellowPlayerCount);

            switch (currentMode)
            {
                default:
                case PayloadMode.Inactive:
                {
                    UpdateStatus(PayloadStatus.Inactive, PayloadStatusContext.None);
                    return;
                }
                case PayloadMode.Red:
                {
                    friendlyPlayerCount = redPlayerCount;
                    enemyPlayerCount = yellowPlayerCount;
                    break;
                }
                case PayloadMode.Yellow:
                {
                    friendlyPlayerCount = yellowPlayerCount;
                    enemyPlayerCount = redPlayerCount;
                    break;
                }
            }

            if (friendlyPlayerCount == 0)
            {
                UpdateStatus(PayloadStatus.Stopped, PayloadStatusContext.NoFriendlyNearby);
                return;
            }

            if (enemyPlayerCount > 0)
            {
                UpdateStatus(PayloadStatus.Stopped, PayloadStatusContext.StoppedByEnemy);
                return;
            }

            if (friendlyPlayerCount == 1)
            {
                UpdateStatus(PayloadStatus.Running, PayloadStatusContext.SpeedLv1);
                return;
            }

            if (friendlyPlayerCount > 1 && friendlyPlayerCount <= 4)
            {
                UpdateStatus(PayloadStatus.Running, PayloadStatusContext.SpeedLv2);
                return;
            }

            UpdateStatus(PayloadStatus.Running, PayloadStatusContext.SpeedLv3);
        }

        private void UpdateStatus(PayloadStatus status, PayloadStatusContext context)
        {
            switch (status)
            {
                default:
                case PayloadStatus.Inactive:
                    _objectWeight = 0;
                    break;
                case PayloadStatus.Stopped:
                    _objectWeight = controller.maximumCarryingWeightInKilogram;
                    break;
                case PayloadStatus.Running:
                    _objectWeight = GetObjectWeightByContext(context);
                    break;
            }

            _currentStatus = status;
            _currentContext = context;
            ui.UpdateStatus(status, context);
            controller.UpdateHoldingObjects();
        }

        private void UpdateUI()
        {
            ui.UpdateStatus(_currentStatus, _currentContext);
            ui.UpdateMode(currentMode);
            areaCounter.GetPlayerCount(out var ignore, out var red, out var yel);
            ui.UpdatePlayerCount(red, yel);
        }

        private float GetObjectWeightByContext(PayloadStatusContext context)
        {
            switch (context)
            {
                default:
                case PayloadStatusContext.None:
                    return 0F;
                case PayloadStatusContext.NoFriendlyNearby:
                case PayloadStatusContext.StoppedByEnemy:
                    return controller.maximumCarryingWeightInKilogram;
                case PayloadStatusContext.SpeedLv1:
                    return speedLv1Weight;
                case PayloadStatusContext.SpeedLv2:
                    return speedLv2Weight;
                case PayloadStatusContext.SpeedLv3:
                    return speedLv3Weight;
            }
        }

        private PayloadMode CyclePayloadMode(PayloadMode current)
        {
            switch (current)
            {
                case PayloadMode.Inactive:
                    return PayloadMode.Red;
                case PayloadMode.Red:
                    return PayloadMode.Yellow;
                default:
                case PayloadMode.Yellow:
                    return PayloadMode.Inactive;
            }
        }
    }

    public enum PayloadMode
    {
        Inactive,
        Red,
        Yellow
    }

    public enum PayloadStatus
    {
        Inactive,
        Running,
        Stopped
    }

    public enum PayloadStatusContext
    {
        None,
        NoFriendlyNearby,
        StoppedByEnemy,
        SpeedLv1,
        SpeedLv2,
        SpeedLv3
    }
}
