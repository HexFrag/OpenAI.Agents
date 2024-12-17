// File: OpenAIChatLibrary/OpenAIClient.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenAIChatLibrary
{
    public interface IOpenAIClient
    {
        Task<string> GetChatCompletionAsync(Agent agent);
    }
    public sealed class OpenAIClient : IOpenAIClient
    {
        private readonly HttpClient _httpClient;
        private readonly OpenAIClientSettings _settings;        
        

        public OpenAIClient(OpenAIClientSettings settings, HttpClient httpClient)
        {
            _settings = settings;
            _httpClient = httpClient;

        }      

        private sealed class ChatCompletionRequest
        {
            public string model { get; set; } = string.Empty;
            public List<ChatRoleContent> messages { get; set; } = new();
        }

        private sealed class ChatRoleContent
        {
            public string role { get; set; } = string.Empty;
            public string content { get; set; } = string.Empty;
        }

        private sealed class ChatCompletionResponse
        {
            public string id { get; set; } = string.Empty;
            public string Object { get; set; } = string.Empty;
            public long created { get; set; }
            public List<ChatCompletionChoice> choices { get; set; } = new();
        }

        private sealed class ChatCompletionChoice
        {
            public int index { get; set; }
            public ChatMessageContent message { get; set; } = new();
            public string finish_reason { get; set; } = string.Empty;
        }

        private sealed class ChatMessageContent
        {
            public string role { get; set; } = string.Empty;
            public string content { get; set; } = string.Empty;
        }

        public async Task<string> GetChatCompletionAsync(Agent agent)
        {
            var apiKey = agent.OverrideApiKey ?? _settings.DefaultApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("No API key provided. Either set a default key or override key for this agent.");
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var messages = agent.GetPreparedMessagesForRequest();
            var request = new ChatCompletionRequest
            {
                model = agent.Model,
                messages = new List<ChatRoleContent>()
            };
            foreach (var msg in messages)
            {
                request.messages.Add(new ChatRoleContent { role = msg.Role, content = msg.Content });
            }

            using var httpContent = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(_settings.ApiUrl, httpContent).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"OpenAI API call failed with status {response.StatusCode}: {responseString}");
            }

            var completionResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseString);
            if (completionResponse == null || completionResponse.choices.Count == 0)
            {
                throw new InvalidOperationException("No response from OpenAI API.");
            }

            return completionResponse.choices[0].message.content;
        }
    }
}
