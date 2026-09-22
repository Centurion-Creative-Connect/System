using System;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;
namespace CenturionCC.System.Gun.Behaviour
{
    public class ObjectToggleBehaviour : GunBehaviourBase
    {
        [SerializeField]
        private GameObject[] targetObjects;
        [SerializeField]
        private bool[] targetObjectStates;
        [SerializeField]
        private bool toggleOnAction = true;

        private bool _lastActiveState = true;

        private void OnValidate()
        {
            if (targetObjectStates.Length != targetObjects.Length)
            {
                targetObjectStates = new bool[targetObjects.Length];
            }

            for (var i = 0; i < targetObjects.Length; i++)
            {
                if (targetObjects[i] == null) continue;
                targetObjectStates[i] = targetObjects[i].activeSelf;
            }
        }

        public override void OnAction(GunBase instance)
        {
            if (toggleOnAction)
            {
                instance._SendNetworkGunBehaviourEvent(NetworkEventTarget.All, nameof(_ToggleObject));
            }
        }

        public void _ActivateObject()
        {
            _SetObjectsActive(true);
        }

        public void _DeactivateObject()
        {
            _SetObjectsActive(false);
        }

        public void _ToggleObject()
        {
            _SetObjectsActive(!_lastActiveState);
        }

        private void _SetObjectsActive(bool b)
        {
            _lastActiveState = b;
            for (var i = 0; i < targetObjects.Length; i++)
            {
                if (targetObjects[i] == null) continue;
                targetObjects[i].SetActive(targetObjectStates[i] == b);
            }
        }
    }
}
