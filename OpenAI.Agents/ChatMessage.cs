// File: OpenAIChatLibrary/ChatMessage.cs
namespace OpenAIChatLibrary
{
    // Represents a single message in a chat conversation
    public sealed class ChatMessage
    {
        public string Role { get; }
        public string Content { get; }

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }
}
