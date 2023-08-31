using System;
using UdonSharp;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FeatureFlags : UdonSharpBehaviour
    {
        // These field will be changed using UdonBehaviour#SetProgramVariable
        // ReSharper disable FieldCanBeMadeReadOnly.Global ConvertToConstant.Global
        [NonSerialized]
        public bool useVictimRequest = false;
        [NonSerialized]
        public bool useConditionalResultCheck = false;
        [NonSerialized]
        public bool doNotifyIfLocalHitCancelled = true;
        [NonSerialized]
        public bool doNotifyIfRemoteHitCancelled = false;
        [NonSerialized]
        public bool makeSyncerPlayerIdDependent = true;
        // ReSharper restore FieldCanBeMadeReadOnly.Global ConvertToConstant.Global
    }
}