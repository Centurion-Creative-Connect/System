using System;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Audio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class AudioMarker : UdonSharpBehaviour
    {
        // TODO: refactor it to use enum instead of string const
        private const string OT_FALLBACK = "fallback";
        private const string OT_WOOD = "wood";
        private const string OT_CLOTH = "cloth";
        private const string OT_IRON = "iron";
        private const string OT_DISABLE = "none";
        
        // TODO: centralize these hint strings
        [NonSerialized]
        private const string HolderHintString = "Logics/System/Audio/AudioHolder/";
        [SerializeField]
        private AudioHolder audioHolder;
        [NonSerialized]
        private bool _isDisabled;

        private void Start()
        {
            if (audioHolder == null)
                SendCustomEventDelayedFrames(nameof(TryAssignScheduler), UnityEngine.Random.Range(100, 200));
        }

        public void TryAssignScheduler()
        {
            var hint = TryGetObjectType(gameObject.name);
            if (hint.Equals(OT_DISABLE))
            {
                _isDisabled = true;
                return;
            }

            Debug.Log($"TryGetObjType: {gameObject.name} => {hint}");
            var g = GameObject.Find($"{HolderHintString}AudioHolder-{hint}");
            if (g)
                audioHolder = g.GetComponent<AudioHolder>();
        }

        private string TryGetObjectType(string n)
        {
            if (n.Contains("cloth") || n.Contains("sandbag"))
                return OT_CLOTH;
            if (n.Contains("wood") || n.Contains("window") || n.Contains("doorframe"))
                return OT_WOOD;
            if (n.Contains("iron") || n.Contains("drum"))
                return OT_IRON;
            if (n.Contains("ground"))
                return OT_DISABLE;
            return OT_FALLBACK;
        }

        public void PlayAt(Vector3 position, float volume)
        {
            if (_isDisabled)
                return;
            if (audioHolder)
                audioHolder.PlayAtPosition(position, volume);
            else
                Debug.LogWarning($"[AudioMarker-{gameObject.name}] audio holder is null");
        }
    }
}