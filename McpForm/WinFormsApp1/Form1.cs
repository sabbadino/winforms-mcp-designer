using Microsoft.AspNetCore.Mvc.ModelBinding;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI.Chat;
using System.Text.Json;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        internal static LLMDrivenForm? _LLMDrivenForm;
        private readonly ChatClient _chatClient;
        private readonly Dictionary<Guid, List<ChatMessage>> _AllMessages = new();
        public Form1()
        {
            InitializeComponent();
        }

        public Form1(IMcpClient mcpClient, ChatClient chatClient) : this()
        {
            _mcpClient = mcpClient;
            _chatClient = chatClient;
        }

        private Guid _conversationId = Guid.NewGuid();
        private readonly IMcpClient _mcpClient;

        private async void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Add($"U: {textBox1.Text}");
            var tools = await _mcpClient.ListToolsAsync();
            List<ChatMessage>? messages = GetOrCreateConversation(_conversationId);
            messages.Add(new UserChatMessage(textBox1.Text));
            textBox1.Text = "";
            var co = new ChatCompletionOptions();
            foreach (var tool in tools)
            {
                co.Tools.Add(tool.ToOpenAITool());
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
                            break;
                        }

                    case ChatFinishReason.ToolCalls:
                        {
                            // First, add the assistant message with tool calls to the conversation history.
                            messages.Add(new AssistantChatMessage(completion));

                            // Then, add a new tool message for each tool call that is resolved.
                            foreach (ChatToolCall toolCall in completion.ToolCalls)
                            {
                                if (tools.Select(t => t.Name).Contains(toolCall.FunctionName, StringComparer.OrdinalIgnoreCase))
                                {
                                    var toolResult = await _mcpClient.CallToolAsync(toolCall.FunctionName, JsonSerializer.Deserialize<Dictionary<string, object?>>(toolCall.FunctionArguments.ToString()));
                                    messages.Add(new ToolChatMessage(toolCall.Id,((TextContentBlock)toolResult.Content[0]).Text));
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
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _conversationId = Guid.NewGuid();
            listBox1.Items.Clear();
            textBox1.Clear();
            _LLMDrivenForm.Controls.Clear();    
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


        private List<ChatMessage> GetOrCreateConversation(Guid conversationId)
        {
            _AllMessages.TryGetValue(conversationId, out var messages);
            if (messages == null)
            {
                messages = new();
                messages.Add(new SystemChatMessage(File.ReadAllText(@".\Templates\system-message-1.md")));  
                _AllMessages.Add(conversationId, messages);
            }

            return messages;
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


}
