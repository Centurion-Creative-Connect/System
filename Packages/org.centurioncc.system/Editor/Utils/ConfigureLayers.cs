using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CenturionCC.System.Editor.Utils
{
    public static class ConfigureLayers
    {
        public static Dictionary<int, string> CenturionSystemLayers =>
            new Dictionary<int, string> { [29] = "GamePlayer", [30] = "GameGun", [31] = "GameProjectile" };

        [MenuItem("Centurion-Utils/Setup Layers")]
        public static void SetupLayers()
        {
            Debug.Log("Setting up layer names");
            UpdateLayers.SetLayers(CenturionSystemLayers);
            Debug.Log("Setting up collision layers");
            var environmentLayer = LayerMask.NameToLayer("Environment");
            var mirrorReflectionLayer = LayerMask.NameToLayer("MirrorReflection");
            var gamePlayerLayer = LayerMask.NameToLayer("GamePlayer");
            var gameGunLayer = LayerMask.NameToLayer("GameGun");
            var gameProjectileLayer = LayerMask.NameToLayer("GameProjectile");
            for (int i = 0; i < 32; i++)
            {
                Physics.IgnoreLayerCollision(gameProjectileLayer, i, i != environmentLayer && i != gamePlayerLayer);
                Physics.IgnoreLayerCollision(gameGunLayer, i, i != environmentLayer && i != gameGunLayer);
                Physics.IgnoreLayerCollision(gamePlayerLayer, i, i != gameProjectileLayer && i != mirrorReflectionLayer);
            }
        }
    }
}