using McpForm_net_framework;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        internal static LLMDrivenForm _LLMDrivenForm;
        private readonly ChatClient _chatClient;
        private readonly FakeMcpClient _mcpClient;
        private readonly Dictionary<Guid, List<ChatMessage>> _AllMessages = new Dictionary<Guid, List<ChatMessage>>();
        private readonly Dictionary<Guid, List<ChatMessageSerializable>> _AllMessagesSerializable= new Dictionary<Guid, List<ChatMessageSerializable>>();
        public Form1()
        {
            InitializeComponent();
        }

        public Form1(ChatClient chatClient) : this()
        {
            _chatClient = chatClient;
            _mcpClient = new FakeMcpClient();
        }

        private Guid _conversationId = Guid.NewGuid();
       
    

        private async void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Add($"U: {textBox1.Text}");
            var tools = _mcpClient.ListTools();
            (var messages,var ms)  = GetOrCreateConversation(_conversationId);
            messages.Add(new UserChatMessage(textBox1.Text));
            textBox1.Text = "";
            var co = new ChatCompletionOptions();
            foreach (var tool in tools)
            {
                co.Tools.Add(tool);
            }
            bool requiresAction;

            do
            {
                requiresAction = false;
                ChatCompletion completion = _chatClient.CompleteChat(messages, co);

                switch (completion.FinishReason)
                {
                    case ChatFinishReason.Stop:
                        {
                            // Add the assistant message to the conversation history.
                            messages.Add(new AssistantChatMessage(completion));
                            ms.Add(new ChatMessageSerializable { Role = "Assistant", Text = messages.Last().Content[0].Text });
                            break;
                        }

                    case ChatFinishReason.ToolCalls:
                        {
                            // First, add the assistant message with tool calls to the conversation history.
                            messages.Add(new AssistantChatMessage(completion));
                            ms.Add(new ChatMessageSerializable { Role = completion.Role.ToString(), Text = $"function name: {completion.ToolCalls[0].FunctionName} arguments {completion.ToolCalls[0].FunctionArguments.ToString()}" });
                            // Then, add a new tool message for each tool call that is resolved.
                            foreach (ChatToolCall toolCall in completion.ToolCalls)
                            {
                                if (tools.Select(t => t.FunctionName).Contains(toolCall.FunctionName, StringComparer.OrdinalIgnoreCase))
                                {
                                    var toolResult = await _mcpClient.CallToolAsync(toolCall.FunctionName, JsonSerializer.Deserialize<Dictionary<string, object>>(toolCall.FunctionArguments.ToString()));
                                    messages.Add(new ToolChatMessage(toolCall.Id,toolResult));
                                    ms.Add(new ChatMessageSerializable { Role = "Tool", Text = toolResult });   
                                }
                                else
                                {
                                    throw new Exception($"Tool {toolCall.FunctionName} not found");
                                }
                            }

                            requiresAction = true;
                            break;
                        }

                    case ChatFinishReason.Length:
                        throw new NotImplementedException("Incomplete model output due to MaxTokens parameter or token limit exceeded.");

                    case ChatFinishReason.ContentFilter:
                        throw new NotImplementedException("Omitted content due to a content filter flag.");

                    case ChatFinishReason.FunctionCall:
                        throw new NotImplementedException("Deprecated in favor of tool calls.");

                    default:
                        throw new NotImplementedException(completion.FinishReason.ToString());
                }
            } while (requiresAction);

            TextSplitter.SplitText(messages.Last().Content[0].Text);
            var prefix = "A: ";
            foreach (var part in TextSplitter.SplitText(messages.Last().Content[0].Text, listBox1.Width/7, listBox1.Width/7+1))
            {
                listBox1.Items.Add($"{prefix}{part}");
                prefix = "";
            }
            File.WriteAllLines(GetFileName(),  new[] { JsonSerializer.Serialize(ms,new JsonSerializerOptions { WriteIndented = true}) } );
        }

        private string GetFileName() => 
            $".\\{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}conversation-{_conversationId}.json";

        private void button2_Click(object sender, EventArgs e)
        {
            _conversationId = Guid.NewGuid();
            listBox1.Items.Clear();
            textBox1.Clear();
            _LLMDrivenForm.Controls.Clear();
            _LLMDrivenForm.Invalidate(); // Force repaint
        }



        

        private (List<ChatMessage> ChatMessages, List<ChatMessageSerializable> ChatMessageSerializable) GetOrCreateConversation(Guid conversationId)
        {
            _AllMessages.TryGetValue(conversationId, out var messages);
            _AllMessagesSerializable.TryGetValue(conversationId, out var ms);
            if (messages == null)
            {
                messages = new List<ChatMessage>();
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

        private void Form1_Load_1(object sender, EventArgs e)
        {
            _LLMDrivenForm = new LLMDrivenForm();
            _LLMDrivenForm.Show();
            _LLMDrivenForm.StartPosition = FormStartPosition.Manual;

            // Set location: (X, Y) from top-left corner of primary screen
            _LLMDrivenForm.Location = new Point(0, 0); // Example: 200px from left, 100px from top
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(800, 0); // Example: 200px from left, 100px from top    
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
