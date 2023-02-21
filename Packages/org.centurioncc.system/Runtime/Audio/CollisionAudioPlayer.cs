using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace CenturionCC.System.Audio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CollisionAudioPlayer : UdonSharpBehaviour
    {
        [SerializeField]
        private CollisionAudioStore collisionAudioStore;
        [SerializeField] [HideInInspector] [NewbieInject]
        private AudioManager audioManager;

        private int _collisionCount;
        [UdonSynced]
        private Vector3 _pos;

        [UdonSynced]
        private ObjectType _type;

        private void OnCollisionEnter(Collision collision)
        {
            if (!Networking.IsOwner(gameObject))
                return;

            ++_collisionCount;

            if (_collisionCount < 1)
                return;

            var objMarker = collision.gameObject.GetComponent<ObjectMarkerBase>();
            if (objMarker == null || objMarker.Tags.ContainsString("NoCollisionAudio"))
                return;

            _pos = collision.GetContact(0).point;
            _type = objMarker.ObjectType;

            RequestSerialization();
        }

        private void OnCollisionExit(Collision other)
        {
            --_collisionCount;
            if (_collisionCount < 0)
                _collisionCount = 0;
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (result.success)
                ProcessAudio();
        }

        public override void OnDeserialization()
        {
            ProcessAudio();
        }

        private void ProcessAudio()
        {
            audioManager.PlayAudioAtPosition(collisionAudioStore.Get(_type), _pos);
        }
    }
}