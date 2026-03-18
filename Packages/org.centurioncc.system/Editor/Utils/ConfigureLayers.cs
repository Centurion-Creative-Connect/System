using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.PackageManagement.Core;

namespace CenturionCC.System.Editor.Utils
{
    public static class ConfigureLayers
    {
        public static Dictionary<int, string> CenturionSystemLayers =>
            new Dictionary<int, string>
                { [28] = "GamePickup", [29] = "GamePlayer", [30] = "GameGun", [31] = "GameProjectile" };

        [MenuItem("Centurion System/Setup Layers")]
        public static void SetupLayers()
        {
            Debug.Log("Setting up layer names");
            UpdateLayers.SetLayers(CenturionSystemLayers);
            Debug.Log("Setting up collision layers");
            var defaultLayer = LayerMask.NameToLayer("Default");
            var environmentLayer = LayerMask.NameToLayer("Environment");
            var walkthroughLayer = LayerMask.NameToLayer("Walkthrough");
            var mirrorReflectionLayer = LayerMask.NameToLayer("MirrorReflection");
            var gamePickupLayer = LayerMask.NameToLayer("GamePickup");
            var gamePlayerLayer = LayerMask.NameToLayer("GamePlayer");
            var gameGunLayer = LayerMask.NameToLayer("GameGun");
            var gameProjectileLayer = LayerMask.NameToLayer("GameProjectile");
            for (int i = 0; i < 32; i++)
            {
                Physics.IgnoreLayerCollision(gamePickupLayer, i,
                    i != defaultLayer && i != environmentLayer && i != gameProjectileLayer && i != gameGunLayer);

                Physics.IgnoreLayerCollision(gameProjectileLayer, i,
                    i != environmentLayer && i != gamePlayerLayer && i != walkthroughLayer && i != gamePickupLayer);

                Physics.IgnoreLayerCollision(gameGunLayer, i,
                    i != environmentLayer && i != gameGunLayer && i != walkthroughLayer && i != gamePickupLayer);

                Physics.IgnoreLayerCollision(gamePlayerLayer, i,
                    i != gameProjectileLayer && i != mirrorReflectionLayer);
            }
        }

        public static bool IsConfigured()
        {
            if (!UpdateLayers.IsCollisionLayerMatrixSetup()) return false;

            var defaultLayer = LayerMask.NameToLayer("Default");
            var environmentLayer = LayerMask.NameToLayer("Environment");
            var walkthroughLayer = LayerMask.NameToLayer("Walkthrough");
            var mirrorReflectionLayer = LayerMask.NameToLayer("MirrorReflection");
            var gamePickupLayer = CenturionSystemLayers.FindFirstKeyByValue("GamePickup");
            var gamePlayerLayer = CenturionSystemLayers.FindFirstKeyByValue("GamePlayer");
            var gameGunLayer = CenturionSystemLayers.FindFirstKeyByValue("GameGun");
            var gameProjectileLayer = CenturionSystemLayers.FindFirstKeyByValue("GameProjectile");

            for (var i = 0; i < 32; i++)
            {
                var gamePickupIntersection = i != defaultLayer && i != environmentLayer && i != gameProjectileLayer && i != gameGunLayer;
                if (Physics.GetIgnoreLayerCollision(gamePickupLayer, i) != gamePickupIntersection)
                {
                    Debug.LogError($"Collision layer {LayerMask.LayerToName(i)} vs GamePickupLayer is not configured correctly");
                    return false;
                }

                var gameProjectileIntersection = i != environmentLayer && i != gamePlayerLayer && i != walkthroughLayer && i != gamePickupLayer;
                if (Physics.GetIgnoreLayerCollision(gameProjectileLayer, i) != gameProjectileIntersection)
                {
                    Debug.LogError($"Collision layer {LayerMask.LayerToName(i)} vs GameProjectileLayer is not configured correctly");
                    return false;
                }

                var gameGunIntersection = i != environmentLayer && i != gameGunLayer && i != walkthroughLayer && i != gamePickupLayer;
                if (Physics.GetIgnoreLayerCollision(gameGunLayer, i) != gameGunIntersection)
                {
                    Debug.LogError($"Collision layer {LayerMask.LayerToName(i)} vs GameGunLayer is not configured correctly");
                    return false;
                }

                var gamePlayerIntersection = i != gameProjectileLayer && i != mirrorReflectionLayer;
                if (Physics.GetIgnoreLayerCollision(gamePlayerLayer, i) != gamePlayerIntersection)
                {
                    Debug.LogError($"Collision layer {LayerMask.LayerToName(i)} vs GamePlayerLayer is not configured correctly");
                    return false;
                }
            }

            return true;
        }
    }
}
