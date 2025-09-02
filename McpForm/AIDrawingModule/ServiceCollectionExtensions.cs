using AIDrawingModuleAbstractions.IocConventions;

using AIDrawingModule.Settings;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;


namespace AIDrawingModule
{


    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Elasticsearch GenerativeAI Functionalities to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="generativeAIOptionsAction">The action to configure GenerativeAI options.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddAIModuleOptions(this IServiceCollection services,
            Action<AIDrawingModuleOptions> playWrightTestGeneratorOptionsAction)
        {
            var playWrightTestGeneratorOptions = new AIDrawingModuleOptions();
            playWrightTestGeneratorOptionsAction.Invoke(playWrightTestGeneratorOptions);
            return services.AddAIModuleOptions(playWrightTestGeneratorOptions);    

        }

        /// <summary>
        /// Adds Elasticsearch GenerativeAi Functionalities to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="aIDrawingModuleOptions">The GenerativeAi options.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddAIModuleOptions(this IServiceCollection services,
            AIDrawingModuleOptions? aIDrawingModuleOptions)
        {
                  ArgumentNullException.ThrowIfNull(aIDrawingModuleOptions);    
            var validator = new SemanticKernelOptionsValidation();
            // I want to trigger validation during setup
            // i will not even inject GenerativeAiOptions in the IOC 
            var validationResult = validator.Validate(null, aIDrawingModuleOptions);
            if (validationResult.Failed)
            {
                throw new SemanticKernelConfigurationException($"SemanticKernelOptions validation failed: {string.Join(',', validationResult.Failures)}");
            }

            var kernelsSettings = aIDrawingModuleOptions.SemanticKernelsSettings;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            // checked for null by semanticKernelOptionsValidation.Validate
            var allPlugins = kernelsSettings.KernelSettings.SelectMany(k => k.Plugins).Distinct();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            foreach (var namespaceQualifiedClassName in allPlugins)
            {
                // build all plugins of all kernels using the global IOC container
                // to check life time , if it fits for rety logic for retry logic
                var pluginName = namespaceQualifiedClassName.Split('.').Last();
                services.AddKeyedTransient(pluginName, (serviceProvider, _) =>
                {
                    var type = Type.GetType(namespaceQualifiedClassName);
                    ArgumentNullException.ThrowIfNull(type, $"Class {namespaceQualifiedClassName} could not be found");
                    return KernelPluginFactory.CreateFromType(type, pluginName, serviceProvider);
                });
            }
         
           
            foreach (var kernelSetting in kernelsSettings.KernelSettings.Index())
            {
                services.AddTransient(globalServiceProvider =>
                {
                    // create kernel 
                    var skBuilder = Kernel.CreateBuilder();
                    ConfigureKernel(skBuilder, kernelsSettings, kernelSetting.Item);
                    if (kernelsSettings.LogLevel != null)
                    {
                        skBuilder.Services.AddLogging(l => l.SetMinimumLevel(kernelsSettings.LogLevel.Value).AddConsole());
                    }
                    var kernel = skBuilder.Build();

                    // register internal the plugin for this kernel 
                    RegisterKernelPlugins(globalServiceProvider, kernel, kernelSetting.Item.Plugins);

                    
                    return new KernelWrapper { KernelSettings = kernelSetting.Item, Kernel = kernel };
                });
            }
            services.RegisterByConvention<AIDrawingModuleOptions>();
            services.AddSingleton(Options.Create(aIDrawingModuleOptions));
            return services;
        }

        private static void ConfigureKernel(IKernelBuilder skBuilder, SemanticKernelsSettings semanticKernelsSettings, KernelSettings kernelSettings)
        {
            var model = kernelSettings.Model;
            ArgumentNullException.ThrowIfNull(model);
            string deploymentOrModelName = model.DeploymentOrModelName;
            string? url = model.Url;
            string apiKeyName = model.ApiKeyName;
            var category = model.Category;
            if (!semanticKernelsSettings.ApiKeys.TryGetValue(apiKeyName, out var apiKeyValue))
            {
                throw new Exception($"Could not find key {apiKeyName}");
            }
            if (string.IsNullOrEmpty(apiKeyValue))
            {
                throw new Exception($"value for key {apiKeyName} was found but is null or empty");
            }
            if (category == ModelCategory.AzureOpenAi)
            {
                skBuilder.AddAzureOpenAIChatCompletion(deploymentOrModelName, url, apiKeyValue);
            }
            else if (category == ModelCategory.OpenAi)
            {
                skBuilder.AddOpenAIChatCompletion(deploymentOrModelName, apiKeyValue);
            }
            else 
            {
                 throw new SemanticKernelConfigurationException($"ModelCategory {category} is not supported");  
            }


        }

  
        private static void RegisterKernelPlugins(IServiceProvider globalServiceProvider, Kernel kernel, IEnumerable<string> plugins)
        {
            foreach (var namespaceQualifiedClassName in plugins)
            {
                var pluginName = namespaceQualifiedClassName.Split('.').Last();
                var plugin = globalServiceProvider.GetRequiredKeyedService<KernelPlugin>(pluginName);
                ArgumentNullException.ThrowIfNull(plugin, $"Plugin {pluginName} could not be cast to KernelPlugin");
                kernel.Plugins.Add(plugin);
            }
        }
    }


}
