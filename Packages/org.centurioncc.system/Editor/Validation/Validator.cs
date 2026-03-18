using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.SceneManagement;
namespace CenturionCC.System.Editor.Validation
{
    public struct ValidationInfo
    {
        public bool IsValid;
        public string Message;
        public MessageType MessageType;

        [CanBeNull]
        public Action AutoFix;
        [CanBeNull]
        public UnityEngine.Object TargetObject;
    }

    public struct ValidationTarget
    {
        public Scene Scene;
        public IValidationCollector Collector;
    }

    public interface IValidationCollector
    {
        public void AddValidationInfo(ValidationInfo validationInfo);
    }

    public interface IValidator
    {
        public void Validate(ValidationTarget target);
    }

    public static class Validator
    {

        private static readonly List<ValidationInfo> LastValidationResult = new List<ValidationInfo>();

        public static IReadOnlyList<ValidationInfo> PerformValidation()
        {
            LastValidationResult.Clear();

            var validationTarget = new ValidationTarget { Collector = new ValidationCollector(), Scene = SceneManager.GetActiveScene() };
            var validators = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => typeof(IValidator).IsAssignableFrom(t));

            foreach (var validator in validators)
            {
                validator.GetMethod("Validate")?.Invoke(Activator.CreateInstance(validator), new object[] { validationTarget });
            }

            return LastValidationResult;
        }

        public static IReadOnlyList<ValidationInfo> GetLastValidationResult()
        {
            return LastValidationResult;
        }
        private class ValidationCollector : IValidationCollector
        {
            public void AddValidationInfo(ValidationInfo validationInfo)
            {
                LastValidationResult.Add(validationInfo);
            }
        }
    }
}
