using Microsoft.Extensions.Configuration;
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
            var modelName = "gpt-4.1";
            var openAIApiKey = System.Configuration.ConfigurationManager.AppSettings["apiKey"];
            var client = new OpenAI.OpenAIClient(openAIApiKey);
            var chatClient = client.GetChatClient(modelName);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(chatClient));
        }
    }
}
