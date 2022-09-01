using UdonSharp;

namespace CenturionCC.System.Utils.Watchdog
{
    public class WatchdogCallbackBase : UdonSharpBehaviour
    {
        public int KeepAlive(WatchdogProc wd, int nonce)
        {
            return nonce;
        }

        public WatchdogChildCallbackBase[] GetChildren()
        {
            return null;
        }
    }
}