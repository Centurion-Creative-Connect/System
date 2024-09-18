using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AnimatorDebugger : UdonSharpBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Text text;

        private AnimatorControllerParameter[] _animatorParams;

        private void Start()
        {
            _animatorParams = animator.parameters;
        }

        private void Update()
        {
            string tmp = "";
            foreach (var param in _animatorParams)
            {
                switch (param.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        tmp += $"{param.name,-16}(b): {animator.GetBool(param.nameHash)}\n";
                        break;
                    case AnimatorControllerParameterType.Float:
                        tmp += $"{param.name,-16}(f): {animator.GetFloat(param.nameHash)}\n";
                        break;
                    case AnimatorControllerParameterType.Int:
                        tmp += $"{param.name,-16}(i): {animator.GetInteger(param.nameHash)}\n";
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        tmp += $"{param.name,-16}(t): {animator.GetBool(param.nameHash)}\n";
                        break;
                    default:
                        tmp += $"{param.name,-16}(?): {animator.GetInteger(param.nameHash)}\n";
                        break;
                }
            }

            text.text = tmp;
        }
    }
}