using CenturionCC.System.Editor.Utils;
using UnityEditor;
namespace CenturionCC.System.Editor.Validation
{
    public class ReferencesValidator : IValidator
    {
        public void Validate(ValidationTarget target)
        {
            CenturionReferenceCache.TargetScene = target.Scene;
            if (CenturionReferenceCache.UpdateManager == null)
            {
                target.Collector.AddValidationInfo(new ValidationInfo
                {
                    AutoFix = () => CenturionSampleFactory.Create(CenturionSampleFactory.ObjectType.UpdateManager, target.Scene),
                    Message = "UpdateManager is not in the scene!",
                    MessageType = MessageType.Error,
                    IsValid = false,
                });
            }

            if (CenturionReferenceCache.Logger == null)
            {
                target.Collector.AddValidationInfo(new ValidationInfo
                {
                    AutoFix = () => CenturionSampleFactory.Create(CenturionSampleFactory.ObjectType.Logger, target.Scene),
                    Message = "Logger is not in the scene!",
                    MessageType = MessageType.Error,
                    IsValid = false,
                });
            }

            if (CenturionReferenceCache.RoleProvider == null)
            {
                target.Collector.AddValidationInfo(new ValidationInfo()
                {
                    AutoFix = () => CenturionSampleFactory.Create(CenturionSampleFactory.ObjectType.RoleProvider, target.Scene),
                    Message = "RoleProvider is not in the scene!",
                    MessageType = MessageType.Error,
                    IsValid = false,
                });
            }

            if (CenturionReferenceCache.AudioManager == null)
            {
                target.Collector.AddValidationInfo(new ValidationInfo()
                {
                    AutoFix = () => CenturionSampleFactory.Create(CenturionSampleFactory.ObjectType.AudioManager, target.Scene),
                    Message = "AudioManager is not in the scene!",
                    MessageType = MessageType.Error,
                    IsValid = false,
                });
            }

            if (CenturionReferenceCache.CenturionSystem == null)
            {
                target.Collector.AddValidationInfo(new ValidationInfo
                {
                    AutoFix = () => CenturionSampleFactory.Create(CenturionSampleFactory.ObjectType.CenturionSystem, target.Scene),
                    Message = "CenturionSystem is not in the scene!",
                    MessageType = MessageType.Error,
                    IsValid = false,
                });
            }

            if (CenturionReferenceCache.GunManager == null)
            {
                target.Collector.AddValidationInfo(new ValidationInfo
                {
                    AutoFix = () => CenturionSampleFactory.Create(CenturionSampleFactory.ObjectType.GunManager, target.Scene),
                    Message = "GunManager is not in the scene. Gun related systems will not work!",
                    MessageType = MessageType.Warning,
                    IsValid = true,
                });
            }

            if (CenturionReferenceCache.PlayerManager == null)
            {
                target.Collector.AddValidationInfo(new ValidationInfo
                {
                    AutoFix = () => CenturionSampleFactory.Create(CenturionSampleFactory.ObjectType.PlayerManager, target.Scene),
                    Message = "PlayerManager is not in the scene. Player related systems will not work!",
                    MessageType = MessageType.Warning,
                    IsValid = true,
                });
            }
        }
    }
}
