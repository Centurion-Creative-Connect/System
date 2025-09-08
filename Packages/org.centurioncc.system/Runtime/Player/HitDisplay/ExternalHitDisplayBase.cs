using UdonSharp;

namespace CenturionCC.System.Player.HitDisplay
{
    public abstract class ExternalHitDisplayBase : UdonSharpBehaviour
    {
        public abstract void Play(PlayerBase player);
    }
}