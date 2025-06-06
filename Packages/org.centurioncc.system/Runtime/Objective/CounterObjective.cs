using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Objective
{
    public class CounterObjective : ObjectiveBase
    {
        [SerializeField]
        [UdonSynced]
        [FieldChangeCallback(nameof(RequiredCount))]
        private uint requiredCount;

        [UdonSynced]
        [FieldChangeCallback(nameof(CurrentCount))]
        private uint _currentCount;

        public uint RequiredCount
        {
            get => requiredCount;
            private set
            {
                requiredCount = value;
                UpdateProgress();
            }
        }

        public uint CurrentCount
        {
            get => _currentCount;
            private set
            {
                _currentCount = value;
                UpdateProgress();
            }
        }

        public override void Interact()
        {
            if (!IsActiveAndRunning) return;

            ++CurrentCount;
            RequestSync();
        }

        protected override void OnObjectiveStart()
        {
            if (!Networking.IsMaster) return;
            CurrentCount = 0;
            RequestSync();
        }

        private void UpdateProgress()
        {
            if (!Networking.IsMaster) return;
            SetProgress(CurrentCount / (float)RequiredCount);
        }
    }
}