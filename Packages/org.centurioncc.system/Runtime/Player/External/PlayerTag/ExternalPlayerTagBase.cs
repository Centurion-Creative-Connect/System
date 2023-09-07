using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player.External.PlayerTag
{
    public abstract class ExternalPlayerTagBase : UdonSharpBehaviour
    {
        [SerializeField] [Tooltip("Offset from head position, applied upwards.")]
        protected float tagOffset = 0.3F;

        protected VRCPlayerApi cachedLocalPlayer;
        protected Transform cachedTransform;
        protected bool didSetup;

        protected bool didStart;
        [NonSerialized]
        public VRCPlayerApi followingPlayer;

        protected ExternalPlayerTagManager tagManager;

        protected virtual Vector3 NameTagOffset => Vector3.up * tagOffset;


        protected virtual void Start()
        {
            cachedTransform = transform;
            cachedLocalPlayer = Networking.LocalPlayer;

            didStart = true;
        }

        protected virtual void OnDestroy()
        {
            tagManager.RemovePlayerTag(this);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (followingPlayer == player)
                DestroyThis();
        }

        public virtual void _Update()
        {
            if (!Utilities.IsValid(followingPlayer))
            {
                DestroyThis();
                return;
            }

            var localHead = cachedLocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            var remoteHeadPos = followingPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            if (followingPlayer.isLocal)
                remoteHeadPos += localHead.rotation * Vector3.forward * .5F;
            var forwardVec = (localHead.position - remoteHeadPos).normalized * .1F;
            cachedTransform.position = remoteHeadPos + NameTagOffset + forwardVec;
            cachedTransform.LookAt(localHead.position);
        }

        public virtual void Setup(ExternalPlayerTagManager manager, VRCPlayerApi api)
        {
            gameObject.SetActive(true);
            cachedTransform = transform;
            tagManager = manager;
            followingPlayer = api;
            cachedLocalPlayer = Networking.LocalPlayer;
            didSetup = true;
        }

        public void DestroyThis()
        {
            Destroy(gameObject);
        }

        public abstract void SetTagOn(TagType type, bool isOn);

        public abstract void SetTeamTag(int teamId, Color teamColor, bool isStaff);
    }
}