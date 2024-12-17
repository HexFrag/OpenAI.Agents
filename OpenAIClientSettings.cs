// File: OpenAIChatLibrary/OpenAIClientSettings.cs
namespace OpenAIChatLibrary
{
    public sealed class OpenAIClientSettings
    {
        // Default is to share a single API key; can override on agent basis if needed.
        public string? DefaultApiKey { get; init; }
        // Optionally allow a separate API base URL, defaults to official OpenAI endpoint
        public string ApiUrl { get; init; } = "https://api.openai.com/v1/chat/completions";
        public TimeSpan HttpTimeout { get; init; } = TimeSpan.FromSeconds(600);
        public int SelfRepairLimit { get; init; } = 3;

        public OpenAIClientSettings(string? defaultApiKey)
        {
            DefaultApiKey = defaultApiKey;
        }
    }
}
