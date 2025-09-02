using System.IO;
using AIDrawingModuleAbstractions.IocConventions;
using Microsoft.Extensions.Caching.Memory;



namespace AIDrawingModule
{

    public interface ITemplatesProvider
    {
        string GetTemplate(string name);
        string GetSystemMessage(string name);
    }
    public class TemplatesProvider : ITemplatesProvider, ISingletonScope
    {
        private const string TemplateNamespace = "AIDrawingModule.Templates";
        public string GetSystemMessage(string name)
        {
            var template = GetTemplate(name);
            if (string.IsNullOrWhiteSpace(template))
            {
                throw new SemanticKernelException($"Could not find template {name}");
            }
            return template;   
        }
        public string GetTemplate(string name)
        {
            var resourceName = $"{TemplateNamespace}.{name}";
            using var stream = typeof(TemplatesProvider).Assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                throw new SemanticKernelException($"Could not find resource {resourceName}");
            }
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
