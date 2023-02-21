using System;
using CenturionCC.System.Utils;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace CenturionCC.System.Audio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CollisionAudioStore : UdonSharpBehaviour
    {
        [FormerlySerializedAs("fallbackAudio")]
        [SerializeField]
        private AudioDataStore fallback;
        [FormerlySerializedAs("clothAudio")]
        [SerializeField]
        private AudioDataStore gravel;
        [FormerlySerializedAs("woodAudio")]
        [SerializeField]
        private AudioDataStore wood;
        [FormerlySerializedAs("ironAudio")]
        [SerializeField]
        private AudioDataStore metallic;
        [SerializeField]
        private AudioDataStore dirt;
        [SerializeField]
        private AudioDataStore concrete;

        [Obsolete]
        public AudioDataStore FallbackAudio => fallback;
        [Obsolete]
        public AudioDataStore WoodAudio => wood;
        [Obsolete]
        public AudioDataStore IronAudio => metallic;
        [Obsolete]
        public AudioDataStore ClothAudio => gravel;

        [PublicAPI] [CanBeNull]
        public AudioDataStore Get(ObjectType type)
        {
            AudioDataStore result;
            switch (type)
            {
                default:
                case ObjectType.Prototype:
                    result = fallback;
                    break;
                case ObjectType.Gravel:
                    result = gravel;
                    break;
                case ObjectType.Wood:
                    result = wood;
                    break;
                case ObjectType.Metallic:
                    result = metallic;
                    break;
                case ObjectType.Dirt:
                    result = dirt;
                    break;
                case ObjectType.Concrete:
                    result = concrete;
                    break;
            }

            return result != null ? result : fallback;
        }
    }
}