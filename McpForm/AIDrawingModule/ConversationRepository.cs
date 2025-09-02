using AIDrawingModule.Settings;
using AIDrawingModuleAbstractions.IocConventions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIDrawingModule
{
        public interface IConversationRepository
    {
        Task<ChatHistory?> GetConversationByIdAsync(string Id);

        Task PersistConversation(string Id, ChatHistory chatHistory);
    }
    public class ConversationRepository : IConversationRepository,ISingletonScope
    {
        JsonSerializerOptions  _jsonSerializerOptions = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
        private string _path;
        public ConversationRepository() 
        {
            _path = Path.GetTempPath();
        }

        public async Task<ChatHistory?> GetConversationByIdAsync(string Id)
        {
            if(File.Exists(Path.Combine(_path,$"{Id}.json")))
            {
                var content = await File.ReadAllTextAsync(Path.Combine(_path, $"{Id}.json"));
                var chatHistory = JsonSerializer.Deserialize<ChatHistory>(content, _jsonSerializerOptions);
                return chatHistory;
            }   
            return null;
        }
        public async Task PersistConversation(string Id,ChatHistory chatHistory)
        {
            var file = File.Exists(Path.Combine(_path, $"{Id}.json"));
            JsonSerializer.Serialize(chatHistory, _jsonSerializerOptions);
        }
    }
}
