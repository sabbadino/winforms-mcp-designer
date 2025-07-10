using ModelContextProtocol.Client;
using ModelContextProtocol.Server;
using OpenAI.Chat;
using System;
using System.Collections.Generic;

namespace WinFormsApp1
{

        public static class McpExtensions
    {
        public static IList<ChatTool> ToOpenAITools(this IList<McpServerTool> tools)
        {
            var ret = new List<ChatTool>();
            foreach (var tool in tools)
            {
                ret.Add(tool.ToOpenAITool());
            }
            return ret;
        }

        public static ChatTool ToOpenAITool(this McpServerTool tool)
        {
            return ChatTool.CreateFunctionTool(tool.ProtocolTool.Name, tool.ProtocolTool.Description, new BinaryData(tool.ProtocolTool.InputSchema ));
        }
    }
}

 
