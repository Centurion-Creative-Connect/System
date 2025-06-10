using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun.GunCamera
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunCameraDataStore : UdonSharpBehaviour
    {
        [SerializeField]
        private Transform[] offsets;

        [SerializeField]
        private bool updateDynamically;

        [NonSerialized]
        private Vector3[] _cachedPosOffsets;

        [NonSerialized]
        private Quaternion[] _cachedRotOffsets;

        [PublicAPI]
        public Vector3[] PositionOffsets
        {
            get
            {
                if (offsets == null || offsets.Length == 0)
                    return GetDefaultPositionOffsets();

                if (_cachedPosOffsets == null || updateDynamically)
                {
                    var gunCamPosOffsets = new Vector3[offsets.Length];
                    for (var i = 0; i < offsets.Length; i++)
                        gunCamPosOffsets[i] = offsets[i].localPosition;
                    _cachedPosOffsets = gunCamPosOffsets;
                }

                return _cachedPosOffsets;
            }
        }

        [PublicAPI]
        public Quaternion[] RotationOffsets
        {
            get
            {
                if (offsets == null || offsets.Length == 0)
                    return GetDefaultRotationOffsets();

                if (_cachedRotOffsets == null || updateDynamically)
                {
                    var gunCamRotOffsets = new Quaternion[offsets.Length];
                    for (var i = 0; i < offsets.Length; i++)
                        gunCamRotOffsets[i] = offsets[i].localRotation;
                    _cachedRotOffsets = gunCamRotOffsets;
                }

                return _cachedRotOffsets;
            }
        }

        [PublicAPI]
        public static Vector3[] GetDefaultPositionOffsets()
        {
            return new[] { new Vector3(0.055F, 0.038F, 0.35F), new Vector3(0.055F, 0.038F, 0.35F) };
        }

        [PublicAPI]
        public static Quaternion[] GetDefaultRotationOffsets()
        {
            return new[] { Quaternion.Euler(0, 0, 0), Quaternion.Euler(0, 180, 0) };
        }

        [PublicAPI]
        public static Vector3[] GetOrDefaultPositionOffsets(GunCameraDataStore store)
        {
            return store == null ? GetDefaultPositionOffsets() : store.PositionOffsets;
        }

        [PublicAPI]
        public static Quaternion[] GetOrDefaultRotationOffsets(GunCameraDataStore store)
        {
            return store == null ? GetDefaultRotationOffsets() : store.RotationOffsets;
        }
    }
}