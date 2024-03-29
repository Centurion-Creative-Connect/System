using System.Linq;
using System.Runtime.CompilerServices;
using UdonSharp;
using UnityEngine;

[assembly: InternalsVisibleTo("CenturionCC.System.Editor")]

namespace CenturionCC.System.Gun.DataStore
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunRecoilPatternDataStore : UdonSharpBehaviour
    {
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

        public Vector3[] RecoilOffsetPatterns => recoilOffsetPatterns;
        public Vector3[] PositionOffsetPatterns => positionOffsetPatterns;
        public float[] SpeedOffsetPatterns => speedOffsetPatterns;

#if !COMPILER_UDONSHARP && UNITY_EDITOR

        public int MaxPatterns
        {
            get
            {
                var nums = new[]
                    { recoilOffsetPatterns.Length, positionOffsetPatterns.Length, speedOffsetPatterns.Length };
                return nums.Distinct().Aggregate((a, b) => a * b);
            }
        }

#endif

        public virtual Vector3 GetRecoilOffset(int count)
        {
            return recoilOffsetPatterns[count % recoilOffsetPatterns.Length];
        }

        public virtual Vector3 GetPositionOffset(int count)
        {
            return positionOffsetPatterns[count % positionOffsetPatterns.Length];
        }

        public virtual float GetSpeedOffset(int count)
        {
            return speedOffsetPatterns[count % speedOffsetPatterns.Length];
        }

        public virtual void Get(int count, out float speedOffset, out Vector3 recoilOffset, out Vector3 positionOffset)
        {
            speedOffset = speedOffsetPatterns[count % speedOffsetPatterns.Length];
            recoilOffset = recoilOffsetPatterns[count % recoilOffsetPatterns.Length];
            positionOffset = positionOffsetPatterns[count % positionOffsetPatterns.Length];
        }
    }
}