using AIDrawingModule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AIDrawingModuleAbstractions.IocConventions; 


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

            var builder = Host.CreateApplicationBuilder(args);
            builder.Configuration
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                 .AddUserSecrets<Form1>()
                 .AddEnvironmentVariables()
                 .AddCommandLine(Environment.GetCommandLineArgs());
            builder.Services.AddAIModuleOptions(options => builder.Configuration.Bind("AIDrawingModuleOptions", options));
            builder.Services.RegisterByConvention<CommandExecutor>();
            var app = builder.Build();

           




            var drawerChatService = app.Services.GetRequiredService<IDrawerChatService>();
            var conf = app.Services.GetRequiredService<IConfiguration>();
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1(drawerChatService, conf));

            await app.StopAsync();
        }
    }
}