using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player.PlayerExternal
{
    public abstract class ExternalPlayerTagBase : UdonSharpBehaviour
    {
        public VRCPlayerApi followingPlayer;

        public abstract void Setup(ExternalPlayerTagManager manager, VRCPlayerApi api);
        public abstract void SetTagOn(TagType type, bool isOn);
        public abstract void SetTeamTag(int teamId, Color teamColor);

        public void DestroyThis()
        {
            Destroy(gameObject);
        }
    }
}