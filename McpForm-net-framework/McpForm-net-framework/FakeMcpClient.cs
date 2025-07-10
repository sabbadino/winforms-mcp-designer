using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WinFormsApp1;

namespace McpForm_net_framework
{
    public class FakeMcpClient
    {
        IEnumerable<ChatTool> _tools;
        Dictionary<string,MethodInfo> _handlers = new Dictionary<string, MethodInfo>();
        private readonly IEnumerable<IMcpMarker> _mcpClasses;

        public FakeMcpClient(IEnumerable<ChatTool> tools, Dictionary<string, MethodInfo> handlers, IEnumerable<IMcpMarker> mcpClasses)
        {
            _tools = tools;
            _handlers = handlers;
            _mcpClasses = mcpClasses;
        }

        internal async Task<string> CallToolAsync(string functionName, Dictionary<string, object> dictionary)
        {
            if(_handlers.TryGetValue(functionName, out var handler)) {
                var instance = _mcpClasses.SingleOrDefault(x => x.GetType() == handler.DeclaringType);   
                var aif = AIFunctionFactory.Create(handler, instance);
                return JsonSerializer.Serialize(await aif.InvokeAsync(new AIFunctionArguments(dictionary)));
            }
            throw new Exception($"Tool {functionName} not found."); 

        }

        internal IEnumerable<ChatTool> ListTools()
        {
            return _tools;
        }
    }
}
