using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;


namespace AIDrawingModule.Settings
{
    public class AIDrawingModuleOptions 
    {
        public string StepsLanguage { get; init; } = "";
        public string RunTestOptions { get; init; } ="";
        public string KernelName { get; init; } = "";
        public SemanticKernelsSettings? SemanticKernelsSettings { get; init; }
        public int ScriptFixRetries { get; init; } = 3;


    }
    public class SemanticKernelsSettings
    {
        

        public List<KernelSettings> KernelSettings { get; init; } = new();

        public Dictionary<string, string> ApiKeys { get; init; } = new();
        public LogLevel? LogLevel { get; init; }
    }
    public class KernelSettings
    {
        public List<string> Plugins { get; init; } = new();
        public string SystemMessageName { get; init; } = "";
      
        public string Name { get; init; } = "";
        public int Temperature { get; init; } = 0;
        public Model? Model { get; init; }

    
        public IList<string> Arguments { get; init; } = new List<string>();

        public OpenAISpecificSettings? OpenAISpecificSettings { get; init; }
    }

    public class OpenAISpecificSettings
    {
        public string ReasoningEffort { get; init; } = "low"; // - String values: "minimal", "low", "medium", "high";
    }
    public class Model
    {
        public bool RequiresUrl { get; init; } = true;

        public ModelCategory? Category { get; init; }

        public string DeploymentOrModelName { get; init; } = "";

        public string Url { get; init; } = "";

        public string ApiKeyName { get; init; } = "";
    }

    public enum ModelCategory
    {
        None,
        OpenAi,
        AzureOpenAi
    }
}
