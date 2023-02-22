using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Audio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class CollisionAudioPlayer : UdonSharpBehaviour
    {
        [SerializeField]
        private CollisionAudioStore collisionAudioStore;
        [SerializeField] [HideInInspector] [NewbieInject]
        private AudioManager audioManager;

        private int _collisionCount;

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

            SendCustomNetworkEvent(NetworkEventTarget.All, GetMethodName(objMarker.ObjectType));
        }

        private void OnCollisionExit(Collision other)
        {
            --_collisionCount;
            if (_collisionCount < 0)
                _collisionCount = 0;
        }

        private string GetMethodName(ObjectType type)
        {
            const string methodBaseName = "Play{0}Audio";
            switch (type)
            {
                default:
                case ObjectType.Prototype:
                    return string.Format(methodBaseName, "Prototype");
                case ObjectType.Gravel:
                    return string.Format(methodBaseName, "Gravel");
                case ObjectType.Wood:
                    return string.Format(methodBaseName, "Wood");
                case ObjectType.Metallic:
                    return string.Format(methodBaseName, "Metallic");
                case ObjectType.Dirt:
                    return string.Format(methodBaseName, "Dirt");
                case ObjectType.Concrete:
                    return string.Format(methodBaseName, "Concrete");
            }
        }

        public void PlayPrototypeAudio()
        {
            ProcessAudio(ObjectType.Prototype);
        }

        public void PlayGravelAudio()
        {
            ProcessAudio(ObjectType.Gravel);
        }

        public void PlayWoodAudio()
        {
            ProcessAudio(ObjectType.Wood);
        }

        public void PlayMetallicAudio()
        {
            ProcessAudio(ObjectType.Metallic);
        }

        public void PlayDirtAudio()
        {
            ProcessAudio(ObjectType.Dirt);
        }

        public void PlayConcreteAudio()
        {
            ProcessAudio(ObjectType.Concrete);
        }

        private void ProcessAudio(ObjectType type)
        {
            audioManager.PlayAudioAtPosition(collisionAudioStore.Get(type), transform.position);
        }
    }
}