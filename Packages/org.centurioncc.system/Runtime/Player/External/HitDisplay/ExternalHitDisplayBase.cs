using UdonSharp;

namespace CenturionCC.System.Player.External.HitDisplay
{
    public abstract class ExternalHitDisplayBase : UdonSharpBehaviour
    {
        public abstract void Play(PlayerBase player);
    }
}