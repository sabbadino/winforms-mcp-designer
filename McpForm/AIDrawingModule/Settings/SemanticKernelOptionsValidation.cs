using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AIDrawingModule.Settings
{

    public class SemanticKernelOptionsValidation : IValidateOptions<AIDrawingModuleOptions>
    {
        public ValidateOptionsResult Validate(string? name, AIDrawingModuleOptions? aIDrawingModuleOptions)
        {
            if (aIDrawingModuleOptions is null)
            {
                var failureReason = "aIDrawingModuleOptions is null";
                return ValidateOptionsResult.Fail(failureReason);
            }
            if (string.IsNullOrWhiteSpace(aIDrawingModuleOptions.KernelName))
            {
                var failureReason = "aIDrawingModuleOptions.TestGenerationKernelName";
                return ValidateOptionsResult.Fail(failureReason);
            }
            if (string.IsNullOrWhiteSpace(aIDrawingModuleOptions.KernelName))
            {
                var failureReason = "aIDrawingModuleOptions.TestRefactorKernelName";
                return ValidateOptionsResult.Fail(failureReason);
            }
            if (aIDrawingModuleOptions.SemanticKernelsSettings == null)
            {
                var failureReason = "SemanticKernelSettings is null";
                return ValidateOptionsResult.Fail(failureReason);
            }
            var kernelsSettings = aIDrawingModuleOptions?.SemanticKernelsSettings;
            if (kernelsSettings!.KernelSettings.Count == 0)
            {
                var failureReason = "KernelSettings.Count==0";
                return ValidateOptionsResult.Fail(failureReason);
            }
           
            var duplicates = kernelsSettings!.KernelSettings.GroupBy(x => x.Name)
             .Where(g => g.Count() > 1)
             .Select(g => g.Key);

            if (duplicates.Any())
            {
                var failureReason = $"There are at least two kernels settings in the configuration provided that have the same name ({string.Join(',',duplicates)})";
                return ValidateOptionsResult.Fail(failureReason);
            }

            foreach (var kernelSettingsWithIndex in kernelsSettings.KernelSettings.Select((kernel, kernelIndex) => new { kernel, kernelIndex }))
            {
                if (kernelSettingsWithIndex.kernel == null)
                {
                    var failureReason = $"kernelSettings with index {kernelSettingsWithIndex.kernelIndex} is null";
                    return ValidateOptionsResult.Fail(failureReason);
                }

                if (kernelSettingsWithIndex.kernel.Model == null)
                {
                    var failureReason = $"kernelSettings.Model with index {kernelSettingsWithIndex.kernelIndex} is null";
                    return ValidateOptionsResult.Fail(failureReason);
                }
                if (kernelSettingsWithIndex.kernel.Model.Category == ModelCategory.None)
                {
                    var failureReason = $"model.Category == ModelCategory.None kernelIndex {kernelSettingsWithIndex.kernelIndex}";
                    return ValidateOptionsResult.Fail(failureReason);
                }
                if (string.IsNullOrWhiteSpace(kernelSettingsWithIndex.kernel.Model.DeploymentOrModelName))
                {
                    var failureReason = $"model.DeploymentName IsNullOrWhiteSpace kernelIndex {kernelSettingsWithIndex.kernelIndex}";
                    return ValidateOptionsResult.Fail(failureReason);
                }
                if (string.IsNullOrWhiteSpace(kernelSettingsWithIndex.kernel.Model.Url) && kernelSettingsWithIndex.kernel.Model.RequiresUrl)
                {
                    var failureReason = $"model.Url IsNullOrWhiteSpace for kernelIndex {kernelSettingsWithIndex.kernelIndex}";
                    return ValidateOptionsResult.Fail(failureReason);
                }
                if (string.IsNullOrWhiteSpace(kernelSettingsWithIndex.kernel.Model.ApiKeyName))
                {
                    var failureReason = $"model.ApiKeyName IsNullOrWhiteSpace for kernelIndex {kernelSettingsWithIndex.kernelIndex}";
                    return ValidateOptionsResult.Fail(failureReason);
                }
                else
                {
                    if (!kernelsSettings.ApiKeys.TryGetValue(kernelSettingsWithIndex.kernel.Model.ApiKeyName, out var _))
                    {
                        var failureReason = $"Could not find in ApiKeys dictionary a key named {kernelSettingsWithIndex.kernel.Model.ApiKeyName}";
                        return ValidateOptionsResult.Fail(failureReason);
                    }
                }
            }
            return ValidateOptionsResult.Success;
        }
    }
}
