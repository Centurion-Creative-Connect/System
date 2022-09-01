using CenturionCC.System.Audio;
using UdonSharp;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class JoinLeaveNotifier : UdonSharpBehaviour
    {
        public bool playOnJoin;
        public bool playOnLeave;
        public AudioPlayer joinAudio;
        public AudioPlayer leaveAudio;

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (playOnJoin)
                joinAudio.Play(true);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (playOnLeave)
                leaveAudio.Play(true);
        }
    }
}