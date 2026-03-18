using CenturionCC.System.Editor.Utils;
using UnityEditor;
namespace CenturionCC.System.Editor.Validation
{
    public class LayerSettingsValidator : IValidator
    {
        public void Validate(ValidationTarget target)
        {
            if (ConfigureLayers.IsConfigured())
            {
                return;
            }

            target.Collector.AddValidationInfo(new ValidationInfo
            {
                AutoFix = ConfigureLayers.SetupLayers,
                Message = "Layers are not configured for CenturionSystem.",
                MessageType = MessageType.Error,
                IsValid = false,
            });
        }
    }
}
