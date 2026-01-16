using CenturionCC.System.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace CenturionCC.System.Editor.EditorWindow
{
    public class CockingGunAnimatorTester : ShooterEditorWindow
    {
        public const float BUTTON_WIDTH = 100F;

        private static GameObject _model;
        private static Animator _animator;

        private static float _triggerProgress = 0;
        private static float _cockingProgress = 0;
        private static float _cockingTwist = 0;
        private static bool _hasBullet = false;
        private static bool _hasCocked = false;
        private static int _state = 0;
        private static bool _isShooting = false;
        private static bool _isShootingEmpty = false;
        private static int _selectorType = 0;

        private static readonly int HashedTriggerProgress = Animator.StringToHash("TriggerProgress");
        private static readonly int HashedCockingProgress = Animator.StringToHash("CockingProgress");
        private static readonly int HashedCockingTwist = Animator.StringToHash("CockingTwist");
        private static readonly int HashedHasBullet = Animator.StringToHash("HasBullet");
        private static readonly int HashedHasCocked = Animator.StringToHash("HasCocked");
        private static readonly int HashedState = Animator.StringToHash("State");
        private static readonly int HashedIsShooting = Animator.StringToHash("IsShooting");
        private static readonly int HashedIsShootingEmpty = Animator.StringToHash("IsShootingEmpty");
        private static readonly int HashedSelectorType = Animator.StringToHash("SelectorType");

        private void OnDisable()
        {
            StopAnimator();
        }


        [MenuItem("Centurion System/Utils/Cocking Gun Animator Tester")]
        public static void InitMenu() => Init();

        public static void Init(GameObject model = null, Animator animator = null)
        {
            // Get existing open window or if none, make a new one:
            CockingGunAnimatorTester window = GetWindow<CockingGunAnimatorTester>();
            window.titleContent.text = "Cocking Gun Animator Tester";
            _model = model;
            _animator = animator;
            window.Show();
        }

        protected override bool DrawInfo()
        {
            return false;
        }

        protected override bool DrawProperty()
        {
            _model = (GameObject)EditorGUILayout.ObjectField("Model", _model, typeof(GameObject), true);
            _animator = (Animator)EditorGUILayout.ObjectField("Animator", _animator, typeof(Animator), true);

            GUIUtil.HorizontalBar();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    _isShooting = GUILayout.Button("Shoot", GUILayout.Width(BUTTON_WIDTH));
                    _triggerProgress =
                        GUILayout.HorizontalSlider(_triggerProgress, 0, 1, GUILayout.Width(BUTTON_WIDTH));
                }

                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Button("Cocking", GUILayout.Width(BUTTON_WIDTH));
                    _cockingProgress =
                        GUILayout.HorizontalSlider(_cockingProgress, 0, 1, GUILayout.Width(BUTTON_WIDTH));
                }
            }


            return true;
        }

        protected override void OnApplyButton()
        {
            StartAnimator();
        }

        protected override bool CanApply()
        {
            return _model != null && _animator != null;
        }

        void StartAnimator()
        {
            Debug.Log("starting animator");
            EditorApplication.update -= UpdateAnimator;
            EditorApplication.update += UpdateAnimator;
        }

        void StopAnimator()
        {
            Debug.Log("stopping animator");
            EditorApplication.update -= UpdateAnimator;
        }

        void UpdateAnimator()
        {
            _animator.SetFloat(HashedTriggerProgress, _triggerProgress);
            _animator.SetFloat(HashedCockingProgress, _cockingProgress);
            _animator.SetFloat(HashedCockingTwist, _cockingTwist);

            _animator.SetBool(HashedHasBullet, _hasBullet);
            _animator.SetBool(HashedHasCocked, _hasCocked);

            _animator.SetInteger(HashedState, _state);
            _animator.SetInteger(HashedSelectorType, _selectorType);

            if (_isShooting)
            {
                _isShooting = false;
                _animator.SetTrigger(HashedIsShooting);
            }

            if (_isShootingEmpty)
            {
                _isShootingEmpty = false;
                _animator.SetTrigger(HashedIsShootingEmpty);
            }

            _animator.Update(ShooterEditorUtil.DeltaTime);
            Debug.Log($"updated animator {_animator.name} with delta time of {ShooterEditorUtil.DeltaTime}");
        }
    }
}
