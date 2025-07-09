using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI.Chat;

namespace WinFormsApp1
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static async Task Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);
            builder.Services
            .AddMcpServer().WithHttpTransport(o => /* required for open ai mco calls */ o.Stateless = true)
            .WithToolsFromAssembly();
            builder.Services.AddHttpClient();

            var transport = new SseClientTransport(new SseClientTransportOptions { Endpoint = new Uri($"{builder.Configuration["mcp-server"]}"), TransportMode = HttpTransportMode.StreamableHttp });
            builder.Services.AddSingleton((serviceProvider) =>
            {
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var mcpClient = McpClientFactory.CreateAsync(transport, new McpClientOptions
                {
                }, loggerFactory).Result;
                return mcpClient;
            });


            var modelName = "gpt-4.1";
            var openAIApiKey = builder.Configuration["open-ai-api-key"];
            var client = new OpenAI.OpenAIClient(openAIApiKey);
            var chatClient = client.GetChatClient(modelName);
            builder.Services.AddSingleton(chatClient);


            var app = builder.Build();

            app.MapMcp("mcp");
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(() =>
            {
                // This is a workaround to ensure the MCP server starts before the WinForms application.
                // The MCP server will run in the background.
                app.Run();
            });




#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            var mcpClient = app.Services.GetRequiredService<IMcpClient>();  
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1(mcpClient, chatClient));

            await app.StopAsync();
        }
    }
}