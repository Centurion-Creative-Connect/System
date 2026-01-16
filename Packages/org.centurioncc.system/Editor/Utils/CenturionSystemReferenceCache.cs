using CenturionCC.System.Gun;
using System;
using System.Collections.Generic;
using CenturionCC.System.Player;
using UdonSharp;
using UnityEngine.SceneManagement;

namespace CenturionCC.System.Editor.Utils
{
    public static class CenturionSystemReferenceCache
    {
        private static CenturionSystem _cachedCenturionSystem;
        private static PlayerManagerBase _cachedPlayerManager;
        private static GunManagerBase _cachedGunManager;

        public static CenturionSystem CenturionSystem
        {
            get
            {
                if (!_cachedCenturionSystem)
                {
                    _cachedCenturionSystem = FindUdonSharpComponent<CenturionSystem>();
                    if (!_cachedCenturionSystem)
                        return null;
                }

                return _cachedCenturionSystem;
            }
        }

        public static PlayerManagerBase PlayerManager
        {
            get
            {
                if (!_cachedPlayerManager)
                {
                    _cachedPlayerManager = FindUdonSharpComponent<PlayerManagerBase>();
                    if (!_cachedPlayerManager)
                        return null;
                }

                return _cachedPlayerManager;
            }
        }

        public static GunManagerBase GunManager
        {
            get
            {
                if (!_cachedGunManager)
                {
                    _cachedGunManager = FindUdonSharpComponent<GunManagerBase>();
                    if (!_cachedGunManager)
                        return null;
                }

                return _cachedGunManager;
            }
        }

        public static T[] FindUdonSharpComponents<T>() where T : UdonSharpBehaviour
        {
            var scene = SceneManager.GetActiveScene();
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
