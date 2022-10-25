using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun.DataStore
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunRecoilPatternDataStore : UdonSharpBehaviour
    {
        [SerializeField]
        private int patternCount = 6;
        [SerializeField]
        private Vector3[] recoilOffsetPatterns =
        {
            new Vector3(-.05F, .05F, 0F),
            new Vector3(-.02F, .02F, 0F),
            new Vector3(.05F, -.05F, 0F),
            new Vector3(-.06F, .04F, 0F),
            new Vector3(-.07F, .045F, 0F),
            new Vector3(-.08F, .05F, 0F)
        };

        [SerializeField]
        private Vector3[] positionOffsetPatterns =
        {
            Vector3.zero,
            Vector3.zero,
            Vector3.zero,
            Vector3.zero,
            Vector3.zero,
            Vector3.zero
        };

        [SerializeField]
        private float[] speedOffsetPatterns =
        {
            0.0F,
            0.2F,
            0.25F,
            -0.1F,
            0.5F,
            0.3F
        };

        public virtual Vector3 GetRecoilOffset(int count)
        {
            return recoilOffsetPatterns[GetIndexFromCount(count)];
        }

        public virtual Vector3 GetPositionOffset(int count)
        {
            return positionOffsetPatterns[GetIndexFromCount(count)];
        }

        public virtual float GetSpeedOffset(int count)
        {
            return speedOffsetPatterns[GetIndexFromCount(count)];
        }

        public virtual void Get(int count, out float speedOffset, out Vector3 recoilOffset, out Vector3 positionOffset)
        {
            var i = GetIndexFromCount(count);
            speedOffset = speedOffsetPatterns[i];
            recoilOffset = recoilOffsetPatterns[i];
            positionOffset = positionOffsetPatterns[i];
        }

        private int GetIndexFromCount(int count)
        {
            return Mathf.RoundToInt(Mathf.Repeat(count, patternCount));
        }
    }
}