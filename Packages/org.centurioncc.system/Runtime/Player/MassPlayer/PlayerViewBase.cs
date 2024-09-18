using UdonSharp;

namespace CenturionCC.System.Player.MassPlayer
{
    public abstract class PlayerViewBase : UdonSharpBehaviour
    {
        public abstract PlayerBase PlayerModel { get; set; }
        public abstract PlayerCollider[] GetColliders();

        public virtual void Init()
        {
        }

        public abstract void UpdateView();
        public abstract void UpdateCollider();
    }
}