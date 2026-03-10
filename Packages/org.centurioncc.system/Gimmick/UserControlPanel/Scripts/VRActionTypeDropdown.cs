using CenturionCC.System.Gun;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using System;
using System.Linq;
using TMPro;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Gimmick.UserControlPanel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)] [RequireComponent(typeof(TMP_Dropdown))]
    public class VRActionTypeDropdown : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject(SearchScope.Self)]
        private TMP_Dropdown dropdown;
        [SerializeField]
        private TMP_Text captionText;
        [SerializeField] [HideInInspector]
        private VRActionType[] allTypes;
        [SerializeField] [HideInInspector]
        private string[] allTypeNames;
        [SerializeField]
        private int currentType;
        [SerializeField]
        private string callbackEventName;
        [SerializeField]
        private UdonSharpBehaviour callbackBehaviour;

        private bool _isNotifyingChange;

        private void Start()
        {
            UpdateDisplay();
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP

        private void OnValidate()
        {
            allTypes = Enum.GetValues(typeof(VRActionType)).Cast<VRActionType>().ToArray();
            allTypeNames = Enum.GetNames(typeof(VRActionType));

            dropdown = GetComponent<TMP_Dropdown>();
            dropdown.ClearOptions();
            var options = allTypeNames.ToList();
            options.Add("");
            dropdown.AddOptions(options);

            captionText = dropdown.captionText;
        }

#endif

        public void SetCallback(UdonSharpBehaviour behaviour, string eventName)
        {
            callbackBehaviour = behaviour;
            callbackEventName = eventName;
        }

        public void OnDropdownValueChanged()
        {
            var input = dropdown.value;
            var flag = (int)allTypes[input];
            var actionType = (VRActionType)flag;
            if (actionType == VRActionType.None)
            {
                SetValue(0);
                return;
            }

            var current = currentType;
            var edited = false;

            edited |= ResolveBindConflict(ref current, (((int)VRActionType.GunDirectionUp) | ((int)VRActionType.GunDirectionDown)), flag);
            edited |= ResolveBindConflict(ref current, (((int)VRActionType.InputLookUp) | ((int)VRActionType.InputLookDown)), flag);
            if (!edited)
            {
                current = BitFlag.Toggle(current, flag);
            }

            SetValue(current);
        }

        public void SetValue(int flags)
        {
            SetValueWithoutNotify(flags);
            NotifyChange();
        }

        public void SetValueWithoutNotify(int flags)
        {
            currentType = flags;
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            var value = "";
            for (int i = 0; i < allTypes.Length; i++)
            {
                var type = allTypes[i];
                if (((int)type & currentType) != 0)
                {
                    value += allTypeNames[i] + " ";
                }
            }

            dropdown.SetValueWithoutNotify(32);
            captionText.text = value.Length != 0 ? value : "None";
        }

        public int GetValue() => currentType;

        private void NotifyChange()
        {
            if (_isNotifyingChange) return;

            _isNotifyingChange = true;
            if (callbackBehaviour != null) callbackBehaviour.SendCustomEvent(callbackEventName);
            _isNotifyingChange = false;
        }

        private static bool ResolveBindConflict(ref int flags, int mask, int input)
        {
            if ((input & mask) == 0)
            {
                return false;
            }

            var maskExcludingInput = BitFlag.Set(mask, input, false);
            flags = BitFlag.Set(flags, maskExcludingInput, false);
            flags = BitFlag.Toggle(flags, input);
            return true;
        }
    }
}
