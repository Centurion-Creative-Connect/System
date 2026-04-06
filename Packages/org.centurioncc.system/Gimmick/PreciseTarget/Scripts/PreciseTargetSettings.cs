using UdonSharp;
namespace CenturionCC.System.Gimmick.PreciseTarget
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PreciseTargetSettings : UdonSharpBehaviour
    {
        public bool isEnabled = true;
        public bool showLocalPlayerHits = true;
        public bool showOtherPlayerHits = true;
    }
}
