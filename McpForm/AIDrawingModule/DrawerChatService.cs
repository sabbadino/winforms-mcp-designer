using AIDrawingModule.Settings;
using AIDrawingModuleAbstractions.IocConventions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AIDrawingModule
{
    public interface IDrawerChatService
    {
        Task<string> GetResponse(string conversationId, string prompt);
    }

    public class DrawerChatService : IDrawerChatService, ISingletonScope
    {
        private readonly AIDrawingModuleOptions _options;
        private readonly IEnumerable<KernelWrapper> _kernelWrappers;
        private readonly ITemplatesProvider _templatesProvider;
        private readonly IConversationRepository _conversationRepository;
        

        public DrawerChatService(IOptions<AIDrawingModuleOptions> options, IEnumerable<KernelWrapper> kernelWrappers, 
            ITemplatesProvider templatesProvider, IConversationRepository conversationRepository)
        {
            _options = options.Value;
            _kernelWrappers = kernelWrappers;
            _templatesProvider = templatesProvider;
            _conversationRepository = conversationRepository;
            
        }
        private static PromptExecutionSettings CreatePromptExecutionSettings(KernelWrapper kernelWrapper)
        {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            return kernelWrapper.KernelSettings.Model?.Category switch
            {
                ModelCategory.AzureOpenAi => new AzureOpenAIPromptExecutionSettings
                {
                    Temperature = kernelWrapper.KernelSettings.Temperature,
                    ReasoningEffort = kernelWrapper.KernelSettings.OpenAISpecificSettings?.ReasoningEffort,
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(
                    options: new FunctionChoiceBehaviorOptions
                    {
                        AllowStrictSchemaAdherence = true,
                        RetainArgumentTypes = true,
                    })
                },
                ModelCategory.OpenAi => new OpenAIPromptExecutionSettings
                {
                    Temperature = kernelWrapper.KernelSettings.Temperature,
                    ReasoningEffort = kernelWrapper.KernelSettings.OpenAISpecificSettings?.ReasoningEffort,
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(
                    options: new FunctionChoiceBehaviorOptions
                    {
                        AllowStrictSchemaAdherence = true,
                        RetainArgumentTypes = true
                    })
                },
                _ => throw new SemanticKernelException($"Model category {kernelWrapper.KernelSettings.Model?.Category} is not supported"),
            };
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        }
        private KernelWrapper GetKernelWrapper(string kernelName)
        {
            if (string.IsNullOrWhiteSpace(kernelName))
            {
                throw new ArgumentNullException(nameof(kernelName), "Kernel name cannot be null or empty.");
            }
            var kernelWrapper = _kernelWrappers.SingleOrDefault(k => string.Equals(k.KernelSettings.Name, kernelName, StringComparison.OrdinalIgnoreCase));
            if (kernelWrapper == null)
            {
                throw new SemanticKernelException($"Kernel with name {kernelName} not found.");
            }
            return kernelWrapper;
        }

        public  async Task<string> GetResponse(string conversationId, string prompt)
        {
            var kernelWrapper = GetKernelWrapper(_options.KernelName);
            var chatClient = kernelWrapper.Kernel.GetRequiredService<IChatCompletionService>();
            var promptExecutionSettings = CreatePromptExecutionSettings(kernelWrapper);
            var chatHistory = await _conversationRepository.GetConversationByIdAsync(conversationId)?? new ChatHistory();
            if (chatHistory.Count == 0)
            {
                chatHistory.AddSystemMessage(_templatesProvider.GetSystemMessage(kernelWrapper.KernelSettings.SystemMessageName));
            }
            chatHistory.AddUserMessage(prompt); 
            var completion = await chatClient.GetChatMessageContentsAsync(chatHistory,promptExecutionSettings, kernelWrapper.Kernel);
            chatHistory.AddAssistantMessage(completion[0].Content??"");
            return completion[0].Content??"";   
        }
    }
}
