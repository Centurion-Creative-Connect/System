using CenturionCC.System.Editor.Utils;
using CenturionCC.System.UI;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VRC.Udon;

namespace CenturionCC.System.Editor.EditorInspector
{
    //[CustomEditor(typeof(PlayerManagerView))]
    public class PlayerManagerUIEditor : ShooterEditor
    {
        private static string HelpBoxButtonEventsNotAssigned =>
            string.Format(GUIUtil.NotAssignedStringFormat, "Button UI Event");
        private static string HelpBoxApplyButtonEvent => string.Format(GUIUtil.ApplyStringFormat, "Button UI Event");

        protected override bool DrawInfo()
        {
            PlayerManagerView instance = target as PlayerManagerView;
            if (instance == null) return false;

            if (!ShooterEditorUtil.HasUdonEventAtOnClick(instance.joinButton.onClick) ||
                !ShooterEditorUtil.HasUdonEventAtOnClick(instance.leaveButton.onClick) ||
                !ShooterEditorUtil.HasUdonEventAtOnClick(instance.resetButton.onClick) ||
                !ShooterEditorUtil.HasUdonEventAtOnClick(instance.updateButton.onClick))
                Button_AssignUIEvents(instance, false);


            return false;
        }


        protected override bool DrawUtils()
        {
            PlayerManagerView instance = target as PlayerManagerView;
            if (instance == null) return false;

            Button_AssignUIEvents(instance);

            return true;
        }

        private static void Button_AssignUIEvents(PlayerManagerView instance, bool isUtil = true)
        {
            bool fix = GUIUtil.HelpBoxWithButton(
                isUtil ? HelpBoxApplyButtonEvent : HelpBoxButtonEventsNotAssigned,
                isUtil ? MessageType.Info : MessageType.Error);

            if (fix)
                Util_AssignUIEvents(instance);
        }

        private static void Util_AssignUIEvents(PlayerManagerView instance)
        {
            Button joinB = instance.joinButton;
            Button leaveB = instance.leaveButton;
            Button resetB = instance.resetButton;
            Button updateB = instance.updateButton;
            UdonBehaviour ub = UdonSharpEditorUtility.GetBackingUdonBehaviour(instance);
            UnityAction<string> sce = ub.SendCustomEvent;

            Undo.RecordObjects(new Object[] {joinB, leaveB, resetB, updateB},
                "Auto-Assign UI events from PlayerManagerUI");

            ShooterEditorUtil.AssignPersistent(joinB.onClick, sce, nameof(instance.HandleJoinButton));
            ShooterEditorUtil.AssignPersistent(leaveB.onClick, sce, nameof(instance.HandleLeaveButton));
            ShooterEditorUtil.AssignPersistent(resetB.onClick, sce, nameof(instance.HandleResetButton));
            ShooterEditorUtil.AssignPersistent(updateB.onClick, sce, nameof(instance.HandleUpdateButton));
        }
    }
}