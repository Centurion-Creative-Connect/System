using UdonSharp;
namespace CenturionCC.System.Player
{
    public abstract class PlayerViewBase : UdonSharpBehaviour
    {
        /// <summary>
        /// Called when PlayerBase wants to update visual elements.
        /// </summary>
        /// <remarks>
        /// This isn't called every frame, but called each 
        /// </remarks>
        public virtual void OnUpdateView()
        {
        }
    }
}
