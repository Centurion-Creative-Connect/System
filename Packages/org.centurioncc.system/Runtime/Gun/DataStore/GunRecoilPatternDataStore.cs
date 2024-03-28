using System;
using System.Collections.Generic;
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

#if !COMPILER_UDONSHARP && UNITY_EDITOR

        public int MaxPatterns => Lcm(new[]
            { recoilOffsetPatterns.Length, positionOffsetPatterns.Length, speedOffsetPatterns.Length });

        private static int Lcm(int[] vs)
        {
            vs = vs.Where(x => x > 1).ToArray();
            if (vs.Length < 2)
                return vs.Length == 1 ? vs[0] : 1;

            var div = 2;
            var divs = new List<int>();

            while (true)
            {
                if (vs.Count(x => x % div == 0) == vs.Length)
                {
                    vs = vs.Select(x => x / div).ToArray();
                    divs.Add(div);
                }
                else
                {
                    div++;
                }

                Array.Sort(vs);
                if (vs[vs.Length - 2] < div) break;
            }

            return divs.Aggregate(1, (current, i) => current * i);
        }
#endif
    }
}