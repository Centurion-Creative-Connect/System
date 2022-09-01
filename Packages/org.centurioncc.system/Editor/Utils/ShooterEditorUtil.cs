using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using VRC.Udon;

namespace CenturionCC.System.Editor.Utils
{
    public static class ShooterEditorUtil
    {
        public static float DeltaTime
        {
            get
            {
                if (!HasInitialized) Initialize();
                return _deltaTime;
            }
        }
        public static bool HasInitialized = false;

        private static float _lastTime = 0F;
        private static float _currentTime = 0F;
        private static float _deltaTime = 0F;

        private static void Update()
        {
            _lastTime = _currentTime;
            _currentTime = GetTime();

            _deltaTime = _currentTime - _lastTime;
        }

        public static void EnsureInitialized()
        {
            if (!HasInitialized) Initialize();
        }

        public static void Initialize()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            _lastTime = GetTime();
            HasInitialized = true;
        }

        private static float GetTime()
        {
            return (float) EditorApplication.timeSinceStartup;
        }

        public static void AssignPersistent(UnityEventBase eventBase, UnityAction<string> target, string methodName)
        {
            UnityEventTools.AddStringPersistentListener(eventBase, target, methodName);
        }

        public static void RemovePersistent(UnityEventBase eventBase)
        {
            for (int i = 0; i < eventBase.GetPersistentEventCount(); i++)
            {
                UnityEventTools.RemovePersistentListener(eventBase, i);
            }
        }

        // TODO: should check more further
        public static bool HasUdonEventAtOnClick(UnityEventBase eventBase)
        {
            if (eventBase.GetPersistentEventCount() == 0) return false;

            for (int i = 0; i < eventBase.GetPersistentEventCount(); i++)
            {
                Object obj = eventBase.GetPersistentTarget(i);
                if (obj as UdonBehaviour) return true;
            }

            return false;
        }

        public static IEnumerable<GameObject> CreateObjects(Object origin, int amount, Transform inWhere = null)
        {
            Debug.Log($"Generating {amount} of GameObjects");
            GameObject[] objs = new GameObject[amount];
            if (inWhere == null)
                inWhere = SceneManager.GetActiveScene().GetRootGameObjects()[0].transform;

            for (int i = 0; i < amount; i++)
            {
                var obj = CreateObject(origin, inWhere);
                obj.name = origin.name + "-" + i;
                objs[i] = obj;
            }

            return objs;
        }

        public static GameObject CreateObject(Object origin, Transform inWhere = null)
        {
            bool isPrefab = PrefabUtility.GetPrefabAssetType(origin) != PrefabAssetType.NotAPrefab;

            GameObject obj;
            if (isPrefab)
                obj = (GameObject) PrefabUtility.InstantiatePrefab(origin, inWhere);
            else
                obj = (GameObject) Object.Instantiate(origin, inWhere);

            StageUtility.PlaceGameObjectInCurrentStage(obj);
            return obj;
        }
    }
}