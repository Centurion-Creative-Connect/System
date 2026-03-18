using CenturionCC.System.Editor.Utils;
using DerpyNewbie.Common.Role;
using UnityEditor;
namespace CenturionCC.System.Editor.Validation
{
    public class RoleManagerValidator : IValidator
    {
        public void Validate(ValidationTarget target)
        {
            CenturionSystemReferenceCache.TargetScene = target.Scene;
            var roleProvider = CenturionSystemReferenceCache.RoleProvider;
            if (roleProvider == null) return;

            var roleManager = roleProvider as RoleManager;
            if (roleManager == null) return;

            var so = new SerializedObject(roleManager);
            var playersProperty = so.FindProperty("players");
            if (playersProperty == null) return;

            var playerCount = playersProperty.arraySize;
            if (playerCount == 0)
            {
                target.Collector.AddValidationInfo(new ValidationInfo
                {
                    Message = "No players are registered in the RoleManager. You are not guaranteed to have permission to manage CenturionSystem.",
                    MessageType = MessageType.Warning,
                    IsValid = true,
                    TargetObject = roleManager,
                });
                return;
            }

            var examplePlayerCount = 0;
            for (var i = 0; i < playerCount; i++)
            {
                var rolePlayerData = playersProperty.GetArrayElementAtIndex(0).objectReferenceValue as RolePlayerData;
                if (rolePlayerData != null && rolePlayerData.DisplayName is not ("ExamplePlayer" or "DerpyNewbie")) continue;
                examplePlayerCount++;
            }

            if (examplePlayerCount != playerCount) return;

            target.Collector.AddValidationInfo(new ValidationInfo()
            {
                Message = "The only player registered in the RoleManager is the example player(s). You are not guaranteed to have permission to manage CenturionSystem.",
                MessageType = MessageType.Warning,
                IsValid = true,
                TargetObject = roleManager,
            });
        }
    }
}
