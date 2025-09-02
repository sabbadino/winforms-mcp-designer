using AIDrawingModule;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.Configuration;
using System.Text.Json;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        internal static LLMDrivenForm? _LLMDrivenForm;
        private readonly ChatClient _chatClient;
        private readonly IConfiguration _configuration;
        private readonly IDrawerChatService _drawerChatService;
        private readonly Dictionary<Guid, List<ChatMessage>> _AllMessages = new();
        private readonly Dictionary<Guid, List<ChatMessageSerializable>> _AllMessagesSerializable= new();
        public Form1()
        {
            InitializeComponent();
        }

        public Form1(IDrawerChatService drawerChatService, IConfiguration configuration) : this()
        {
            _drawerChatService = drawerChatService;
            _configuration = configuration;
        }

        private Guid _conversationId = Guid.NewGuid();
       
      

        private async void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Add($"U: {textBox1.Text}");
            var responseText = await _drawerChatService.GetResponse(_conversationId.ToString(),textBox1.Text);
            textBox1.Text = "";
            var prefix = "A: ";
            foreach (var part in TextSplitter.SplitText(responseText, listBox1.Width/7, listBox1.Width/7+1))
            {
                listBox1.Items.Add($"{prefix}{part}");
                prefix = "";
            }
          
        }

     
        private void button2_Click(object sender, EventArgs e)
        {
            _conversationId = Guid.NewGuid();
            listBox1.Items.Clear();
            textBox1.Clear();
            _LLMDrivenForm.Controls.Clear();
            _LLMDrivenForm.Invalidate(); // Force repaint
        }



        private void Form1_Load(object sender, EventArgs e)
        {
            _LLMDrivenForm = new LLMDrivenForm();
            _LLMDrivenForm.Show();
            _LLMDrivenForm.StartPosition = FormStartPosition.Manual;

            // Set location: (X, Y) from top-left corner of primary screen
            _LLMDrivenForm.Location = new Point(0, 0); // Example: 200px from left, 100px from top
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(800, 0); // Example: 200px from left, 100px from top    
            
        }


        private (List<ChatMessage> ChatMessages, List<ChatMessageSerializable> ChatMessageSerializable) GetOrCreateConversation(Guid conversationId)
        {
            _AllMessages.TryGetValue(conversationId, out var messages);
            _AllMessagesSerializable.TryGetValue(conversationId, out var ms);
            if (messages == null)
            {
                messages = new();
                ms = new List<ChatMessageSerializable>();
                messages.Add(new SystemChatMessage(File.ReadAllText(@".\Templates\system-message-1.md")));
                ms.Add(new ChatMessageSerializable { Role = "System", Text = messages[0].Content[0].Text });
                _AllMessagesSerializable.Add(conversationId, ms);
                _AllMessages.Add(conversationId, messages);
            }

            return (messages, ms);
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == (Keys.Return))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                button1_Click(this, new EventArgs());

                return;
            }

            base.OnKeyDown(e);
        }
    }



public class TextSplitter
    {
        public static List<string> SplitText(string text, int maxLength = 200, int minLengthToSplit = 201)
        {
            var result = new List<string>();

            if (text.Length < minLengthToSplit)
            {
                result.Add(text);
                return result;
            }

            int start = 0;

            while (start < text.Length)
            {
                int length = Math.Min(maxLength, text.Length - start);
                int end = start + length;

                // Look backwards for a space
                int splitAt = end;

                if (end < text.Length)
                {
                    int lastSpace = text.LastIndexOf(' ', end - 1, length);
                    if (lastSpace > start)
                        splitAt = lastSpace;
                }

                if (splitAt == start) // no space found, force split at maxLength
                    splitAt = end;

                result.Add(text.Substring(start, splitAt - start).Trim());
                start = splitAt + 1; // skip space
            }

            return result;
        }
    }

    public class ChatMessageSerializable
    {
        public string Role { get; set; }
        public string Text { get; set; }

    }

}
