// File: OpenAIChatLibrary/Agent.cs
using System.Collections.Generic;
using System.Linq;
using TiktokenSharp;
using System;
using System.Reflection;

namespace OpenAIChatLibrary
{
    
    public sealed class Agent
    {
        public string Name { get; }
        public string Model { get; }
        public string SystemPrompt { get; private set; }
        public string? OverrideApiKey { get; }

        private readonly List<ChatMessage> _conversationHistory = new();
        
        private int _maxContextTokens;
        private int _bufferTokens;
        private TikToken enc;
        public Agent(string name, string model, string systemPrompt, string? overrideApiKey = null)
        {
            Name = name;
            Model = model;
            SystemPrompt = systemPrompt;
            OverrideApiKey = overrideApiKey;

            enc = TikToken.EncodingForModel(model);
            SetModelContextLimits(model);

            // Initialize conversation with system prompt
            _conversationHistory.Add(new ChatMessage(GetSystemRoleByModel(model), systemPrompt));
        }
        public void UpdateSystemPrompt(string newSystemPrompt, string model)
        {           
            if (_conversationHistory.Count > 0 && _conversationHistory[0].Role == GetSystemRoleByModel(model))
            {
                _conversationHistory[0] = new ChatMessage(GetSystemRoleByModel(model), newSystemPrompt);
            }
            else
            {                
                _conversationHistory.Insert(0, new ChatMessage(GetSystemRoleByModel(model), newSystemPrompt));
            }

            SystemPrompt = newSystemPrompt;
            
            if(_conversationHistory.Count > 3)
                TrimConversationIfNeeded();
        }

        private string GetSystemRoleByModel(string model)
        {
            if (model.Contains("o1"))
                return "user";
            else
                return "system";
        }

        public IReadOnlyList<ChatMessage> GetConversationHistory() => _conversationHistory.ToList();

        public void AddUserMessage(string content)
        {
            _conversationHistory.Add(new ChatMessage("user", content));
            TrimConversationIfNeeded();
        }

        public void AddAssistantMessage(string content)
        {
            _conversationHistory.Add(new ChatMessage("assistant", content));
            TrimConversationIfNeeded();
        }

        public void AddMessage(string role, string content)
        {
            _conversationHistory.Add(new ChatMessage(role, content));
            TrimConversationIfNeeded();
        }

        public void ClearConversationHistory(string model)
        {
            _conversationHistory.Clear();
            _conversationHistory.Add(new ChatMessage(GetSystemRoleByModel(model), SystemPrompt));
        }

        public IReadOnlyList<ChatMessage> GetPreparedMessagesForRequest()
        {
            // Return the full conversation as is (already trimmed as needed)
            return _conversationHistory.ToList();
        }

        private void TrimConversationIfNeeded()
        {
            // Check token counts and trim if needed
            int currentTokenCount = CountTokensForConversation(_conversationHistory);
            int maxAllowedTokens = _maxContextTokens - _bufferTokens;
            while (currentTokenCount > maxAllowedTokens && _conversationHistory.Count > 1)
            {
                // Remove the oldest message after the system prompt to save space
                // We never remove the first system prompt, so start trimming from index 1
                if (_conversationHistory.Count > 1)
                {
                    _conversationHistory.RemoveAt(1);
                    currentTokenCount = CountTokensForConversation(_conversationHistory);
                }
                else
                {
                    // If nothing left but system, break
                    break;
                }
            }
        }

        private void SetModelContextLimits(string model)
        {            
            switch (model)
            {
                case "gpt-4":
                    _maxContextTokens = 8192;
                    _bufferTokens = 800;
                    break;
                case "gpt-3.5-turbo":
                    _maxContextTokens = 4096;
                    _bufferTokens = 200;
                    break;
                // If we have some custom models mentioned in snippet "gpt-4o" or "gpt-4o-mini"
                case "gpt-4o":
                case "gpt-4o-mini":
                    _maxContextTokens = 128000;
                    _bufferTokens = 512;
                    break;
                default: //o1 etc
                    _maxContextTokens = 128000; // Very large default
                    _bufferTokens = 512;
                    break;
            }
        }

        private int CountTokensForConversation(IReadOnlyList<ChatMessage> messages)
        {
           return messages.Sum(m => CountTokens(m.Content));            
        }

        private int CountTokens(string text)
        {
            if(string.IsNullOrWhiteSpace(text))
                return 0;

            var tokens = enc.Encode(text);
            return tokens.Count();
        }

       
    }
}
