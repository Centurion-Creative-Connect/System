using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Gimmick.Defuser
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DefuserBombDetector : UdonSharpBehaviour
    {
        [SerializeField]
        private Defuser defuser;

        public void OnBombEntered()
        {
            defuser.CanPlant = true;
        }

        public void OnBombExit()
        {
            defuser.CanPlant = false;
        }

        // private void OnTriggerEnter(Collider other)
        // {
        //     Debug.Log($"ontriggerenter: {other.name}");
        //     var bomb = other.GetComponent<Bomb>();
        //     if (bomb == null) return;
        //
        //     defuser.CanPlant = true;
        // }
        //
        // private void OnTriggerExit(Collider other)
        // {
        //     Debug.Log($"ontriggerexit: {other.name}");
        //     var bomb = other.GetComponent<Bomb>();
        //     if (bomb == null) return;
        //
        //     defuser.CanPlant = false;
        // }
    }
}
