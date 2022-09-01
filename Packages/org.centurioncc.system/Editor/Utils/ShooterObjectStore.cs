using System;
using System.Collections.Generic;
using CenturionCC.System.Player;
using UdonSharp;
using UnityEngine.SceneManagement;

namespace CenturionCC.System.Editor.Utils
{
    public static class ShooterObjectStore
    {
        private static GameManager _cachedGameManager;
        private static PlayerManager _cachedPlayerManager;
        public static GameManager GameManager
        {
            get
            {
                if (!_cachedGameManager)
                {
                    _cachedGameManager = FindUdonSharpComponent<GameManager>();
                    if (!_cachedGameManager)
                        throw new NullReferenceException(
                            "GameManager not found in active scene. consider creating one");
                }

                return _cachedGameManager;
            }
        }

        public static PlayerManager PlayerManager
        {
            get
            {
                if (!_cachedPlayerManager)
                {
                    _cachedPlayerManager = FindUdonSharpComponent<PlayerManager>();
                    if (!_cachedPlayerManager)
                        throw new NullReferenceException(
                            "PlayerManager not found in active scene. consider creating one");
                }

                return _cachedPlayerManager;
            }
        }

        public static T[] FindUdonSharpComponents<T>() where T : UdonSharpBehaviour
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            var components = new List<T>();
            foreach (var root in roots) components.AddRange(root.GetComponents<T>());

            return components.ToArray();
        }

        public static T FindUdonSharpComponent<T>() where T : UdonSharpBehaviour
        {
            var result = FindUdonSharpComponents<T>();
            return result.Length != 0 ? result[0] : null;
        }
    }
}