using CenturionCC.System.Utils;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace CenturionCC.System.UI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FieldMapUI : UdonSharpBehaviour
    {
        [SerializeField]
        private GameObject leftImage;
        [SerializeField]
        private GameObject rightImage;
        [SerializeField]
        private GameObject upImage;
        [SerializeField]
        private GameObject downImage;

        [FormerlySerializedAs("A00IsActive")]
        public bool a00IsActive;
        [FormerlySerializedAs("A01IsActive")]
        public bool a01IsActive;
        [FormerlySerializedAs("B00IsActive")]
        public bool b00IsActive;
        [FormerlySerializedAs("B01IsActive")]
        public bool b01IsActive;

        private void Start()
        {
            var wall = GameObject.Find("WallManager").GetComponent<WallManager>();
            wall.SubscribeCallback(this);
        }

        public void OnUIRefresh()
        {
            leftImage.SetActive(a00IsActive);
            rightImage.SetActive(a01IsActive);
            upImage.SetActive(b00IsActive);
            downImage.SetActive(b01IsActive);
        }
    }
}