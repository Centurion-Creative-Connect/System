using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CenturionCC.System.Utils
{
    [RequireComponent(typeof(Collider))] [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SpeedChecker : UdonSharpBehaviour
    {
        public Text text;
        public string textFormat = "{0}: {1:F2} m/s";
        public int count;

        private void Start()
        {
            DisplaySpeed(-1);
        }

        public void OnTriggerEnter(Collider other)
        {
            var rb = other.gameObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                DisplaySpeed(-1);
                return;
            }

            var speed = rb.velocity.magnitude;
            DisplaySpeed(speed);
        }

        public void ResetCounter()
        {
            count = 0;
        }

        private void DisplaySpeed(float speed)
        {
            var displayValue = $"{speed}";
            if (Mathf.Approximately(speed, -1)) displayValue = "ERR";
            text.text = string.Format(textFormat, ++count, displayValue);
        }
    }
}