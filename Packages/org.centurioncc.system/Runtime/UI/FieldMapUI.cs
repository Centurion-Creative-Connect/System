using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.UI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FieldMapUI : UdonSharpBehaviour
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private WallManager wallManager;

        [SerializeField]
        private GameObject leftImage;
        [SerializeField]
        private GameObject rightImage;
        [SerializeField]
        private GameObject upImage;
        [SerializeField]
        private GameObject downImage;

        private void Start()
        {
            wallManager.SubscribeCallback(this);
        }

        public void OnUIRefresh()
        {
            leftImage.SetActive(A00IsActive);
            rightImage.SetActive(A01IsActive);
            upImage.SetActive(B00IsActive);
            downImage.SetActive(B01IsActive);
        }

        // Disable inconsistent naming warn for WallManager callback field
        // ReSharper disable InconsistentNaming
        public bool A00IsActive;
        public bool A01IsActive;
        public bool B00IsActive;
        public bool B01IsActive;
        // ReSharper restore InconsistentNaming
    }
}