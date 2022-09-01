using UdonSharp;

namespace CenturionCC.System.Utils.Watchdog
{
    public class WatchdogChildCallbackBase : UdonSharpBehaviour
    {
        /// <summary>
        /// WatchdogProc will call this method to check if UdonBehaviour has halted or not.
        /// </summary>
        /// <param name="wd">Caller</param>
        /// <param name="nonce">Random int value to return</param>
        /// <returns></returns>
        public int ChildKeepAlive(WatchdogProc wd, int nonce)
        {
            return nonce;
        }
    }
}
