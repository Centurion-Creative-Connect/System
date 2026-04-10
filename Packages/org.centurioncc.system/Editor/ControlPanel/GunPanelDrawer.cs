using CenturionCC.System.Editor.Utils;
using CenturionCC.System.Gun;
using CenturionCC.System.Gun.DataStore;
using JetBrains.Annotations;
using System;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CenturionCC.System.Editor.ControlPanel
{
    public class GunPanelDrawer : IControlPanelDrawer, IDisposable
    {
        private static CachedEditor _gunManagerEditor;
        private static Transform _lastVariantsRoot;
        private static CenturionGunManagerVariantsEditor _variantsEditor;
        private bool _isGunManagerFoldout = true;
        private bool _isGunVariantDataStoresFoldout = true;

        public void Draw()
        {
            using (new GUILayout.VerticalScope("GroupBox"))
            {
                if (GUIUtil.HeaderFoldoutWithObjectSelection("GunManager", CenturionReferenceCache.GunManager, ref _isGunManagerFoldout))
                {
                    GUILayout.Space(5);
                    DrawGunManager();
                }
            }

            using (new GUILayout.VerticalScope("GroupBox"))
            {
                if (GUIUtil.HeaderFoldoutWithObjectSelection("GunVariantDataStores", _lastVariantsRoot, ref _isGunVariantDataStoresFoldout))
                {
                    GUILayout.Space(5);
                    DrawGunVariantDataStores();
                }
            }
        }

        public void Dispose()
        {
            _gunManagerEditor?.Dispose();
        }

        private static void DrawGunManager()
        {
            if (!EnsureGunManagerExists(out var gunManager))
            {
                return;
            }

            _gunManagerEditor ??= new CachedEditor(gunManager);
            _gunManagerEditor.SetTarget(gunManager);
            _gunManagerEditor.OnInspectorGUI();
        }

        private static void DrawGunVariantDataStores()
        {
            if (!EnsureGunManagerExists(out var gunManager))
            {
                return;
            }

            var so = new SerializedObject(gunManager);
            var variantsRootProperty = so.FindProperty("variantsRoot");
            if (variantsRootProperty == null)
            {
                EditorGUILayout.HelpBox("GunManagerBase reference is not CenturionGunManager.", MessageType.Info);
                return;
            }

            var variantsRoot = variantsRootProperty.objectReferenceValue as Transform;
            if (variantsRoot == null)
            {
                EditorGUILayout.HelpBox("Variants Root is not set.", MessageType.Error);
                return;
            }

            if (variantsRoot != _lastVariantsRoot || _variantsEditor == null)
            {
                _variantsEditor?.Dispose();
                _variantsEditor = new CenturionGunManagerVariantsEditor(variantsRoot);
                _lastVariantsRoot = variantsRoot;
            }


            _variantsEditor.Draw();
        }

        private static bool EnsureGunManagerExists(out GunManagerBase gunManager)
        {
            gunManager = CenturionReferenceCache.GunManager;
            if (!gunManager)
            {
                EditorGUILayout.HelpBox("GunManager is not in the scene!", MessageType.Error);
                if (GUILayout.Button("Generate GunManager Object"))
                    CenturionSampleFactory.Create(CenturionSampleFactory.ObjectType.GunManager, SceneManager.GetActiveScene());

                return false;
            }

            return true;
        }

        private class CenturionGunManagerVariantsEditor : IDisposable
        {

            private VariantDataEditor[] _variantEditors;

            public CenturionGunManagerVariantsEditor(Transform variantsRoot)
            {
                LoadVariants(variantsRoot);
            }

            public Transform VariantsRoot { get; private set; }

            [ItemCanBeNull]
            public GunVariantDataStore[] Variants { get; private set; }

            public void Dispose()
            {
                if (_variantEditors != null)
                {
                    foreach (var editor in _variantEditors)
                    {
                        editor?.Dispose();
                    }
                }
            }

            public void LoadVariants(Transform variantsRoot)
            {
                if (variantsRoot == null) throw new ArgumentNullException(nameof(variantsRoot));
                VariantsRoot = variantsRoot;

                Debug.Log($"VariantsRoot: {VariantsRoot.name}");

                // just to make behaviour the same as Udon, we don't use GetComponentsInChildren, but use GetComponent by all child instead.
                var variants = new GunVariantDataStore[VariantsRoot.childCount];
                for (var i = 0; i < VariantsRoot.childCount; i++)
                {
                    var child = VariantsRoot.GetChild(i);
                    variants[i] = child.GetComponent<GunVariantDataStore>();
                }

                Variants = variants;

                if (_variantEditors != null)
                {
                    foreach (var editor in _variantEditors)
                    {
                        editor?.Dispose();
                    }
                }

                _variantEditors = new VariantDataEditor[Variants.Length];
                for (var i = 0; i < Variants.Length; i++)
                {
                    _variantEditors[i] = new VariantDataEditor(Variants[i]);
                }
            }

            public void Draw()
            {
                for (var i = 0; i < Variants.Length; i++)
                {
                    _variantEditors[i].Draw();
                }
            }
            private class VariantDataEditor : IDisposable
            {
                private readonly GunVariantDataStore _variantData;
                private CachedEditor _editor;
                private bool _isFoldout;

                public VariantDataEditor(GunVariantDataStore variantData)
                {
                    _variantData = variantData;
                }

                public void Dispose()
                {
                    _editor?.Dispose();
                }

                public void Draw()
                {
                    if (_variantData == null)
                    {
                        GUIUtil.Header("Null VariantData", false);
                        return;
                    }

                    using (new GUILayout.VerticalScope(new GUIStyle("HelpBox") { padding = new RectOffset(10, 10, 2, 2) }))
                    {
                        if (GUIUtil.HeaderFoldoutWithObjectSelection($"{_variantData.UniqueId:000}: {_variantData.WeaponName}", _variantData, ref _isFoldout))
                        {
                            UdonSharpGUI.DrawUILine();
                            _editor ??= new CachedEditor(_variantData);
                            _editor.SetTarget(_variantData);
                            _editor.OnInspectorGUI();
                        }
                    }
                }
            }
        }
    }
}
