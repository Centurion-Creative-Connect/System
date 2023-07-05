using CenturionCC.System.Utils;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace CenturionCC.System.Audio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FootstepAudioStore : UdonSharpBehaviour
    {
        [FormerlySerializedAs("fallbackAudio")]
        [SerializeField]
        private AudioDataStore fallback;
        [FormerlySerializedAs("slowFallbackAudio")]
        [SerializeField]
        private AudioDataStore slowFallback;

        [FormerlySerializedAs("groundAudio")]
        [SerializeField]
        private AudioDataStore gravel;
        [FormerlySerializedAs("slowGroundAudio")]
        [SerializeField]
        private AudioDataStore slowGravel;

        [FormerlySerializedAs("woodAudio")]
        [SerializeField]
        private AudioDataStore wood;
        [FormerlySerializedAs("slowWoodAudio")]
        [SerializeField]
        private AudioDataStore slowWood;

        [FormerlySerializedAs("ironAudio")]
        [SerializeField]
        private AudioDataStore metallic;
        [FormerlySerializedAs("slowIronAudio")]
        [SerializeField]
        private AudioDataStore slowMetallic;

        [SerializeField]
        private AudioDataStore dirt;
        [SerializeField]
        private AudioDataStore slowDirt;

        [SerializeField]
        private AudioDataStore concrete;
        [SerializeField]
        private AudioDataStore slowConcrete;

        [PublicAPI] [CanBeNull]
        public AudioDataStore Get(ObjectType type, bool isSlow = false)
        {
            AudioDataStore result;
            switch (type)
            {
                default:
                case ObjectType.Prototype:
                    result = isSlow ? slowFallback : fallback;
                    break;
                case ObjectType.Gravel:
                    result = isSlow ? slowGravel : gravel;
                    break;
                case ObjectType.Wood:
                    result = isSlow ? slowWood : wood;
                    break;
                case ObjectType.Metallic:
                    result = isSlow ? slowMetallic : metallic;
                    break;
                case ObjectType.Dirt:
                    result = isSlow ? slowDirt : dirt;
                    break;
                case ObjectType.Concrete:
                    result = isSlow ? slowConcrete : concrete;
                    break;
            }

            return result != null ? result : isSlow && slowFallback != null ? slowFallback : fallback;
        }
    }
}