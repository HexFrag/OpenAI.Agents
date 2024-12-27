# OpenAI.Agents

This project provides a straightforward way to define and manage role-based AI agents (such as a Project Manager, Developer, Reviewer, and QA engineer) using OpenAI’s Chat API (or compatible APIs). Each agent has its own "system prompt" that establishes its role. The solution is built with .NET 8, making use of `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Configuration`, `TiktokenSharp` (for token counting), and `HttpClient` to communicate with OpenAI.

## Requirements

- .NET 8 SDK or newer
- An OpenAI API key to call the OpenAI Chat Completion endpoint
- A valid `appsettings.json` file containing your `OpenAI:ApiKey`
- An internet connection to access OpenAI’s API

## Installation

1. **Clone** or **download** this repository.
2. Confirm that your `.NET` environment is ready (for example, run `dotnet --version` in a terminal).
3. In the `AgentTestProject` folder, ensure there is a valid `appsettings.json` with at least:
   ```json
   {
     "OpenAI": {
       "ApiKey": "YOUR-OPENAI-KEY-HERE"
     }
   }

4. Run dotnet restore to restore the dependencies.

## Project Structure

    - OpenAI.Agents (class library):
        - Includes Agent.cs, AgentManager.cs, OpenAIClient.cs, OpenAIClientSettings.cs, ServiceCollectionExtensions.cs, and supporting classes.
        - Manages the creation of agents, stores conversation history, and dispatches requests to OpenAI.

    - AgentTestProject (console application):
        - Program.cs demonstrates using the library by creating multiple agents (Project Manager, Developer, Reviewer, QA) and sending messages back and forth.
        - Illustrates a workflow in which each agent assumes a distinct role.

## How It Works

    - Dependency Injection
        - Call AddOpenAIAgents() to register an IAgentManager and the underlying OpenAIClient.
        - The OpenAIClient retrieves your API key from the configuration (IConfiguration) to authenticate.

    - Agent Creation
        - Create named agents like so:

        `var manager = serviceProvider.GetRequiredService<IAgentManager>();`
        `var myAgent = manager.CreateAgent("AgentName", "gpt-3.5-turbo", "System prompt for this agent");`

        - Each agent maintains its own system prompt and conversation history. The library automatically trims older messages if token limits are exceeded.

    - Sending Messages
        - Use manager.SendMessageAsync("AgentName", "Hello!") to pass a user message to an agent and receive a response.
        - The conversation history updates automatically, and you get the completed text.

    - Token Management
        - The library leverages TiktokenSharp to gauge token usage. When the limit is approached, older conversation entries are discarded to remain within constraints.

## Example Usage

Below is a shortened example from AgentTestProject/Program.cs:

`
// Read appsettings.json
var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
var configuration = builder.Build();

// Setup services
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddOpenAIAgents();
var serviceProvider = services.BuildServiceProvider();

// Acquire the agent manager
var manager = serviceProvider.GetRequiredService<IAgentManager>();

// Define agents
var pm = manager.CreateAgent("ProjectManager", "gpt-3.5-turbo", "You are a project manager...");
var dev = manager.CreateAgent("Developer", "gpt-3.5-turbo", "You are a software developer...");
var reviewer = manager.CreateAgent("CodeReviewer", "gpt-3.5-turbo", "You are a code reviewer...");
var qa = manager.CreateAgent("QA", "gpt-3.5-turbo", "You are a QA engineer...");

// Example interaction
var pmMessage = await manager.SendMessageAsync("ProjectManager", "Please define a small task.");
Console.WriteLine($"PM says: {pmMessage}");

var devResponse = await manager.SendMessageAsync("Developer", $"Task from PM: {pmMessage}");
Console.WriteLine($"Developer says: {devResponse}");

// ...and so on for Reviewer and QA.
`

## Running the Example

    1. Open a terminal in the AgentTestProject directory.
    2. Run dotnet run.
    3. Watch the console to see the Project Manager define a task, the Developer implement it, the Reviewer offer feedback, and the QA agent finalize it.

## Customization

    - Model Choice: You can select "gpt-3.5-turbo", "gpt-4", or another model ID your account can access.
    - System Prompts: Adjust each agent’s system prompt to match your use cases.
    - API Key Overrides: Provide a custom API key to an individual agent through CreateAgent if desired.

## Support the Project

- BTC: bc1qe74apu6zhts5rlak7dnpak4p42ezyhkh7cgmaxef47qwjyajmx0qswxtln

## Contributing

Pull requests and issues are welcome! Feel free to contribute if you find improvements or run into problems.

## License

This project is licensed under the MIT License. See LICENSE for more details.