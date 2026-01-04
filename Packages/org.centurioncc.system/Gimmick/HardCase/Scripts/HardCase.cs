using DerpyNewbie.Common;
using DerpyNewbie.Common.Invoker;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
namespace CenturionCC.System.Gimmick.HardCase
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class HardCase : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        private UpdateManager updateManager;

        [SerializeField] [NewbieInject(SearchScope.Children)]
        private HardCaseLid lid;

        [SerializeField] [NewbieInject(SearchScope.Children)]
        private HardCaseText text;

        [SerializeField] [NewbieInject(SearchScope.Children)]
        private HardCaseKeyboard keyboard;

        [SerializeField] private float interactionTime = 3F;
        [SerializeField] private CommonInvokerBase interactionInvoker;

        private float _interactBeginTime;
        private float _interactEndTime;

        [UdonSynced] private int _interactingPlayerId;

        [UdonSynced] [FieldChangeCallback(nameof(IsInteracting))]
        private bool _isInteracting;

        private bool IsInteracting
        {
            get => _isInteracting;
            set
            {
                var hasDiff = _isInteracting != value;
                _isInteracting = value;

                if (!hasDiff || Networking.IsOwner(gameObject)) return;

                if (_isInteracting)
                {
                    _interactBeginTime = Time.timeSinceLevelLoad;
                    _interactEndTime = Time.timeSinceLevelLoad + interactionTime;
                    updateManager.SubscribeUpdate(this);
                }
                else
                {
                    if (_interactEndTime < Time.timeSinceLevelLoad)
                    {
                        UpdateProgressText();
                        text.PlantReady();
                    }
                    else
                    {
                        text.Abort(NewbieUtils.GetPlayerName(_interactingPlayerId));
                    }
                }
            }
        }

        private void Start()
        {
            lid.Subscribe(this);
            keyboard.Subscribe(this);
        }

        public void _Update()
        {
            if (!IsInteracting)
            {
                updateManager.UnsubscribeUpdate(this);
                return;
            }

            UpdateProgressText();

            if (Time.timeSinceLevelLoad < _interactEndTime) return;

            text.PlantReady();
            if (!Networking.IsOwner(gameObject)) return;

            interactionInvoker.Invoke();
            IsInteracting = false;
            Sync();
        }

        private void UpdateProgressText()
        {
            var username = NewbieUtils.GetPlayerName(_interactingPlayerId);
            var now = Time.timeSinceLevelLoad;
            var progress = now - _interactBeginTime;
            var progressNormalized = progress / (_interactEndTime - _interactBeginTime);
            text.PlantProgress(username, progressNormalized, Mathf.CeilToInt(_interactEndTime - now + 1));
        }

        [PublicAPI] // Callback from Lid
        public void OnOpenProgressUpdated()
        {
            var hasOpened = lid.OpenProgressNormalized > 0.5F;
            text.SetObjectsVisible(hasOpened);
            keyboard.SetInteractable(hasOpened);
        }

        [PublicAPI] // Callback from keyboard
        public void OnKeyboardUseDown()
        {
            _interactBeginTime = Time.timeSinceLevelLoad;
            _interactEndTime = Time.timeSinceLevelLoad + interactionTime;
            IsInteracting = true;
            _interactingPlayerId = Networking.LocalPlayer.playerId;
            updateManager.SubscribeUpdate(this);
            Sync();
        }

        [PublicAPI] // Callback from keyboard
        public void OnKeyboardUseUp()
        {
            if (IsInteracting) text.Abort(NewbieUtils.GetPlayerName(_interactingPlayerId));

            IsInteracting = false;
            Sync();
        }

        private void Sync()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
    }
}
