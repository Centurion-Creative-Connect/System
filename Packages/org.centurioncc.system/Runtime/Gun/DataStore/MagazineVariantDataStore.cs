using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun.DataStore
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MagazineVariantDataStore : UdonSharpBehaviour
    {
        [SerializeField] private int type;
        [SerializeField] private int roundsCapacity;
        [SerializeField] private GameObject model;
        [SerializeField] private Transform modelOffset;
        [SerializeField] private Transform leftHandOffset;
        [SerializeField] private Transform rightHandOffset;
        [SerializeField] private BoxCollider secondaryMagazineDetectionCollider;

        public int Type => type;
        public int RoundsCapacity => roundsCapacity;

        public GameObject Model => model;

        public Vector3 ModelOffsetPosition => modelOffset.localPosition;
        public Quaternion ModelOffsetRotation => modelOffset.localRotation;

        public Transform LeftHandOffset => leftHandOffset;
        public Transform RightHandOffset => rightHandOffset;

        public BoxCollider SecondaryMagazineDetectionCollider => secondaryMagazineDetectionCollider;
    }
}