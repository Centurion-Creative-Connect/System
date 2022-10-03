using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CenturionCC.System.UI
{
    [DefaultExecutionOrder(100000)] [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GameManagerUI : UdonSharpBehaviour
    {
        private const string Prefix = "<color=orange>GameManagerUI</color>::";
        public GameManager manager;

        public Toggle visualizeGunHandle;
        public Toggle visualizeOnHit;
        public Toggle useModeratorMode;
        public Toggle showDebugNameTag;
        public Toggle showDebugTrail;
        public Toggle showCollider;
        public Toggle useBaseCollider;
        public Toggle useBodyCollider;

        public InputField jumpImpulseField;
        public InputField gravityStrengthField;
        public InputField walkSpeedField;
        public InputField runSpeedField;
        public InputField strafeSpeedField;
        public InputField remoteIdField;
        private readonly bool _lastBodyCollider = true;

        private bool _isInitialized;

        private bool _lastBaseCollider = true;
        private bool _lastModeratorMode;
        private bool _lastShowCollider = true;
        private bool _lastShowDebugNameTag = true;
        private bool _lastShowDebugTrail;
        private bool _lastUpperArmCollider = true;
        private bool _lastVisualizeGunHandle = true;

        private bool _lastVisualizeOnHit = true;

        public void OnEnable()
        {
            SendCustomEventDelayedFrames(nameof(RefreshUI), 1);
        }

        public void RefreshUI()
        {
            if (!_isInitialized)
                manager.logger.Log($"{Prefix}not initialized yet! initializing");

            visualizeGunHandle.SetIsOnWithoutNotify(manager.guns.IsDebugGunHandleVisible);
            visualizeOnHit.SetIsOnWithoutNotify(manager.eventLogger.ShouldVisualizeOnLog);
            useModeratorMode.SetIsOnWithoutNotify(manager.moderatorTool.IsModeratorMode);
            showDebugTrail.SetIsOnWithoutNotify(manager.guns.UseDebugBulletTrail);

            {
                var p = manager.players;
                showDebugNameTag.SetIsOnWithoutNotify(p.IsDebug);
                showCollider.SetIsOnWithoutNotify(p.IsDebug);
                useBaseCollider.SetIsOnWithoutNotify(p.UseBaseCollider);
                useBodyCollider.SetIsOnWithoutNotify(p.UseAdditionalCollider);
            }

            RefreshPlayerMovementSettings();

            _isInitialized = true;
        }

        public void UpdateGameManager()
        {
            var m = manager;
            m.logger.Log($"{Prefix}checking updated UI");
            if (!_isInitialized) RefreshUI();

            // Collider toggles
            {
                var p = manager.players;

                // Show Collider
                if (CheckToggle(showCollider, _lastShowCollider))
                {
                    m.logger.Log($"{Prefix}update collider status");
                    _lastShowCollider = showCollider.isOn;
                    p.IsDebug = _lastShowCollider;
                }

                // Base
                if (CheckToggle(useBaseCollider, _lastBaseCollider))
                {
                    m.logger.Log($"{Prefix}update base status");
                    _lastBaseCollider = useBaseCollider.isOn;
                    p.UseBaseCollider = _lastBaseCollider;
                }

                // Body
                if (CheckToggle(useBodyCollider, _lastBodyCollider))
                {
                    m.logger.Log($"{Prefix}update body status");
                    _lastUpperArmCollider = useBodyCollider.isOn;
                    p.UseAdditionalCollider = _lastUpperArmCollider;
                }
            }

            if (CheckToggle(visualizeOnHit, _lastVisualizeOnHit))
            {
                m.logger.Log($"{Prefix}update visualize on hit status");
                _lastVisualizeOnHit = visualizeOnHit.isOn;
                manager.eventLogger.ShouldVisualizeOnLog = _lastVisualizeOnHit;
            }

            if (CheckToggle(showDebugNameTag, _lastShowDebugNameTag))
            {
                m.logger.Log($"{Prefix}update debug nametag status");
                _lastShowDebugNameTag = showDebugNameTag.isOn;
                m.players.IsDebug = _lastShowDebugNameTag;
            }

            if (CheckToggle(showDebugTrail, _lastShowDebugTrail))
            {
                m.logger.Log($"{Prefix}update debug trail status");
                _lastShowDebugTrail = showDebugTrail.isOn;
                m.guns.UseDebugBulletTrail = _lastShowDebugTrail;
            }

            if (CheckToggle(visualizeGunHandle, _lastVisualizeGunHandle))
            {
                m.logger.Log($"{Prefix}update visualize gun handle status");
                _lastVisualizeGunHandle = visualizeGunHandle.isOn;
                m.guns.IsDebugGunHandleVisible = _lastVisualizeGunHandle;
            }

            // ModeratorMode
            if (CheckToggle(useModeratorMode, _lastModeratorMode))
            {
                m.logger.Log($"{Prefix}update moderator mode status");
                _lastModeratorMode = useModeratorMode.isOn;
                m.moderatorTool.IsModeratorMode = _lastModeratorMode;
            }
        }

        public void ApplyPlayerMovementSettings()
        {
            var pm = manager.movement;

            pm.jumpImpulse = GetFloatFromField(jumpImpulseField);
            pm.gravityStrength = GetFloatFromField(gravityStrengthField);
            pm.walkSpeed = GetFloatFromField(walkSpeedField);
            pm.runSpeed = GetFloatFromField(runSpeedField);
            pm.strafeSpeed = GetFloatFromField(strafeSpeedField);

            pm.ApplySetting();
        }

        public void RefreshPlayerMovementSettings()
        {
            var pm = manager.movement;

            jumpImpulseField.text = $"{pm.jumpImpulse}";
            gravityStrengthField.text = $"{pm.gravityStrength}";
            walkSpeedField.text = $"{pm.walkSpeed}";
            runSpeedField.text = $"{pm.runSpeed}";
            strafeSpeedField.text = $"{pm.strafeSpeed}";
        }

        public void PlayHitLocal()
        {
            var localPlayer = manager.players.GetLocalPlayer();
            if (localPlayer != null)
                manager.PlayHitLocal(localPlayer);
        }

        public void PlayHitRemote()
        {
            var remotePlayer = manager.players.GetPlayer(GetIntFromField(remoteIdField));
            if (remotePlayer != null)
                manager.PlayHitRemote(remotePlayer);
        }

        private bool CheckToggle(Toggle toggle, bool lastValue)
        {
            if (!toggle) return false;
            return toggle.isOn != lastValue;
        }

        private float GetFloatFromField(InputField field)
        {
            float value;
            var result = float.TryParse(field.text, out value);
            return result ? value : float.NaN;
        }

        private int GetIntFromField(InputField field)
        {
            int value;
            var result = int.TryParse(field.text, out value);
            return result ? value : -1;
        }
    }
}