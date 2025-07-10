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
    internal class FakeMcpClient
    {
        IEnumerable<ChatTool> _tools;
        Dictionary<string,(MethodInfo MethodInfo, Type Type)> _handlers = new Dictionary<string, (MethodInfo, Type)>(); 
        internal FakeMcpClient() {
            ServiceCollection sc = new ServiceCollection();
            sc.AddMcpServer().WithToolsFromAssembly();
            IServiceProvider services = sc.BuildServiceProvider();
            var tx = services.GetServices<McpServerTool>();
            _tools = tx.Select(t1=> t1.ToOpenAITool());
            var toolTypes = from t in Assembly.GetExecutingAssembly().GetTypes()
                               where t.GetCustomAttribute<McpServerToolTypeAttribute>() != null
                               select t;
            foreach (var toolType in toolTypes)
            {
                if (toolType != null)
                {
                    foreach (var toolMethod in toolType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                    {
                        var tm = toolMethod.GetCustomAttribute<McpServerToolAttribute>();
                        if (tm != null)
                        {
                            _handlers.Add(tm.Name,(toolMethod, toolType));
                        }
                    }
                }
            }
        }

        internal async Task<string> CallToolAsync(string functionName, Dictionary<string, object> dictionary)
        {
            if(_handlers.TryGetValue(functionName, out var handler)) {
                var obj = Activator.CreateInstance(handler.Type);
                var aif = AIFunctionFactory.Create(handler.MethodInfo, obj);
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
