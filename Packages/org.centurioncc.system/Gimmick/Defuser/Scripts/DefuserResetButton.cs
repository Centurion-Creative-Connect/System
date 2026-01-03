using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Gimmick.Defuser
{
    public class DefuserResetButton : UdonSharpBehaviour
    {
        [SerializeField]
        private Defuser[] defusers;

        private Vector3[] _originalPositions;
        private Quaternion[] _originalRotations;

        private void Start()
        {
            var len = defusers.Length;
            _originalPositions = new Vector3[len];
            _originalRotations = new Quaternion[len];

            for (var i = 0; i < defusers.Length; i++)
            {
                if (defusers[i] == null) continue;
                var t = defusers[i].transform;
                _originalPositions[i] = t.position;
                _originalRotations[i] = t.rotation;
            }
        }

        public override void Interact()
        {
            ResetDefusers();
        }

        public void ResetDefusers()
        {
            for (var i = 0; i < defusers.Length; i++)
            {
                if (defusers[i] == null) continue;
                var defuser = defusers[i];

                defuser.ResetDefuser();
                var vrcPickup = defuser.VRCPickup;
                if (vrcPickup != null)
                    vrcPickup.transform.SetPositionAndRotation(_originalPositions[i], _originalRotations[i]);
            }
        }
    }
}
