// File: OpenAIChatLibrary/AgentManager.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;

namespace OpenAIChatLibrary
{
    public interface IAgentManager
    {
        Agent CreateAgent(string name, string model, string systemPrompt, string? overrideApiKey = null);
        Agent GetAgent(string name);
        IReadOnlyList<Agent> ListAgents();
        Task<string> SendMessageAsync(string agentName, string userMessage);
        Task<T?> SendMessageAndParseJsonAsync<T>(string agentName, string userMessage) where T : class;
    }

    public sealed class AgentManager : IAgentManager
    {
        private readonly Dictionary<string, Agent> _agents = new(StringComparer.OrdinalIgnoreCase);
        private readonly IOpenAIClient _client;

        public AgentManager(IOpenAIClient client)
        {
            _client = client;
        }

        public Agent CreateAgent(string name, string model, string systemPrompt, string? overrideApiKey = null)
        {
            if (_agents.ContainsKey(name))
            {
                throw new InvalidOperationException($"An agent with the name '{name}' already exists.");
            }

            var agent = new Agent(name, model, systemPrompt, overrideApiKey);
            _agents[name] = agent;
            return agent;
        }

        public Agent GetAgent(string name)
        {
            if (!_agents.TryGetValue(name, out var agent))
            {
                throw new KeyNotFoundException($"No agent found with name '{name}'.");
            }
            return agent;
        }

        public IReadOnlyList<Agent> ListAgents()
        {
            return new List<Agent>(_agents.Values);
        }

        public async Task<string> SendMessageAsync(string agentName, string userMessage)
        {
            var agent = GetAgent(agentName);
            agent.AddUserMessage(userMessage);
            var response = await _client.GetChatCompletionAsync(agent);
            agent.AddAssistantMessage(response);
            return response;
        }

        public async Task<T?> SendMessageAndParseJsonAsync<T>(string agentName, string userMessage) where T : class
        {
            var agent = GetAgent(agentName);
            agent.AddUserMessage(userMessage);
            var response = await _client.GetChatCompletionAsync(agent);
            agent.AddAssistantMessage(response);
            
            var cleanedResponse = RemoveJsonCodeFences(response);
            try
            {
                var obj = JsonSerializer.Deserialize<T>(cleanedResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return obj;
            }
            catch
            {
                return null;
            }
        }

        private static string RemoveJsonCodeFences(string input)
        {
            const string startMarker = "```json";
            const string endMarker = "```";
            var trimmed = input.Trim();
            if (trimmed.StartsWith(startMarker) && trimmed.EndsWith(endMarker))
            {
                var inner = trimmed.Substring(startMarker.Length, trimmed.Length - startMarker.Length - endMarker.Length);
                return inner.Trim();
            }
            return input;
        }
    }
}
