using CenturionCC.System.Editor.ControlPanel;
using System;
using CenturionCC.System.Editor.Utils;
using CenturionCC.System.Gun;
using CenturionCC.System.Gun.DataStore;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace CenturionCC.System.Editor.EditorInspector.Gun.DataStore
{
    [CustomEditor(typeof(GunVariantDataStore))]
    public class GunVariantDataStoreEditor : UnityEditor.Editor
    {

        private static readonly string[] PlayerMovementMessages =
        {
            "Will be using default values specified in PlayerController",
            "Values specified here will be directly applied.",
            "Values specified here will be multiplied by PlayerController values.",
            "Will not change movement behavior by gun's direction. Player will move freely unless they have other guns activating MovementOption.",
        };

        private static readonly string[] CombatTagMessages =
        {
            "Will be using default values specified in PlayerController",
            "Values specified here will be directly applied.",
            "Values specified here will be multiplied by PlayerController values.",
            "Will not slow down when shooting unless they have other guns activating CombatTag.",
        };

        private static Dictionary<string, GUIContent> GUIContents = new Dictionary<string, GUIContent>
        {
            { "FireMode", new GUIContent("Fire Mode", "Firing mode when holding down the trigger.") },
            { "FireModeTooltip", new GUIContent("", "Firing mode when holding down the trigger.") },
            { "MaxRPM", new GUIContent("Rounds Per Minute (RPM)", "Rounds per Minute.") },
            { "MaxRPMTooltip", new GUIContent("", "Rounds per Minute.") },
            { "MaxRPS", new GUIContent("Rounds Per Second (RPS)", "Rounds per Second.") },
            { "MaxRPSTooltip", new GUIContent("", "Rounds per Second.") },
            { "PerBurstInterval", new GUIContent("Per Burst Intervals", "Minimum seconds required between bursts.") },
            { "PerBurstIntervalTooltip", new GUIContent("", "Minimum seconds required between bursts.") },
        };

        private static bool _foldoutReferences = true;
        private static bool _foldoutOffsets = true;
        private static bool _foldoutMessages = true;
        private static bool _foldoutObjectMarker = true;
        private static bool _foldoutPlayerController = true;
        private static bool _foldoutFireMode = true;
        private static bool _foldoutObsolete;
        private static bool _foldoutAdvancedOptions;

        public void OnSceneGUI()
        {
            DrawHandles((GunVariantDataStore)target);
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            var so = serializedObject;
            var dataVersion = so.FindProperty("dataVersion").intValue;
            if (dataVersion != GunVariantDataStore.DataVersion)
            {
                if (dataVersion < GunVariantDataStore.DataVersion)
                {
                    EditorGUILayout.HelpBox(
                        "This VariantData has outdated properties.",
                        MessageType.Warning
                    );

                    if (GUILayout.Button("Open Control Panel"))
                    {
                        CenturionSystemControlPanelWindow.OpenWindow();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "This VariantData has future version properties that could not be read or stored. Please update Centurion System package!",
                        MessageType.Error
                    );
                }
            }

            EditorGUILayout.PropertyField(so.FindProperty("uniqueId"));
            EditorGUILayout.PropertyField(so.FindProperty("weaponName"));
            EditorGUILayout.PropertyField(so.FindProperty("holsterSize"));
            EditorGUILayout.PropertyField(so.FindProperty("ergonomics"));
            EditorGUILayout.PropertyField(so.FindProperty("animator"));
            EditorGUILayout.PropertyField(so.FindProperty("syncedAnimatorParameterNames"));
            EditorGUILayout.PropertyField(so.FindProperty("behaviours"));
            EditorGUILayout.PropertyField(so.FindProperty("useWallCheck"));
            EditorGUILayout.PropertyField(so.FindProperty("useSafeZoneCheck"));
            EditorGUILayout.PropertyField(so.FindProperty("projectilePoolOverride"));

            if (GUIUtil.Foldout("Fire Mode Settings", ref _foldoutFireMode))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawFireModeInspector(so);
                }
            }

            if (GUIUtil.Foldout("Offset References", ref _foldoutOffsets))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(so.FindProperty("shooterOffset"));
                    EditorGUILayout.PropertyField(so.FindProperty("mainHandleOffset"));
                    EditorGUILayout.PropertyField(so.FindProperty("mainHandlePitchOffset"));
                    EditorGUILayout.PropertyField(so.FindProperty("subHandleOffset"));
                    EditorGUILayout.PropertyField(so.FindProperty("isDoubleHanded"));
                }
            }

            if (GUIUtil.Foldout("DataStore References", ref _foldoutReferences))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    GUIUtil.FoldoutPropertyField(so.FindProperty("projectileData"), 3);
                    GUIUtil.FoldoutPropertyField(so.FindProperty("audioData"), 3);
                    GUIUtil.FoldoutPropertyField(so.FindProperty("hapticData"), 3);
                    GUIUtil.FoldoutPropertyField(so.FindProperty("cameraData"), 3);
                }
            }

            if (GUIUtil.Foldout("Message Settings", ref _foldoutMessages))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(so.FindProperty("desktopTooltip"));
                    EditorGUILayout.PropertyField(so.FindProperty("vrTooltip"));
                }
            }

            if (GUIUtil.Foldout("ObjectMarker Settings", ref _foldoutObjectMarker))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(so.FindProperty("objectType"));
                    EditorGUILayout.PropertyField(so.FindProperty("objectWeight"));
                    EditorGUILayout.PropertyField(so.FindProperty("tags"));
                }
            }

            if (GUIUtil.Foldout("PlayerController Settings", ref _foldoutPlayerController))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawPlayerControllerInspector(so);
                }
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(so.FindProperty("dataVersion"));
                if (GUIUtil.Foldout("Obsolete Fields", ref _foldoutObsolete))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(so.FindProperty("behaviour"));
                        EditorGUILayout.PropertyField(so.FindProperty("maxRoundsPerSecond"));
                    }
                }
            }

            if (GUIUtil.Foldout("Advanced Options", ref _foldoutAdvancedOptions))
            {
            }

            so.ApplyModifiedProperties();
        }

        public static void DrawHandles(GunVariantDataStore data)
        {
            var s = new GUIStyle
                { fontSize = 16, normal = { textColor = Color.white }, alignment = TextAnchor.MiddleCenter };
            var t = data.transform;
            var pos = t.position;
            var rot = t.rotation;
            Handles.Label(pos + Vector3.up * .3F, $"{data.WeaponName}\nId: {data.UniqueId}", s);

            var pickupColor = new Color(.5F, .5F, 1F, .2F);
            DrawWireSphere(pos + rot * data.MainHandlePositionOffset, .05F, pickupColor, "MainHandle");
            if (data.IsDoubleHanded)
                DrawWireSphere(pos + rot * data.SubHandlePositionOffset, .05F, pickupColor, "SubHandle");

            if (data.AudioData != null)
                GunAudioDataStoreEditor.DrawHandles(data.transform, data.AudioData);

            if (data.ProjectileData != null && data.ProjectileData as GunBulletDataStore != null)
                GunBulletDataStoreEditor.DrawHandles(data.transform, data.FiringPositionOffset,
                    data.FiringRotationOffset,
                    (GunBulletDataStore)data.ProjectileData);
        }

        private static void DrawWireSphere(Vector3 pos, float radius, Color color, string text, GUIStyle style = null)
        {
            Handles.color = color;
            Handles.SphereHandleCap(0, pos, Quaternion.identity, radius, EventType.Repaint);

            if (string.IsNullOrWhiteSpace(text))
                return;

            if (style == null)
                style = new GUIStyle { fontSize = 16, normal = { textColor = Color.white } };

            Handles.zTest = CompareFunction.Disabled;
            Handles.Label(pos + Vector3.up * 0.025F, text, style);
        }

        private static void DrawPlayerControllerInspector(SerializedObject so)
        {
            var movementOptionProperty = so.FindProperty("movementOption");
            DrawEnumDisabledProperties(movementOptionProperty, 3, false, i => i == 0 || i == 3, i =>
            {
                EditorGUILayout.HelpBox(
                    PlayerMovementMessages[i],
                    MessageType.Info
                );
            });

            var combatTagOptionProperty = so.FindProperty("combatTagOption");
            DrawEnumDisabledProperties(combatTagOptionProperty, 2, false, i => i == 0 || i == 3, i =>
            {
                EditorGUILayout.HelpBox(
                    CombatTagMessages[i],
                    MessageType.Info
                );
            });
        }

        private static void DrawFireModeInspector(SerializedObject so)
        {
            var fireModesArrayProperty = so.FindProperty("availableFiringModes");
            var secondsPerRoundArrayProperty = so.FindProperty("secondsPerRoundArray");
            var triggerTapIntervalArrayProperty = so.FindProperty("perBurstIntervalArray");

            if (fireModesArrayProperty.arraySize == 0)
            {
                fireModesArrayProperty.arraySize = 1;
            }

            if (fireModesArrayProperty.arraySize != secondsPerRoundArrayProperty.arraySize ||
                fireModesArrayProperty.arraySize != triggerTapIntervalArrayProperty.arraySize)
            {
                secondsPerRoundArrayProperty.arraySize = fireModesArrayProperty.arraySize;
                triggerTapIntervalArrayProperty.arraySize = fireModesArrayProperty.arraySize;
            }

            var fireModeSettings = new List<FireModeSetting>();
            for (var i = 0; i < fireModesArrayProperty.arraySize; i++)
            {
                var fireModeProperty = fireModesArrayProperty.GetArrayElementAtIndex(i);
                var secondsPerRoundProperty = secondsPerRoundArrayProperty.GetArrayElementAtIndex(i);
                var triggerTapIntervalProperty = triggerTapIntervalArrayProperty.GetArrayElementAtIndex(i);

                var fireMode = (FireMode)fireModeProperty.enumValueIndex;
                var secondsPerRound = secondsPerRoundProperty.floatValue;
                var tapInterval = triggerTapIntervalProperty.floatValue;
                var helpBoxContents = string.Empty;
                var helpBoxMessageType = MessageType.None;

                if (fireMode == FireMode.FullAuto)
                {
                    if (float.IsNegative(secondsPerRound) || float.IsInfinity(secondsPerRound))
                    {
                        helpBoxContents = "Negative or Infinity is not a valid value for \"RPM\" or \"RPS\", and can cause unexpected behaviour at runtime.";
                        helpBoxMessageType = MessageType.Error;
                    }
                }

                if (fireMode != FireMode.Safety && secondsPerRound == 0)
                {
                    helpBoxContents = "0 is not a valid value for \"RPM\" or \"RPS\", and can cause unexpected behaviour at runtime.";
                    helpBoxMessageType = MessageType.Error;
                }

                if (float.IsPositiveInfinity(tapInterval))
                {
                    helpBoxContents = "Setting \"Per Burst Intervals\" to Infinity disables further firing.";
                    helpBoxMessageType = MessageType.Warning;
                }

                fireModeSettings.Add(
                    new FireModeSetting
                    {
                        FireMode = fireMode,
                        MaxRoundsPerSecond = float.IsFinite(secondsPerRound) && secondsPerRound != 0 ? 1 / secondsPerRound : secondsPerRound,
                        MinTriggerTapIntervals = tapInterval,
                        HelpBoxContents = helpBoxContents,
                        HelpBoxMessageType = helpBoxMessageType,
                    }
                );
            }

            var reorderableList = new ReorderableList(fireModeSettings, typeof(FireModeSetting))
            {
                elementHeightCallback = idx => string.IsNullOrEmpty(fireModeSettings[idx].HelpBoxContents) ? EditorGUIUtility.singleLineHeight : EditorGUIUtility.singleLineHeight * 3,
                drawHeaderCallback = rect =>
                {
                    var singleRect = new Rect(rect.x, rect.y, rect.width / 4 - 10, rect.height);
                    EditorGUI.LabelField(singleRect, GUIContents["FireMode"]);
                    singleRect.x += singleRect.width + 5;
                    EditorGUI.LabelField(singleRect, GUIContents["MaxRPS"]);
                    singleRect.x += singleRect.width + 5;
                    EditorGUI.LabelField(singleRect, GUIContents["MaxRPM"]);
                    singleRect.x += singleRect.width + 5;
                    EditorGUI.LabelField(singleRect, GUIContents["PerBurstInterval"]);
                },
                drawElementCallback = (rect, index, _, _) =>
                {
                    var setting = fireModeSettings[index];
                    var singleRect = new Rect(rect.x, rect.y, rect.width / 4 - 10, EditorGUIUtility.singleLineHeight);
                    EditorGUI.BeginChangeCheck();

                    var newMode = (FireMode)EditorGUI.EnumPopup(singleRect, setting.FireMode);
                    GUI.Label(singleRect, GUIContents["FireModeTooltip"]);

                    singleRect.x += singleRect.width + 5;

                    EditorGUI.BeginChangeCheck();
                    var newRps = EditorGUI.FloatField(singleRect, setting.MaxRoundsPerSecond);
                    GUI.Label(singleRect, GUIContents["MaxRPSTooltip"]);
                    var rpsChanged = EditorGUI.EndChangeCheck();

                    singleRect.x += singleRect.width + 5;

                    EditorGUI.BeginChangeCheck();
                    var newRpm = EditorGUI.FloatField(singleRect, setting.MaxRoundsPerSecond * 60);
                    GUI.Label(singleRect, GUIContents["MaxRPSTooltip"]);
                    var rpmChanged = EditorGUI.EndChangeCheck();

                    singleRect.x += singleRect.width + 5;

                    var newTapIntervals = EditorGUI.FloatField(singleRect, setting.MinTriggerTapIntervals);
                    GUI.Label(singleRect, GUIContents["PerBurstIntervalTooltip"]);

                    if (!string.IsNullOrEmpty(setting.HelpBoxContents))
                    {
                        var helpBoxRect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 1.25f, rect.width, EditorGUIUtility.singleLineHeight * 1.5f);
                        EditorGUI.HelpBox(helpBoxRect, setting.HelpBoxContents, setting.HelpBoxMessageType);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        fireModesArrayProperty.GetArrayElementAtIndex(index).enumValueIndex = (int)newMode;
                        var sprProperty = secondsPerRoundArrayProperty.GetArrayElementAtIndex(index);
                        if (rpsChanged)
                        {
                            sprProperty.floatValue = float.IsFinite(newRps) && newRps != 0 ? 1 / newRps : newRps;
                        }
                        else if (rpmChanged)
                        {
                            sprProperty.floatValue = float.IsFinite(newRps) && newRps != 0 ? 1 / (newRpm / 60) : newRpm;
                        }
                        triggerTapIntervalArrayProperty.GetArrayElementAtIndex(index).floatValue = newTapIntervals;
                    }
                },
                onAddCallback = _ =>
                {
                    fireModesArrayProperty.arraySize++;
                },
                onRemoveCallback = _ =>
                {
                    fireModesArrayProperty.arraySize--;
                },
            };

            reorderableList.DoLayoutList();
        }

        private static void DrawEnumDisabledProperties(SerializedProperty property, int count, bool drawDisabled,
                                                       Func<int, bool> disableFunc, Action<int> beforeDisabledGroup)
        {
            GUIUtil.FoldoutPropertyField(property);

            var disabled = disableFunc.Invoke(property.enumValueIndex);
            beforeDisabledGroup.Invoke(property.enumValueIndex);
            if (!drawDisabled && disabled)
            {
                EditorGUILayout.HelpBox("Disabled properties are hidden.", MessageType.None);
                return;
            }

            using (new EditorGUI.DisabledScope(disabled))
            {
                for (int i = 0; i < count; i++)
                {
                    property.NextVisible(false);
                    GUIUtil.FoldoutPropertyField(property);
                }
            }
        }
        private struct FireModeSetting
        {
            public FireMode FireMode;
            public float MaxRoundsPerSecond;
            public float MinTriggerTapIntervals;
            public MessageType HelpBoxMessageType;
            public string HelpBoxContents;
        }
    }
}
