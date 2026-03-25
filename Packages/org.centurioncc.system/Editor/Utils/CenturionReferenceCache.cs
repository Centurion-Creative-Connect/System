using CenturionCC.System.Audio;
using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using CenturionCC.System.UI.HeadUI;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using DerpyNewbie.Logger;
using System.Collections.Generic;
using UdonSharp;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CenturionCC.System.Editor.Utils
{
    public static class CenturionReferenceCache
    {
        private static Scene _cachedScene;
        private static CenturionSystem _cachedCenturionSystem;
        private static UpdateManager _cachedUpdateManager;
        private static PlayerManagerBase _cachedPlayerManager;
        private static GunManagerBase _cachedGunManager;
        private static AudioManager _cachedAudioManager;
        private static HeadUIMover _cachedHeadUIMover;
        private static RoleProvider _cachedRoleProvider;
        private static PrintableBase _cachedLogger;
        public static Scene TargetScene
        {
            get
            {
                if (!_cachedScene.isLoaded) UpdateTargetScene();
                return _cachedScene;
            }
            set
            {
                _cachedScene = value;

                _cachedCenturionSystem = null;
                _cachedUpdateManager = null;
                _cachedGunManager = null;
                _cachedAudioManager = null;
                _cachedHeadUIMover = null;
                _cachedRoleProvider = null;
                _cachedLogger = null;
            }
        }

        public static CenturionSystem CenturionSystem
        {
            get
            {
                if (!_cachedCenturionSystem)
                    _cachedCenturionSystem = FindUdonSharpComponent<CenturionSystem>();

                return _cachedCenturionSystem;
            }
        }

        public static UpdateManager UpdateManager
        {
            get
            {
                if (!_cachedUpdateManager)
                    _cachedUpdateManager = FindUdonSharpComponent<UpdateManager>();

                return _cachedUpdateManager;
            }
        }

        public static PlayerManagerBase PlayerManager
        {
            get
            {
                if (!_cachedPlayerManager)
                    _cachedPlayerManager = FindUdonSharpComponent<PlayerManagerBase>();

                return _cachedPlayerManager;
            }
        }

        public static GunManagerBase GunManager
        {
            get
            {
                if (!_cachedGunManager)
                    _cachedGunManager = FindUdonSharpComponent<GunManagerBase>();

                return _cachedGunManager;
            }
        }

        public static AudioManager AudioManager
        {
            get
            {
                if (!_cachedAudioManager)
                    _cachedAudioManager = FindUdonSharpComponent<AudioManager>();

                return _cachedAudioManager;
            }
        }

        public static HeadUIMover HeadUIMover
        {
            get
            {
                if (!_cachedHeadUIMover)
                    _cachedHeadUIMover = FindUdonSharpComponent<HeadUIMover>();

                return _cachedHeadUIMover;
            }
        }

        public static RoleProvider RoleProvider
        {
            get
            {
                if (!_cachedRoleProvider)
                    _cachedRoleProvider = FindUdonSharpComponent<RoleProvider>();

                return _cachedRoleProvider;
            }
        }

        public static PrintableBase Logger
        {
            get
            {
                if (!_cachedLogger)
                    _cachedLogger = FindUdonSharpComponent<PrintableBase>();

                return _cachedLogger;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void OnInit()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            UpdateTargetScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UpdateTargetScene();
        }

        private static void UpdateTargetScene()
        {
            TargetScene = SceneManager.GetActiveScene();
        }

        public static T[] FindUdonSharpComponents<T>() where T : UdonSharpBehaviour
        {
            var scene = TargetScene;
            var roots = scene.GetRootGameObjects();
            var components = new List<T>();
            foreach (var root in roots) components.AddRange(root.GetComponentsInChildren<T>());

            return components.ToArray();
        }

        public static T FindUdonSharpComponent<T>() where T : UdonSharpBehaviour
        {
            var result = FindUdonSharpComponents<T>();
            return result.Length != 0 ? result[0] : null;
        }
    }
}
