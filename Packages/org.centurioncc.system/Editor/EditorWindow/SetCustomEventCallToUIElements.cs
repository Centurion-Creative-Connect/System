using System.Collections.Generic;
using CenturionCC.System.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VRC.Udon;

namespace CenturionCC.System.Editor.EditorWindow
{
    public class SetCustomEventCallToUIElements : ShooterEditorWindow
    {
        private static UIType _uiType;
        private static string _methodName;
        private static UdonBehaviour _behaviour;
        private static GameObject _gameObject;

        [MenuItem("Centurion-Utils/Set SendCustomEvent call to UI Elements")]
        public static void InitMenu() => Init();

        public static void Init(UdonBehaviour behaviour = null)
        {
            // Get existing open window or if none, make a new one:
            SetCustomEventCallToUIElements window = GetWindow<SetCustomEventCallToUIElements>();
            window.titleContent.text = "Set SendCustomEvent call to UI Elements";
            _behaviour = behaviour;
            window.Show();
        }

        protected override bool DrawInfo()
        {
            return false;
        }

        protected override bool DrawProperty()
        {
            _uiType = (UIType) EditorGUILayout.EnumPopup("UI Type", _uiType);
            _methodName = EditorGUILayout.TextField("Method Name", _methodName);
            _behaviour =
                (UdonBehaviour) EditorGUILayout.ObjectField("Udon Behaviour", _behaviour,
                    typeof(UdonBehaviour), true);
            _gameObject =
                (GameObject) EditorGUILayout.ObjectField("Root Object", _gameObject, typeof(GameObject), true);

            if (GUILayout.Button("Clear Events"))
            {
                Debug.Log("Removing events");
                UnityEventBase[] bases = GetEventBases(_uiType, out Object[] objects);
                Undo.RecordObjects(objects, "Remove UI Events");

                Debug.Log($"Count: {bases.Length}");
                foreach (UnityEventBase eventBase in bases)
                {
                    ShooterEditorUtil.RemovePersistent(eventBase);
                }

                Debug.Log("Done!");
            }

            return true;
        }

        protected override void OnApplyButton()
        {
            Debug.Log("Start Processing");

            UnityEventBase[] eventBases = GetEventBases(_uiType, out Object[] objects);
            Undo.RecordObjects(objects, "Add UI Events");

            UdonBehaviour ub = _behaviour;
            UnityAction<string> sce = ub.SendCustomEvent;
            Debug.Log($"Count: {eventBases.Length}");

            foreach (UnityEventBase eventBase in eventBases)
            {
                ShooterEditorUtil.RemovePersistent(eventBase);
                ShooterEditorUtil.AssignPersistent(eventBase, sce, _methodName);
            }

            Debug.Log("Done!");
        }

        private UnityEventBase[] GetEventBases(UIType type, out Object[] objects)
        {
            switch (_uiType)
            {
                case UIType.Button:
                {
                    Debug.Log("Getting Buttons");
                    Button[] buttons = _gameObject.GetComponentsInChildren<Button>();

                    List<UnityEventBase> bases = new List<UnityEventBase>();
                    foreach (Button button in buttons)
                    {
                        bases.Add(button.onClick);
                    }

                    objects = buttons;
                    return bases.ToArray();
                }
                case UIType.Toggle:
                {
                    Debug.Log("Getting Toggles");
                    Toggle[] toggles = _gameObject.GetComponentsInChildren<Toggle>();

                    List<UnityEventBase> bases = new List<UnityEventBase>();
                    foreach (Toggle toggle in toggles)
                    {
                        bases.Add(toggle.onValueChanged);
                    }

                    objects = toggles;
                    return bases.ToArray();
                }

                default:
                {
                    Debug.Log("Not specified!");
                    objects = null;
                    return new UnityEventBase[0];
                }
            }
        }

        protected override bool CanApply()
        {
            return _behaviour != null && _gameObject != null && !string.IsNullOrWhiteSpace(_methodName);
        }

        enum UIType
        {
            Button,
            Toggle,
        }
    }
}