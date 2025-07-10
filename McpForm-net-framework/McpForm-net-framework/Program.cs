using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp1;

namespace McpForm_net_framework
{
    internal class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ServiceCollection sc = new ServiceCollection();
            var modelName = "gpt-4.1";
            var openAIApiKey = System.Configuration.ConfigurationManager.AppSettings["apiKey"];
            var client = new OpenAI.OpenAIClient(openAIApiKey);
            var chatClient = client.GetChatClient(modelName);
            sc.AddSingleton(chatClient);    

            sc.AddMcpServer().WithToolsFromAssembly();
            
            // add chat tools;
            sc.AddSingleton(s=> s.GetServices<McpServerTool>().Select(t1 => t1.ToOpenAITool()));

            // add handlers;
            sc.AddSingleton(s => {
                Dictionary<string, (MethodInfo MethodInfo, Type Type)> handlers = new Dictionary<string, (MethodInfo, Type)>();
                var tx = s.GetServices<McpServerTool>();
                var tools = tx.Select(t1 => t1.ToOpenAITool());

                var toolTypes = from t in Assembly.GetExecutingAssembly().GetTypes()
                                where t.GetCustomAttribute<McpServerToolTypeAttribute>() != null
                                select t;
                Dictionary<string, (MethodInfo MethodInfo, Type Type)> _handlers = new Dictionary<string, (MethodInfo, Type)>();
                foreach (var toolType in toolTypes)
                {
                    if (toolType != null)
                    {
                        foreach (var toolMethod in toolType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                        {
                            var tm = toolMethod.GetCustomAttribute<McpServerToolAttribute>();
                            if (tm != null)
                            {
                                handlers.Add(tm.Name, (toolMethod, toolType));
                            }
                        }
                    }
                }
                return  handlers;
            });
            sc.AddSingleton<FakeMcpClient>(); 
            sc.AddSingleton<Form1>();   

            IServiceProvider services = sc.BuildServiceProvider();
            var form1= services.GetRequiredService<Form1>();   


           
            Application.Run(form1);
        }
    }
}
