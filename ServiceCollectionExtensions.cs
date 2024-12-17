//OpenAIChatLibrary/ServiceCollectionExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;

namespace OpenAIChatLibrary
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOpenAIAgents(this IServiceCollection services)
        {
            
            services.AddSingleton<OpenAIClientSettings>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var apiKey = config["OpenAI:ApiKey"];

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    throw new InvalidOperationException("OpenAI API key not configured. Please set OpenAI:ApiKey in configuration.");
                }

                return new OpenAIClientSettings(apiKey);
            });

            services.AddHttpClient<IOpenAIClient, OpenAIClient>();
            services.AddTransient<IAgentManager, AgentManager>();

            return services;
        }
    }
}
