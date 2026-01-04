using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Gimmick.Defuser
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Bomb : UdonSharpBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            var bombDetector = other.GetComponentInChildren<DefuserBombDetector>();
            if (bombDetector == null) return;
            bombDetector.OnBombEntered();
        }

        private void OnTriggerExit(Collider other)
        {
            var bombDetector = other.GetComponentInChildren<DefuserBombDetector>();
            if (bombDetector == null) return;
            bombDetector.OnBombExit();
        }
    }
}
