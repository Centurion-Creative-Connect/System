using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player.HitDisplay
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ExternalHitDisplay : ExternalHitDisplayBase
    {
        [SerializeField]
        private float stoppingHeight = 0.7F;

        [SerializeField]
        private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 0.5F, 1F);

        [SerializeField]
        private float duration = 5F;

        private PlayerBase _followingPlayer;
        private VRCPlayerApi _localPlayer;
        private Vector3 _posOffset;

        private float _time;

        private Transform _transform;

        private void Start()
        {
            _transform = transform;
            _localPlayer = Networking.LocalPlayer;
        }

        private void Update()
        {
            if (!Utilities.IsValid(_followingPlayer) || !Utilities.IsValid(_localPlayer))
            {
                DestroyThis();
                return;
            }

            _time += Time.deltaTime;
            _transform.position = _followingPlayer.GetPosition() + _posOffset +
                                  new Vector3(0, stoppingHeight * curve.Evaluate(_time), 0);
            _transform.LookAt(_localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position);
        }

        public override void Play(PlayerBase player)
        {
            if (player == null)
            {
                DestroyThis();
                return;
            }

            _followingPlayer = player;
            var headPos = player.GetBonePosition(HumanBodyBones.Head);
            var playerPos = player.GetPosition();
            _posOffset = headPos - playerPos;
            _time = 0;
            gameObject.SetActive(true);

            SendCustomEventDelayedSeconds(nameof(DestroyThis), duration);
        }

        public void DestroyThis()
        {
            Destroy(gameObject);
        }
    }
}
